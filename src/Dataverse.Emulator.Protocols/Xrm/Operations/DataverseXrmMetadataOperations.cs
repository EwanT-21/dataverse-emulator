using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Metadata;
using ErrorOr;
using Mediator;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Dataverse.Emulator.Protocols.Xrm.Operations;

public sealed class DataverseXrmMetadataOperations(IMediator mediator)
{
    public async Task<ErrorOr<EntityMetadata>> RetrieveEntityAsync(
        RetrieveEntityRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return DataverseXrmErrors.ParameterRequired("request");
        }

        var tableResult = await ResolveTableAsync(request.LogicalName, request.MetadataId, cancellationToken);
        if (tableResult.IsError)
        {
            return tableResult.Errors;
        }

        var relationshipsResult = await mediator.Send(new ListRelationshipDefinitionsQuery(), cancellationToken);
        if (relationshipsResult.IsError)
        {
            return relationshipsResult.Errors;
        }

        return DataverseXrmMetadataMapper.ToEntityMetadata(
            tableResult.Value,
            request.EntityFilters,
            relationshipsResult.Value);
    }

    public async Task<ErrorOr<IReadOnlyList<EntityMetadata>>> RetrieveAllEntitiesAsync(
        RetrieveAllEntitiesRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return DataverseXrmErrors.ParameterRequired("request");
        }

        var tables = await mediator.Send(new ListTableDefinitionsQuery(), cancellationToken);
        var relationshipsResult = await mediator.Send(new ListRelationshipDefinitionsQuery(), cancellationToken);
        if (relationshipsResult.IsError)
        {
            return relationshipsResult.Errors;
        }

        return tables
            .Select(table => DataverseXrmMetadataMapper.ToEntityMetadata(
                table,
                request.EntityFilters,
                relationshipsResult.Value))
            .ToArray();
    }

    public async Task<ErrorOr<AttributeMetadata>> RetrieveAttributeAsync(
        RetrieveAttributeRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return DataverseXrmErrors.ParameterRequired("request");
        }

        if (request.ColumnNumber > 0)
        {
            return DataverseXrmErrors.UnsupportedOperation("RetrieveAttribute by ColumnNumber");
        }

        var tableResult = await ResolveTableAsync(request.EntityLogicalName, Guid.Empty, cancellationToken);
        if (tableResult.IsError)
        {
            if (request.MetadataId != Guid.Empty)
            {
                var tableByAttributeIdResult = await ResolveTableByAttributeMetadataIdAsync(request.MetadataId, cancellationToken);
                if (tableByAttributeIdResult.IsError)
                {
                    return tableByAttributeIdResult.Errors;
                }

                tableResult = tableByAttributeIdResult;
            }
            else
            {
                return tableResult.Errors;
            }
        }

        var attributeResult = ResolveColumn(tableResult.Value, request.LogicalName, request.MetadataId);
        if (attributeResult.IsError)
        {
            return attributeResult.Errors;
        }

        return DataverseXrmMetadataMapper.ToAttributeMetadata(tableResult.Value, attributeResult.Value);
    }

    public async Task<ErrorOr<OneToManyRelationshipMetadata>> RetrieveRelationshipAsync(
        RetrieveRelationshipRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return DataverseXrmErrors.ParameterRequired("request");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var relationshipResult = await mediator.Send(
                new GetRelationshipDefinitionQuery(request.Name),
                cancellationToken);

            return relationshipResult.IsError
                ? relationshipResult.Errors
                : DataverseXrmMetadataMapper.ToRelationshipMetadata(relationshipResult.Value);
        }

        if (request.MetadataId != Guid.Empty)
        {
            var relationshipsResult = await mediator.Send(new ListRelationshipDefinitionsQuery(), cancellationToken);
            if (relationshipsResult.IsError)
            {
                return relationshipsResult.Errors;
            }

            var relationship = relationshipsResult.Value.SingleOrDefault(candidate =>
                DataverseXrmMetadataMapper.CreateRelationshipMetadataId(candidate.SchemaName) == request.MetadataId);

            return relationship is not null
                ? DataverseXrmMetadataMapper.ToRelationshipMetadata(relationship)
                : DomainErrors.Validation(
                    "Protocol.Xrm.Metadata.RelationshipSelectorUnsupported",
                    $"Relationship metadata id '{request.MetadataId}' is not known to the local Dataverse emulator.");
        }

        return DomainErrors.Validation(
            "Protocol.Xrm.Metadata.RelationshipSelectorRequired",
            "A relationship schema name or metadata id is required for this metadata request.");
    }

    private async Task<ErrorOr<TableDefinition>> ResolveTableAsync(
        string? logicalName,
        Guid metadataId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(logicalName))
        {
            return await mediator.Send(new GetTableDefinitionQuery(logicalName), cancellationToken);
        }

        if (metadataId != Guid.Empty)
        {
            var tables = await mediator.Send(new ListTableDefinitionsQuery(), cancellationToken);
            var matched = tables.SingleOrDefault(table => DataverseXrmMetadataMapper.CreateTableMetadataId(table.LogicalName) == metadataId);
            return matched is not null
                ? matched
                : DomainErrors.Validation(
                    "Protocol.Xrm.Metadata.TableSelectorUnsupported",
                    $"Entity metadata id '{metadataId}' is not known to the local Dataverse emulator.");
        }

        return DomainErrors.Validation(
            "Protocol.Xrm.Metadata.TableSelectorRequired",
            "A logical name or metadata id is required for this metadata request.");
    }

    private async Task<ErrorOr<TableDefinition>> ResolveTableByAttributeMetadataIdAsync(
        Guid metadataId,
        CancellationToken cancellationToken)
    {
        var tables = await mediator.Send(new ListTableDefinitionsQuery(), cancellationToken);
        var matched = tables.SingleOrDefault(table => table.Columns.Any(
            column => DataverseXrmMetadataMapper.CreateColumnMetadataId(table.LogicalName, column.LogicalName) == metadataId));

        return matched is not null
            ? matched
            : DomainErrors.Validation(
                "Protocol.Xrm.Metadata.AttributeSelectorUnsupported",
                $"Attribute metadata id '{metadataId}' is not known to the local Dataverse emulator.");
    }

    private static ErrorOr<ColumnDefinition> ResolveColumn(TableDefinition table, string? logicalName, Guid metadataId)
    {
        if (!string.IsNullOrWhiteSpace(logicalName))
        {
            var column = table.FindColumn(logicalName);
            return column is not null
                ? column
                : DomainErrors.UnknownColumn(table.LogicalName, logicalName);
        }

        if (metadataId != Guid.Empty)
        {
            var column = table.Columns.SingleOrDefault(
                candidate => DataverseXrmMetadataMapper.CreateColumnMetadataId(table.LogicalName, candidate.LogicalName) == metadataId);

            return column is not null
                ? column
                : DomainErrors.Validation(
                    "Protocol.Xrm.Metadata.AttributeSelectorUnsupported",
                    $"Attribute metadata id '{metadataId}' is not known on table '{table.LogicalName}'.");
        }

        return DomainErrors.Validation(
            "Protocol.Xrm.Metadata.AttributeSelectorRequired",
            "An attribute logical name or metadata id is required for this metadata request.");
    }
}
