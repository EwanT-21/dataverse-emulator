namespace Dataverse.Emulator.Domain.Queries;

public sealed class RecordQuery
{
    public RecordQuery(string tableLogicalName)
    {
        if (string.IsNullOrWhiteSpace(tableLogicalName))
        {
            throw new ArgumentException("Table logical name is required.", nameof(tableLogicalName));
        }

        TableLogicalName = tableLogicalName;
    }

    public string TableLogicalName { get; }

    public IReadOnlyList<string> SelectedColumns { get; init; } = Array.Empty<string>();

    public IReadOnlyList<QueryCondition> Conditions { get; init; } = Array.Empty<QueryCondition>();

    public IReadOnlyList<QuerySort> Sorts { get; init; } = Array.Empty<QuerySort>();

    public int? Top { get; init; }

    public PageRequest? Page { get; init; }
}
