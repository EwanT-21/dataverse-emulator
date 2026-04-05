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

public sealed record LinkedRecordJoin(
    string TableLogicalName,
    string Alias,
    string FromAttributeName,
    string ToAttributeName,
    IReadOnlyList<string> SelectedColumns,
    bool ReturnAllColumns,
    LinkedRecordFilter? Filter);

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
