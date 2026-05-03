using Ardalis.SmartEnum;

namespace Dataverse.Emulator.Domain.Queries;

public sealed class LinkedRecordJoinType(string name, int value) : SmartEnum<LinkedRecordJoinType>(name, value)
{
    public static readonly LinkedRecordJoinType Inner = new(nameof(Inner), 0);
    public static readonly LinkedRecordJoinType LeftOuter = new(nameof(LeftOuter), 1);
}
