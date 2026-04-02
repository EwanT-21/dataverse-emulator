using Ardalis.SmartEnum;

namespace Dataverse.Emulator.Domain.Queries;

public sealed class SortDirection(string name, int value) : SmartEnum<SortDirection>(name, value)
{
    public static readonly SortDirection Ascending = new(nameof(Ascending), 0);
    public static readonly SortDirection Descending = new(nameof(Descending), 1);
}
