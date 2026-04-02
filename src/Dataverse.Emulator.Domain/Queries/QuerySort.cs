namespace Dataverse.Emulator.Domain.Queries;

public sealed record QuerySort(
    string ColumnLogicalName,
    SortDirection Direction);
