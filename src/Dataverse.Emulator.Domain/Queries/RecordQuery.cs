using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Queries;

public sealed class RecordQuery
{
    internal RecordQuery(
        string tableLogicalName,
        IReadOnlyList<string> selectedColumns,
        IReadOnlyList<QueryCondition> conditions,
        IReadOnlyList<QuerySort> sorts,
        int? top,
        PageRequest? page)
    {
        TableLogicalName = tableLogicalName;
        SelectedColumns = selectedColumns;
        Conditions = conditions;
        Sorts = sorts;
        Top = top;
        Page = page;
    }

    public string TableLogicalName { get; }

    public IReadOnlyList<string> SelectedColumns { get; }

    public IReadOnlyList<QueryCondition> Conditions { get; }

    public IReadOnlyList<QuerySort> Sorts { get; }

    public int? Top { get; }

    public PageRequest? Page { get; }

    public static ErrorOr<RecordQuery> Create(
        string tableLogicalName,
        IReadOnlyList<string>? selectedColumns = null,
        IReadOnlyList<QueryCondition>? conditions = null,
        IReadOnlyList<QuerySort>? sorts = null,
        int? top = null,
        PageRequest? page = null)
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

        return new RecordQuery(
            tableLogicalName,
            selectedColumns ?? Array.Empty<string>(),
            conditions ?? Array.Empty<QueryCondition>(),
            sorts ?? Array.Empty<QuerySort>(),
            top,
            page);
    }
}
