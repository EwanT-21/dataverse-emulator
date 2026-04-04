using Dataverse.Emulator.Domain.Queries;
using ErrorOr;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm;

internal static class DataverseXrmQueryExpressionTranslator
{
    public static ErrorOr<RecordQuery> Translate(QueryBase query)
    {
        if (query is null)
        {
            return DataverseXrmErrors.ParameterRequired("query");
        }

        if (query is not QueryExpression queryExpression)
        {
            return DataverseXrmErrors.UnsupportedQueryType(query.GetType().Name);
        }

        if (queryExpression.LinkEntities.Count > 0)
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("LinkEntity");
        }

        if (queryExpression.Distinct)
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("Distinct");
        }

        if (queryExpression.PageInfo is { Count: > 0 }
            || queryExpression.PageInfo is { PageNumber: > 1 }
            || !string.IsNullOrWhiteSpace(queryExpression.PageInfo?.PagingCookie))
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("Paging");
        }

        if (queryExpression.Criteria is { FilterOperator: LogicalOperator.Or })
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("OR filters");
        }

        if (queryExpression.Criteria is { Filters.Count: > 0 })
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("Nested filter groups");
        }

        var selectedColumnsResult = DataverseXrmEntityMapper.ResolveSelectedColumns(queryExpression.ColumnSet);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        var conditions = new List<QueryCondition>();
        var conditionsSource = queryExpression.Criteria?.Conditions;
        if (conditionsSource is not null)
        {
            foreach (var condition in conditionsSource)
            {
                if (!string.IsNullOrWhiteSpace(condition.EntityName)
                    && !condition.EntityName.Equals(queryExpression.EntityName, StringComparison.OrdinalIgnoreCase))
                {
                    return DataverseXrmErrors.UnsupportedQueryFeature("Cross-entity conditions");
                }

                if (condition.Operator != Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal)
                {
                    return DataverseXrmErrors.UnsupportedQueryFeature(
                        $"Condition operator '{condition.Operator}'");
                }

                if (condition.Values.Count > 1)
                {
                    return DataverseXrmErrors.UnsupportedQueryFeature("Multi-value conditions");
                }

                var valueResult = DataverseXrmEntityMapper.ToScalarValue(
                    condition.Values.Count == 0 ? null : condition.Values[0],
                    condition.AttributeName);
                if (valueResult.IsError)
                {
                    return valueResult.Errors;
                }

                var queryConditionResult = QueryCondition.Create(
                    condition.AttributeName,
                    Dataverse.Emulator.Domain.Queries.ConditionOperator.Equal,
                    valueResult.Value);
                if (queryConditionResult.IsError)
                {
                    return queryConditionResult.Errors;
                }

                conditions.Add(queryConditionResult.Value);
            }
        }

        var sorts = new List<QuerySort>();
        foreach (var order in queryExpression.Orders)
        {
            var sortResult = QuerySort.Create(
                order.AttributeName,
                order.OrderType == OrderType.Descending
                    ? SortDirection.Descending
                    : SortDirection.Ascending);
            if (sortResult.IsError)
            {
                return sortResult.Errors;
            }

            sorts.Add(sortResult.Value);
        }

        return RecordQuery.Create(
            queryExpression.EntityName,
            selectedColumnsResult.Value,
            conditions,
            sorts,
            queryExpression.TopCount,
            page: null);
    }
}
