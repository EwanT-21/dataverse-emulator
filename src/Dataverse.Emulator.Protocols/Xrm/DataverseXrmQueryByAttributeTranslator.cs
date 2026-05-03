using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Protocols.Xrm.Queries;
using ErrorOr;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;
using QueryConditionOperator = Dataverse.Emulator.Domain.Queries.ConditionOperator;

namespace Dataverse.Emulator.Protocols.Xrm;

internal static class DataverseXrmQueryByAttributeTranslator
{
    public static ErrorOr<RecordQuery> Translate(QueryByAttribute query)
    {
        if (query is null)
        {
            return DataverseXrmErrors.ParameterRequired("query");
        }

        if (string.IsNullOrWhiteSpace(query.EntityName))
        {
            return DataverseXrmErrors.ParameterRequired("EntityName");
        }

        var selectedColumnsResult = DataverseXrmEntityMapper.ResolveSelectedColumns(query.ColumnSet);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        var pageResult = TranslatePage(query.PageInfo);
        if (pageResult.IsError)
        {
            return pageResult.Errors;
        }

        var conditionsResult = TranslateConditions(query);
        if (conditionsResult.IsError)
        {
            return conditionsResult.Errors;
        }

        var sortsResult = TranslateSorts(query.Orders, query.EntityName);
        if (sortsResult.IsError)
        {
            return sortsResult.Errors;
        }

        return RecordQuery.Create(
            query.EntityName,
            selectedColumnsResult.Value,
            conditions: conditionsResult.Value,
            sorts: sortsResult.Value,
            top: query.TopCount,
            page: pageResult.Value);
    }

    private static ErrorOr<IReadOnlyList<QueryCondition>> TranslateConditions(QueryByAttribute query)
    {
        if (query.Attributes.Count != query.Values.Count)
        {
            return DomainErrors.Validation(
                "Protocol.Xrm.QueryByAttribute.Mismatch",
                "QueryByAttribute requires the same number of attributes and values.");
        }

        var conditions = new List<QueryCondition>();
        for (var index = 0; index < query.Attributes.Count; index++)
        {
            var attributeName = query.Attributes[index];
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                return DataverseXrmErrors.ParameterRequired($"Attributes[{index}]");
            }

            var valueResult = DataverseXrmEntityMapper.ToScalarValue(query.Values[index], attributeName);
            if (valueResult.IsError)
            {
                return valueResult.Errors;
            }

            var conditionResult = QueryCondition.Create(
                attributeName,
                QueryConditionOperator.Equal,
                valueResult.Value);
            if (conditionResult.IsError)
            {
                return conditionResult.Errors;
            }

            conditions.Add(conditionResult.Value);
        }

        return conditions;
    }

    private static ErrorOr<IReadOnlyList<QuerySort>> TranslateSorts(
        DataCollection<OrderExpression> orders,
        string entityName)
    {
        var sorts = new List<QuerySort>();
        foreach (var order in orders)
        {
            if (!string.IsNullOrWhiteSpace(order.EntityName)
                && !order.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase))
            {
                return DataverseXrmErrors.UnsupportedQueryFeature("Cross-entity ordering");
            }

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

        return sorts;
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
