using System.Reflection;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Metadata;
using ErrorOr;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm.Operations.Metadata;

internal static class DataverseXrmMetadataQueryEvaluator
{
    public static ErrorOr<bool> MatchesEntityQuery(
        TableDefinition table,
        MetadataFilterExpression? criteria)
        => MatchesQuery(
            table,
            criteria,
            DataverseXrmMetadataPropertyResolvers.ResolveTableMetadataPropertyValue);

    public static ErrorOr<Success> ApplyAttributeQuery(
        TableDefinition table,
        EntityMetadata metadata,
        MetadataFilterExpression? criteria)
    {
        if (metadata.Attributes is null || metadata.Attributes.Length == 0 || IsEmptyFilter(criteria))
        {
            return Result.Success;
        }

        var filteredAttributes = new List<AttributeMetadata>();
        foreach (var column in table.Columns)
        {
            var matchesResult = MatchesQuery(
                (table, column),
                criteria,
                DataverseXrmMetadataPropertyResolvers.ResolveColumnMetadataPropertyValue);
            if (matchesResult.IsError)
            {
                return matchesResult.Errors;
            }

            if (matchesResult.Value)
            {
                filteredAttributes.Add(
                    metadata.Attributes.Single(attribute =>
                        string.Equals(attribute.LogicalName, column.LogicalName, StringComparison.OrdinalIgnoreCase)));
            }
        }

        SetMetadataProperty(metadata, nameof(EntityMetadata.Attributes), filteredAttributes.ToArray());
        return Result.Success;
    }

    public static ErrorOr<Success> ApplyRelationshipQuery(
        TableDefinition table,
        EntityMetadata metadata,
        IReadOnlyCollection<LookupRelationshipDefinition> relationships,
        MetadataFilterExpression? criteria)
    {
        if (IsEmptyFilter(criteria))
        {
            return Result.Success;
        }

        var matchingRelationships = new List<LookupRelationshipDefinition>();
        foreach (var relationship in relationships.Where(candidate =>
                     candidate.ReferencedTableLogicalName.Equals(table.LogicalName, StringComparison.OrdinalIgnoreCase)
                     || candidate.ReferencingTableLogicalName.Equals(table.LogicalName, StringComparison.OrdinalIgnoreCase)))
        {
            var matchesResult = MatchesQuery(
                relationship,
                criteria,
                DataverseXrmMetadataPropertyResolvers.ResolveRelationshipMetadataPropertyValue);
            if (matchesResult.IsError)
            {
                return matchesResult.Errors;
            }

            if (matchesResult.Value)
            {
                matchingRelationships.Add(relationship);
            }
        }

        SetMetadataProperty(
            metadata,
            nameof(EntityMetadata.OneToManyRelationships),
            matchingRelationships
                .Where(relationship => relationship.ReferencedTableLogicalName.Equals(table.LogicalName, StringComparison.OrdinalIgnoreCase))
                .Select(DataverseXrmMetadataMapper.ToRelationshipMetadata)
                .ToArray());
        SetMetadataProperty(
            metadata,
            nameof(EntityMetadata.ManyToOneRelationships),
            matchingRelationships
                .Where(relationship => relationship.ReferencingTableLogicalName.Equals(table.LogicalName, StringComparison.OrdinalIgnoreCase))
                .Select(DataverseXrmMetadataMapper.ToRelationshipMetadata)
                .ToArray());

        return Result.Success;
    }

    private static bool IsEmptyFilter(MetadataFilterExpression? filter)
        => filter is null
            || ((filter.Conditions is null || filter.Conditions.Count == 0)
                && (filter.Filters is null || filter.Filters.Count == 0));

    private static ErrorOr<bool> MatchesQuery<TCandidate>(
        TCandidate candidate,
        MetadataFilterExpression? criteria,
        Func<TCandidate, string?, ErrorOr<object?>> propertyResolver)
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
                var conditionResult = MatchesCondition(candidate, condition!, propertyResolver);
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
                var filterResult = MatchesQuery(candidate, nestedFilter!, propertyResolver);
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

    private static ErrorOr<bool> MatchesCondition<TCandidate>(
        TCandidate candidate,
        MetadataConditionExpression condition,
        Func<TCandidate, string?, ErrorOr<object?>> propertyResolver)
    {
        if (condition is null)
        {
            return true;
        }

        var propertyValueResult = propertyResolver(candidate, condition.PropertyName);
        if (propertyValueResult.IsError)
        {
            return propertyValueResult.Errors;
        }

        var candidates = DataverseXrmMetadataConditionValues.Enumerate(condition.Value).ToArray();
        return condition.ConditionOperator switch
        {
            MetadataConditionOperator.Equals => candidates.Any(value => DataverseXrmMetadataConditionValues.ValuesEqual(propertyValueResult.Value, value)),
            MetadataConditionOperator.NotEquals => candidates.All(value => !DataverseXrmMetadataConditionValues.ValuesEqual(propertyValueResult.Value, value)),
            MetadataConditionOperator.In => candidates.Any(value => DataverseXrmMetadataConditionValues.ValuesEqual(propertyValueResult.Value, value)),
            MetadataConditionOperator.NotIn => candidates.All(value => !DataverseXrmMetadataConditionValues.ValuesEqual(propertyValueResult.Value, value)),
            MetadataConditionOperator.GreaterThan => candidates.Any(value =>
                DataverseXrmMetadataConditionValues.TryCompare(propertyValueResult.Value, value, out var comparison)
                && comparison > 0),
            MetadataConditionOperator.LessThan => candidates.Any(value =>
                DataverseXrmMetadataConditionValues.TryCompare(propertyValueResult.Value, value, out var comparison)
                && comparison < 0),
            _ => DataverseXrmErrors.UnsupportedOperation(
                $"RetrieveMetadataChanges metadata condition operator '{condition.ConditionOperator}'")
        };
    }

    private static void SetMetadataProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property?.SetValue(target, value);
    }
}
