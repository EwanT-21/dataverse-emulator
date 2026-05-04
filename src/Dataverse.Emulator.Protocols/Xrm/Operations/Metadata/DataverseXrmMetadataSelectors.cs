using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Metadata;
using ErrorOr;
using Mediator;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;

namespace Dataverse.Emulator.Protocols.Xrm.Operations.Metadata;

internal static class DataverseXrmMetadataSelectors
{
    public static EntityFilters ResolveEntityFilters(EntityQueryExpression query)
    {
        var filters = EntityFilters.Entity;
        if (query.AttributeQuery is not null)
        {
            filters |= EntityFilters.Attributes;
        }

        if (query.RelationshipQuery is not null)
        {
            filters |= EntityFilters.Relationships;
        }

        return filters;
    }

    public static async Task<ErrorOr<TableDefinition>> ResolveTableAsync(
        IMediator mediator,
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

    public static async Task<ErrorOr<TableDefinition>> ResolveTableByAttributeMetadataIdAsync(
        IMediator mediator,
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

    public static ErrorOr<ColumnDefinition> ResolveColumn(TableDefinition table, string? logicalName, Guid metadataId)
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
