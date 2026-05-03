using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Metadata;
using ErrorOr;
using Mediator;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;
using System.Collections;
using System.Security.Cryptography;
using System.Text;

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

        if (!IsEmptyFilter(request.Query.AttributeQuery?.Criteria))
        {
            return DataverseXrmErrors.UnsupportedOperation("RetrieveMetadataChanges AttributeQuery.Criteria");
        }

        if (!IsEmptyFilter(request.Query.RelationshipQuery?.Criteria))
        {
            return DataverseXrmErrors.UnsupportedOperation("RetrieveMetadataChanges RelationshipQuery.Criteria");
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
            var matchesResult = MatchesEntityQueryAsync(table, request.Query.Criteria);
            if (matchesResult.IsError)
            {
                return matchesResult.Errors;
            }

            if (matchesResult.Value)
            {
                filteredTables.Add(table);
            }
        }

        var filters = ResolveEntityFilters(request.Query);
        var entityMetadata = new EntityMetadataCollection();
        foreach (var table in filteredTables)
        {
            entityMetadata.Add(DataverseXrmMetadataMapper.ToEntityMetadata(
                table,
                filters,
                relationshipsResult.Value));
        }

        return new RetrieveMetadataChangesResult(
            entityMetadata,
            CreateServerVersionStamp(filteredTables, relationshipsResult.Value));
    }

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

    private static EntityFilters ResolveEntityFilters(EntityQueryExpression query)
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

    private static bool IsEmptyFilter(MetadataFilterExpression? filter)
        => filter is null
            || ((filter.Conditions is null || filter.Conditions.Count == 0)
                && (filter.Filters is null || filter.Filters.Count == 0));

    private static ErrorOr<bool> MatchesEntityQueryAsync(
        TableDefinition table,
        MetadataFilterExpression? criteria)
    {
        if (criteria is null)
        {
            return true;
        }

        var results = new List<bool>();

        if (criteria.Conditions is not null)
        {
            foreach (var condition in criteria.Conditions.Where(condition => condition is not null))
            {
                var conditionResult = MatchesEntityCondition(table, condition!);
                if (conditionResult.IsError)
                {
                    return conditionResult.Errors;
                }

                results.Add(conditionResult.Value);
            }
        }

        if (criteria.Filters is not null)
        {
            foreach (var nestedFilter in criteria.Filters.Where(filter => filter is not null))
            {
                var filterResult = MatchesEntityQueryAsync(table, nestedFilter!);
                if (filterResult.IsError)
                {
                    return filterResult.Errors;
                }

                results.Add(filterResult.Value);
            }
        }

        if (results.Count == 0)
        {
            return true;
        }

        return criteria.FilterOperator == LogicalOperator.Or
            ? results.Any(match => match)
            : results.All(match => match);
    }

    private static ErrorOr<bool> MatchesEntityCondition(
        TableDefinition table,
        MetadataConditionExpression condition)
    {
        if (condition is null)
        {
            return true;
        }

        var propertyValueResult = ResolveTableMetadataPropertyValue(table, condition.PropertyName);
        if (propertyValueResult.IsError)
        {
            return propertyValueResult.Errors;
        }

        var candidates = EnumerateConditionValues(condition.Value).ToArray();
        return condition.ConditionOperator switch
        {
            MetadataConditionOperator.Equals => candidates.Any(candidate => MetadataValuesEqual(propertyValueResult.Value, candidate)),
            MetadataConditionOperator.NotEquals => candidates.All(candidate => !MetadataValuesEqual(propertyValueResult.Value, candidate)),
            MetadataConditionOperator.In => candidates.Any(candidate => MetadataValuesEqual(propertyValueResult.Value, candidate)),
            MetadataConditionOperator.NotIn => candidates.All(candidate => !MetadataValuesEqual(propertyValueResult.Value, candidate)),
            _ => DataverseXrmErrors.UnsupportedOperation(
                $"RetrieveMetadataChanges metadata condition operator '{condition.ConditionOperator}'")
        };
    }

    private static ErrorOr<object?> ResolveTableMetadataPropertyValue(
        TableDefinition table,
        string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return DataverseXrmErrors.ParameterRequired("MetadataConditionExpression.PropertyName");
        }

        return propertyName switch
        {
            "LogicalName" => table.LogicalName,
            "PrimaryIdAttribute" => table.PrimaryIdAttribute,
            "PrimaryNameAttribute" => table.PrimaryNameAttribute,
            "LogicalCollectionName" => table.EntitySetName,
            "EntitySetName" => table.EntitySetName,
            "MetadataId" => DataverseXrmMetadataMapper.CreateTableMetadataId(table.LogicalName),
            _ => DataverseXrmErrors.UnsupportedOperation(
                $"RetrieveMetadataChanges metadata property '{propertyName}'")
        };
    }

    private static IEnumerable<object?> EnumerateConditionValues(object? value)
    {
        if (value is null)
        {
            yield return null;
            yield break;
        }

        if (value is string)
        {
            yield return value;
            yield break;
        }

        if (value is IEnumerable values)
        {
            foreach (var item in values)
            {
                yield return item;
            }

            yield break;
        }

        yield return value;
    }

    private static bool MetadataValuesEqual(object? actual, object? expected)
    {
        if (actual is null || expected is null)
        {
            return actual is null && expected is null;
        }

        if (actual is string actualString)
        {
            return string.Equals(actualString, Convert.ToString(expected), StringComparison.OrdinalIgnoreCase);
        }

        if (actual is Guid actualGuid)
        {
            return expected switch
            {
                Guid expectedGuid => actualGuid == expectedGuid,
                string expectedGuidString when Guid.TryParse(expectedGuidString, out var parsedGuid) => actualGuid == parsedGuid,
                _ => false
            };
        }

        return actual.Equals(expected);
    }

    private static string CreateServerVersionStamp(
        IReadOnlyCollection<TableDefinition> tables,
        IReadOnlyCollection<LookupRelationshipDefinition> relationships)
    {
        var builder = new StringBuilder();

        foreach (var table in tables.OrderBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(table.LogicalName)
                .Append('|')
                .Append(table.EntitySetName)
                .Append('|')
                .Append(table.PrimaryIdAttribute)
                .Append('|')
                .Append(table.PrimaryNameAttribute)
                .Append(';');

            foreach (var column in table.Columns.OrderBy(column => column.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(column.LogicalName)
                    .Append(':')
                    .Append(column.AttributeType.Name)
                    .Append(':')
                    .Append(column.RequiredLevel.Name)
                    .Append(';');
            }
        }

        foreach (var relationship in relationships.OrderBy(relationship => relationship.SchemaName, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(relationship.SchemaName)
                .Append('|')
                .Append(relationship.ReferencedTableLogicalName)
                .Append('|')
                .Append(relationship.ReferencingTableLogicalName)
                .Append(';');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}

public sealed record RetrieveMetadataChangesResult(
    EntityMetadataCollection EntityMetadata,
    string ServerVersionStamp);
