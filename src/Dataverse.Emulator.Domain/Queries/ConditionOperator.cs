using Ardalis.SmartEnum;

namespace Dataverse.Emulator.Domain.Queries;

public sealed class ConditionOperator(string name, int value) : SmartEnum<ConditionOperator>(name, value)
{
    public static readonly ConditionOperator Equal = new(nameof(Equal), 0);
}
