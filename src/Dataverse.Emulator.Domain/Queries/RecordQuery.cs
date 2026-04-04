using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Queries;

public sealed class RecordQuery
{
    internal RecordQuery(
        string tableLogicalName,
        IReadOnlyList<string> selectedColumns,
        QueryFilter? filter,
        IReadOnlyList<QuerySort> sorts,
        int? top,
        PageRequest? page)
    {
        TableLogicalName = tableLogicalName;
        SelectedColumns = selectedColumns;
        Filter = filter;
        Sorts = sorts;
        Top = top;
        Page = page;
    }

    public string TableLogicalName { get; }

    public IReadOnlyList<string> SelectedColumns { get; }

    public QueryFilter? Filter { get; }

    public IReadOnlyList<QueryCondition> Conditions => Filter?.Conditions ?? Array.Empty<QueryCondition>();

    public IReadOnlyList<QuerySort> Sorts { get; }

    public int? Top { get; }

    public PageRequest? Page { get; }

    public static ErrorOr<RecordQuery> Create(
        string tableLogicalName,
        IReadOnlyList<string>? selectedColumns = null,
        IReadOnlyList<QueryCondition>? conditions = null,
        IReadOnlyList<QuerySort>? sorts = null,
        int? top = null,
        PageRequest? page = null,
        QueryFilter? filter = null)
    {
        if (string.IsNullOrWhiteSpace(tableLogicalName))
        {
            return DomainErrors.Validation(
                "Query.Record.TableLogicalNameRequired",
                "Table logical name is required.");
        }

        if (top is <= 0)
        {
            return DomainErrors.Validation(
                "Query.Top.Invalid",
                "Top must be greater than zero when provided.");
        }

        if (page is { Size: <= 0 })
        {
            return DomainErrors.Validation(
                "Query.Page.SizeInvalid",
                "Page size must be greater than zero when provided.");
        }

        if (filter is not null && conditions is { Count: > 0 })
        {
            return DomainErrors.Validation(
                "Query.Filter.Ambiguous",
                "Record queries cannot provide both root conditions and a filter tree.");
        }

        QueryFilter? effectiveFilter = filter;
        if (effectiveFilter is null && conditions is { Count: > 0 })
        {
            var filterResult = QueryFilter.Create(FilterOperator.And, conditions);
            if (filterResult.IsError)
            {
                return filterResult.Errors;
            }

            effectiveFilter = filterResult.Value;
        }

        return new RecordQuery(
            tableLogicalName,
            selectedColumns ?? Array.Empty<string>(),
            effectiveFilter,
            sorts ?? Array.Empty<QuerySort>(),
            top,
            page);
    }
}
