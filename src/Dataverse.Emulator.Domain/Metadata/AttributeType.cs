using Ardalis.SmartEnum;

namespace Dataverse.Emulator.Domain.Metadata;

public sealed class AttributeType(string name, int value) : SmartEnum<AttributeType>(name, value)
{
    public static readonly AttributeType UniqueIdentifier = new(nameof(UniqueIdentifier), 0);
    public static readonly AttributeType String = new(nameof(String), 1);
    public static readonly AttributeType Integer = new(nameof(Integer), 2);
    public static readonly AttributeType Decimal = new(nameof(Decimal), 3);
    public static readonly AttributeType Boolean = new(nameof(Boolean), 4);
    public static readonly AttributeType DateTime = new(nameof(DateTime), 5);
    public static readonly AttributeType Lookup = new(nameof(Lookup), 6);
}
