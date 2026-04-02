using Ardalis.SmartEnum;

namespace Dataverse.Emulator.Domain.Metadata;

public sealed class RequiredLevel(string name, int value) : SmartEnum<RequiredLevel>(name, value)
{
    public static readonly RequiredLevel None = new(nameof(None), 0);
    public static readonly RequiredLevel SystemRequired = new(nameof(SystemRequired), 1);
    public static readonly RequiredLevel ApplicationRequired = new(nameof(ApplicationRequired), 2);
}
