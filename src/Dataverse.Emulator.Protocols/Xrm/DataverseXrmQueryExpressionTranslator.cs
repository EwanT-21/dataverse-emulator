using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Protocols.Xrm.Queries;
using ErrorOr;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;
using QueryConditionOperator = Dataverse.Emulator.Domain.Queries.ConditionOperator;
using QueryFilterOperator = Dataverse.Emulator.Domain.Queries.FilterOperator;

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

        var selectedColumnsResult = DataverseXrmEntityMapper.ResolveSelectedColumns(queryExpression.ColumnSet);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        var pageResult = TranslatePage(queryExpression.PageInfo);
        if (pageResult.IsError)
        {
            return pageResult.Errors;
        }

        var filterResult = TranslateFilter(queryExpression.Criteria, queryExpression.EntityName);
        if (filterResult.IsError)
        {
            return filterResult.Errors;
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
            filter: filterResult.Value,
            sorts: sorts,
            top: queryExpression.TopCount,
            page: pageResult.Value);
    }

    private static ErrorOr<QueryFilter?> TranslateFilter(FilterExpression? filterExpression, string entityName)
    {
        if (filterExpression is null)
        {
            return (QueryFilter?)null;
        }

        var conditions = new List<QueryCondition>();
        foreach (var condition in filterExpression.Conditions)
        {
            var conditionResult = TranslateCondition(condition, entityName);
            if (conditionResult.IsError)
            {
                return conditionResult.Errors;
            }

            conditions.Add(conditionResult.Value);
        }

        var childFilters = new List<QueryFilter>();
        foreach (var childFilter in filterExpression.Filters)
        {
            var childFilterResult = TranslateFilter(childFilter, entityName);
            if (childFilterResult.IsError)
            {
                return childFilterResult.Errors;
            }

            if (childFilterResult.Value is not null)
            {
                childFilters.Add(childFilterResult.Value);
            }
        }

        if (conditions.Count == 0 && childFilters.Count == 0)
        {
            return (QueryFilter?)null;
        }

        var filterOperator = filterExpression.FilterOperator == LogicalOperator.Or
            ? QueryFilterOperator.Or
            : QueryFilterOperator.And;

        var queryFilterResult = QueryFilter.Create(filterOperator, conditions, childFilters);
        return queryFilterResult.IsError
            ? queryFilterResult.Errors
            : (QueryFilter?)queryFilterResult.Value;
    }

    private static ErrorOr<QueryCondition> TranslateCondition(
        ConditionExpression condition,
        string entityName)
    {
        if (!string.IsNullOrWhiteSpace(condition.EntityName)
            && !condition.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("Cross-entity conditions");
        }

        var operatorResult = TranslateConditionOperator(condition.Operator);
        if (operatorResult.IsError)
        {
            return operatorResult.Errors;
        }

        var valuesResult = TranslateConditionValues(condition, operatorResult.Value);
        if (valuesResult.IsError)
        {
            return valuesResult.Errors;
        }

        return QueryCondition.Create(
            condition.AttributeName,
            operatorResult.Value,
            valuesResult.Value);
    }

    private static ErrorOr<QueryConditionOperator> TranslateConditionOperator(Microsoft.Xrm.Sdk.Query.ConditionOperator conditionOperator)
    {
        return conditionOperator switch
        {
            Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal => QueryConditionOperator.Equal,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.NotEqual => QueryConditionOperator.NotEqual,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.Null => QueryConditionOperator.Null,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.NotNull => QueryConditionOperator.NotNull,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.Like => QueryConditionOperator.Like,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.BeginsWith => QueryConditionOperator.BeginsWith,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.EndsWith => QueryConditionOperator.EndsWith,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.GreaterThan => QueryConditionOperator.GreaterThan,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.GreaterEqual => QueryConditionOperator.GreaterThanOrEqual,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.LessThan => QueryConditionOperator.LessThan,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.LessEqual => QueryConditionOperator.LessThanOrEqual,
            Microsoft.Xrm.Sdk.Query.ConditionOperator.In => QueryConditionOperator.In,
            _ => DataverseXrmErrors.UnsupportedQueryFeature(
                $"Condition operator '{conditionOperator}'")
        };
    }

    private static ErrorOr<IReadOnlyList<object?>> TranslateConditionValues(
        ConditionExpression condition,
        QueryConditionOperator conditionOperator)
    {
        if (conditionOperator == QueryConditionOperator.Null || conditionOperator == QueryConditionOperator.NotNull)
        {
            return Array.Empty<object?>();
        }

        var values = new List<object?>();
        foreach (var rawValue in condition.Values)
        {
            var valueResult = DataverseXrmEntityMapper.ToScalarValue(rawValue, condition.AttributeName);
            if (valueResult.IsError)
            {
                return valueResult.Errors;
            }

            values.Add(valueResult.Value);
        }

        return values;
    }

    private static ErrorOr<PageRequest?> TranslatePage(PagingInfo? pageInfo)
    {
        if (pageInfo is null)
        {
            return (PageRequest?)null;
        }

        if (pageInfo.ReturnTotalRecordCount)
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("ReturnTotalRecordCount");
        }

        var hasPageNumber = pageInfo.PageNumber > 1;
        var hasPagingCookie = !string.IsNullOrWhiteSpace(pageInfo.PagingCookie);
        if (pageInfo.Count <= 0)
        {
            return hasPageNumber || hasPagingCookie
                ? DataverseXrmErrors.InvalidPagingRequest(
                    "PageInfo.Count must be greater than zero when paging is used.")
                : (PageRequest?)null;
        }

        var continuationToken = DataverseXrmPagingCookie.ExtractContinuationToken(pageInfo.PagingCookie);
        if (string.IsNullOrWhiteSpace(continuationToken) && hasPageNumber)
        {
            var offset = checked((pageInfo.PageNumber - 1) * pageInfo.Count);
            continuationToken = offset.ToString(CultureInfo.InvariantCulture);
        }

        var pageRequestResult = PageRequest.Create(pageInfo.Count, continuationToken);
        return pageRequestResult.IsError
            ? pageRequestResult.Errors
            : (PageRequest?)pageRequestResult.Value;
    }
}
