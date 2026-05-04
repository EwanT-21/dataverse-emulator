using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Operations.Metadata;
using ErrorOr;
using Mediator;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Dataverse.Emulator.Protocols.Xrm.Operations;

public sealed class DataverseXrmMetadataOperations(IMediator mediator)
{
    public async Task<ErrorOr<RetrieveMetadataChangesResult>> RetrieveMetadataChangesAsync(
        RetrieveMetadataChangesRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return DataverseXrmErrors.ParameterRequired("request");
        }

        if (request.Query is null)
        {
            return DataverseXrmErrors.ParameterRequired("Query");
        }

        var tables = await mediator.Send(new ListTableDefinitionsQuery(), cancellationToken);
        var relationshipsResult = await mediator.Send(new ListRelationshipDefinitionsQuery(), cancellationToken);
        if (relationshipsResult.IsError)
        {
            return relationshipsResult.Errors;
        }

        var filteredTables = new List<TableDefinition>();
        foreach (var table in tables)
        {
            var matchesResult = DataverseXrmMetadataQueryEvaluator.MatchesEntityQuery(table, request.Query.Criteria);
            if (matchesResult.IsError)
            {
                return matchesResult.Errors;
            }

            if (matchesResult.Value)
            {
                filteredTables.Add(table);
            }
        }

        var filters = DataverseXrmMetadataSelectors.ResolveEntityFilters(request.Query);
        var entityMetadata = new EntityMetadataCollection();
        foreach (var table in filteredTables)
        {
            var metadata = DataverseXrmMetadataMapper.ToEntityMetadata(
                table,
                filters,
                relationshipsResult.Value);

            var attributeFilterResult = DataverseXrmMetadataQueryEvaluator.ApplyAttributeQuery(
                table,
                metadata,
                request.Query.AttributeQuery?.Criteria);
            if (attributeFilterResult.IsError)
            {
                return attributeFilterResult.Errors;
            }

            var relationshipFilterResult = DataverseXrmMetadataQueryEvaluator.ApplyRelationshipQuery(
                table,
                metadata,
                relationshipsResult.Value,
                request.Query.RelationshipQuery?.Criteria);
            if (relationshipFilterResult.IsError)
            {
                return relationshipFilterResult.Errors;
            }

            entityMetadata.Add(metadata);
        }

        return new RetrieveMetadataChangesResult(
            entityMetadata,
            DataverseXrmMetadataServerVersionStamp.Create(filteredTables, relationshipsResult.Value));
    }

    public async Task<ErrorOr<EntityMetadata>> RetrieveEntityAsync(
        RetrieveEntityRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return DataverseXrmErrors.ParameterRequired("request");
        }

        var tableResult = await DataverseXrmMetadataSelectors.ResolveTableAsync(
            mediator,
            request.LogicalName,
            request.MetadataId,
            cancellationToken);
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

        var tableResult = await DataverseXrmMetadataSelectors.ResolveTableAsync(
            mediator,
            request.EntityLogicalName,
            Guid.Empty,
            cancellationToken);
        if (tableResult.IsError)
        {
            if (request.MetadataId != Guid.Empty)
            {
                var tableByAttributeIdResult = await DataverseXrmMetadataSelectors.ResolveTableByAttributeMetadataIdAsync(
                    mediator,
                    request.MetadataId,
                    cancellationToken);
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

        var attributeResult = DataverseXrmMetadataSelectors.ResolveColumn(
            tableResult.Value,
            request.LogicalName,
            request.MetadataId);
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
}

public sealed record RetrieveMetadataChangesResult(
    EntityMetadataCollection EntityMetadata,
    string ServerVersionStamp);
