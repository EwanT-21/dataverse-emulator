using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Domain.Queries;

public sealed record LinkedRecordQuery(
    string RootTableLogicalName,
    IReadOnlyList<string> RootSelectedColumns,
    IReadOnlyList<LinkedRecordJoin> Joins,
    LinkedRecordFilter? Filter,
    IReadOnlyList<LinkedRecordSort> Sorts,
    int? Top,
    PageRequest? Page,
    int CurrentPageNumber);

public sealed record LinkedRecordJoin
{
    public LinkedRecordJoin(
        string TableLogicalName,
        string Alias,
        string FromAttributeName,
        string ToAttributeName,
        IReadOnlyList<string> SelectedColumns,
        bool ReturnAllColumns,
        LinkedRecordFilter? Filter,
        string? ParentScopeName = null,
        LinkedRecordJoinType? JoinType = null)
    {
        this.TableLogicalName = TableLogicalName;
        this.Alias = Alias;
        this.FromAttributeName = FromAttributeName;
        this.ToAttributeName = ToAttributeName;
        this.SelectedColumns = SelectedColumns;
        this.ReturnAllColumns = ReturnAllColumns;
        this.Filter = Filter;
        this.ParentScopeName = ParentScopeName ?? string.Empty;
        this.JoinType = JoinType ?? LinkedRecordJoinType.Inner;
    }

    public string TableLogicalName { get; }

    public string Alias { get; }

    public string ParentScopeName { get; }

    public string FromAttributeName { get; }

    public string ToAttributeName { get; }

    public LinkedRecordJoinType JoinType { get; }

    public IReadOnlyList<string> SelectedColumns { get; }

    public bool ReturnAllColumns { get; }

    public LinkedRecordFilter? Filter { get; }
}

public sealed record LinkedRecordFilter(
    FilterOperator Operator,
    IReadOnlyList<LinkedRecordCondition> Conditions,
    IReadOnlyList<LinkedRecordFilter> Filters);

public sealed record LinkedRecordCondition(
    string ScopeName,
    string ColumnLogicalName,
    ConditionOperator Operator,
    IReadOnlyList<object?> Values);

public sealed record LinkedRecordSort(
    string ScopeName,
    string ColumnLogicalName,
    SortDirection Direction);

public sealed record LinkedEntityRecord(
    EntityRecord RootRecord,
    IReadOnlyDictionary<string, EntityRecord> LinkedRecords);
