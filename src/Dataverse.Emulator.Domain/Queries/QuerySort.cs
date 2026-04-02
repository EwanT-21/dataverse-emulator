using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Queries;

public sealed record QuerySort
{
    internal QuerySort(
        string columnLogicalName,
        SortDirection direction)
    {
        ColumnLogicalName = columnLogicalName;
        Direction = direction;
    }

    public string ColumnLogicalName { get; }

    public SortDirection Direction { get; }

    public static ErrorOr<QuerySort> Create(
        string columnLogicalName,
        SortDirection direction)
    {
        if (string.IsNullOrWhiteSpace(columnLogicalName))
        {
            return DomainErrors.Validation(
                "Query.Sort.ColumnRequired",
                "Sort column logical name is required.");
        }

        return new QuerySort(columnLogicalName, direction);
    }
}
