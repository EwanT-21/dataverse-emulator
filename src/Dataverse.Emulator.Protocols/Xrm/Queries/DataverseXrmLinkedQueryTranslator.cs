using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Queries;
using ErrorOr;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;
using QueryConditionOperator = Dataverse.Emulator.Domain.Queries.ConditionOperator;
using QueryFilterOperator = Dataverse.Emulator.Domain.Queries.FilterOperator;

namespace Dataverse.Emulator.Protocols.Xrm.Queries;

internal static class DataverseXrmLinkedQueryTranslator
{
    public static ErrorOr<LinkedRecordQuery> Translate(QueryExpression queryExpression)
    {
        if (queryExpression is null)
        {
            return DataverseXrmErrors.ParameterRequired("query");
        }

        if (queryExpression.Distinct)
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("Distinct");
        }

        var rootSelectedColumnsResult = DataverseXrmEntityMapper.ResolveSelectedColumns(queryExpression.ColumnSet);
        if (rootSelectedColumnsResult.IsError)
        {
            return rootSelectedColumnsResult.Errors;
        }

        var pageResult = TranslatePage(queryExpression.PageInfo);
        if (pageResult.IsError)
        {
            return pageResult.Errors;
        }

        var rawLinkDefinitions = new List<RawLinkedQueryJoin>();
        foreach (var linkEntity in queryExpression.LinkEntities)
        {
            var linkResult = BuildJoin(linkEntity, queryExpression.EntityName);
            if (linkResult.IsError)
            {
                return linkResult.Errors;
            }

            rawLinkDefinitions.Add(linkResult.Value);
        }

        var scopeRegistryResult = BuildScopeRegistry(queryExpression.EntityName, rawLinkDefinitions);
        if (scopeRegistryResult.IsError)
        {
            return scopeRegistryResult.Errors;
        }

        var scopeRegistry = scopeRegistryResult.Value;

        var filterResult = TranslateFilter(
            queryExpression.Criteria,
            queryExpression.EntityName,
            scopeRegistry);
        if (filterResult.IsError)
        {
            return filterResult.Errors;
        }

        var sortsResult = TranslateSorts(
            queryExpression.Orders,
            queryExpression.EntityName,
            scopeRegistry);
        if (sortsResult.IsError)
        {
            return sortsResult.Errors;
        }

        var joins = new List<LinkedRecordJoin>();
        foreach (var rawLinkDefinition in rawLinkDefinitions)
        {
            var joinFilterResult = TranslateFilter(
                rawLinkDefinition.LinkCriteria,
                rawLinkDefinition.Alias,
                scopeRegistry);
            if (joinFilterResult.IsError)
            {
                return joinFilterResult.Errors;
            }

            joins.Add(new LinkedRecordJoin(
                rawLinkDefinition.TableLogicalName,
                rawLinkDefinition.Alias,
                rawLinkDefinition.FromAttributeName,
                rawLinkDefinition.ToAttributeName,
                rawLinkDefinition.SelectedColumns,
                rawLinkDefinition.ReturnAllColumns,
                joinFilterResult.Value));
        }

        return new LinkedRecordQuery(
            queryExpression.EntityName,
            rootSelectedColumnsResult.Value,
            joins,
            filterResult.Value,
            sortsResult.Value,
            queryExpression.TopCount,
            pageResult.Value.PageRequest,
            pageResult.Value.CurrentPageNumber);
    }

    private static ErrorOr<RawLinkedQueryJoin> BuildJoin(
        LinkEntity linkEntity,
        string rootEntityName)
    {
        if (linkEntity.LinkEntities.Count > 0)
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("Nested LinkEntity");
        }

        if (linkEntity.JoinOperator != JoinOperator.Inner)
        {
            return DataverseXrmErrors.UnsupportedQueryFeature(
                $"Join operator '{linkEntity.JoinOperator}'");
        }

        if (!string.IsNullOrWhiteSpace(linkEntity.LinkFromEntityName)
            && !linkEntity.LinkFromEntityName.Equals(rootEntityName, StringComparison.OrdinalIgnoreCase))
        {
            return DataverseXrmErrors.UnsupportedQueryFeature("Non-root LinkFromEntityName");
        }

        if (string.IsNullOrWhiteSpace(linkEntity.LinkFromAttributeName))
        {
            return DataverseXrmErrors.ParameterRequired("LinkFromAttributeName");
        }

        if (string.IsNullOrWhiteSpace(linkEntity.LinkToEntityName))
        {
            return DataverseXrmErrors.ParameterRequired("LinkToEntityName");
        }

        if (string.IsNullOrWhiteSpace(linkEntity.LinkToAttributeName))
        {
            return DataverseXrmErrors.ParameterRequired("LinkToAttributeName");
        }

        var selectedColumnsResult = DataverseXrmEntityMapper.ResolveSelectedColumns(
            linkEntity.Columns,
            allowEmptySelection: true);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        var alias = string.IsNullOrWhiteSpace(linkEntity.EntityAlias)
            ? linkEntity.LinkToEntityName
            : linkEntity.EntityAlias;

        return new RawLinkedQueryJoin(
            linkEntity.LinkToEntityName,
            alias,
            linkEntity.LinkFromAttributeName,
            linkEntity.LinkToAttributeName,
            selectedColumnsResult.Value,
            linkEntity.Columns?.AllColumns == true,
            linkEntity.LinkCriteria);
    }

    private static ErrorOr<IReadOnlyDictionary<string, string>> BuildScopeRegistry(
        string rootEntityName,
        IReadOnlyList<RawLinkedQueryJoin> joins)
    {
        var scopeRegistry = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [rootEntityName] = rootEntityName
        };

        foreach (var duplicateAlias in joins
                     .GroupBy(join => join.Alias, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            return DomainErrors.Validation(
                "Query.Scope.Duplicate",
                $"Linked query scope '{duplicateAlias}' is defined more than once.");
        }

        foreach (var join in joins)
        {
            if (scopeRegistry.ContainsKey(join.Alias))
            {
                return DomainErrors.Validation(
                    "Query.Scope.Conflict",
                    $"Linked query scope '{join.Alias}' conflicts with an existing scope.");
            }

            scopeRegistry[join.Alias] = join.Alias;
        }

        foreach (var logicalNameGroup in joins.GroupBy(join => join.TableLogicalName, StringComparer.OrdinalIgnoreCase))
        {
            if (logicalNameGroup.Count() != 1)
            {
                continue;
            }

            var join = logicalNameGroup.Single();
            if (!scopeRegistry.ContainsKey(join.TableLogicalName))
            {
                scopeRegistry[join.TableLogicalName] = join.Alias;
            }
        }

        return scopeRegistry;
    }

    private static ErrorOr<LinkedRecordFilter?> TranslateFilter(
        FilterExpression? filterExpression,
        string defaultScopeName,
        IReadOnlyDictionary<string, string> scopeRegistry)
    {
        if (filterExpression is null)
        {
            return (LinkedRecordFilter?)null;
        }

        var conditions = new List<LinkedRecordCondition>();
        foreach (var condition in filterExpression.Conditions)
        {
            var conditionResult = TranslateCondition(condition, defaultScopeName, scopeRegistry);
            if (conditionResult.IsError)
            {
                return conditionResult.Errors;
            }

            conditions.Add(conditionResult.Value);
        }

        var childFilters = new List<LinkedRecordFilter>();
        foreach (var childFilter in filterExpression.Filters)
        {
            var childFilterResult = TranslateFilter(childFilter, defaultScopeName, scopeRegistry);
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
            return (LinkedRecordFilter?)null;
        }

        return new LinkedRecordFilter(
            filterExpression.FilterOperator == LogicalOperator.Or
                ? QueryFilterOperator.Or
                : QueryFilterOperator.And,
            conditions,
            childFilters);
    }

    private static ErrorOr<LinkedRecordCondition> TranslateCondition(
        ConditionExpression condition,
        string defaultScopeName,
        IReadOnlyDictionary<string, string> scopeRegistry)
    {
        var scopeResult = ResolveScope(condition.EntityName, defaultScopeName, scopeRegistry);
        if (scopeResult.IsError)
        {
            return scopeResult.Errors;
        }

        if (string.IsNullOrWhiteSpace(condition.AttributeName))
        {
            return DataverseXrmErrors.ParameterRequired("AttributeName");
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

        return new LinkedRecordCondition(
            scopeResult.Value,
            condition.AttributeName,
            operatorResult.Value,
            valuesResult.Value);
    }

    private static ErrorOr<IReadOnlyList<LinkedRecordSort>> TranslateSorts(
        DataCollection<OrderExpression> orders,
        string defaultScopeName,
        IReadOnlyDictionary<string, string> scopeRegistry)
    {
        var sorts = new List<LinkedRecordSort>();
        foreach (var order in orders)
        {
            var scopeResult = ResolveScope(order.EntityName, defaultScopeName, scopeRegistry);
            if (scopeResult.IsError)
            {
                return scopeResult.Errors;
            }

            if (string.IsNullOrWhiteSpace(order.AttributeName))
            {
                return DataverseXrmErrors.ParameterRequired("Order.AttributeName");
            }

            sorts.Add(new LinkedRecordSort(
                scopeResult.Value,
                order.AttributeName,
                order.OrderType == OrderType.Descending
                    ? SortDirection.Descending
                    : SortDirection.Ascending));
        }

        return sorts;
    }

    private static ErrorOr<string> ResolveScope(
        string? scopeName,
        string defaultScopeName,
        IReadOnlyDictionary<string, string> scopeRegistry)
    {
        var resolvedName = string.IsNullOrWhiteSpace(scopeName)
            ? defaultScopeName
            : scopeName;

        return scopeRegistry.TryGetValue(resolvedName, out var runtimeName)
            ? runtimeName
            : DomainErrors.Validation(
                "Query.Scope.Unknown",
                $"Linked query scope '{resolvedName}' does not exist.");
    }

    private static ErrorOr<QueryConditionOperator> TranslateConditionOperator(
        Microsoft.Xrm.Sdk.Query.ConditionOperator conditionOperator)
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

    private static ErrorOr<LinkedQueryPage> TranslatePage(PagingInfo? pageInfo)
    {
        if (pageInfo is null)
        {
            return new LinkedQueryPage(null, 1);
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
                : new LinkedQueryPage(null, 1);
        }

        var continuationToken = DataverseXrmPagingCookie.ExtractContinuationToken(pageInfo.PagingCookie);
        if (string.IsNullOrWhiteSpace(continuationToken) && hasPageNumber)
        {
            var offset = checked((pageInfo.PageNumber - 1) * pageInfo.Count);
            continuationToken = offset.ToString(CultureInfo.InvariantCulture);
        }

        var pageRequestResult = PageRequest.Create(pageInfo.Count, continuationToken);
        if (pageRequestResult.IsError)
        {
            return pageRequestResult.Errors;
        }

        return new LinkedQueryPage(
            pageRequestResult.Value,
            pageInfo.PageNumber > 0 ? pageInfo.PageNumber : 1);
    }
}

internal sealed record RawLinkedQueryJoin(
    string TableLogicalName,
    string Alias,
    string FromAttributeName,
    string ToAttributeName,
    IReadOnlyList<string> SelectedColumns,
    bool ReturnAllColumns,
    FilterExpression LinkCriteria);

internal sealed record LinkedQueryPage(
    PageRequest? PageRequest,
    int CurrentPageNumber);
