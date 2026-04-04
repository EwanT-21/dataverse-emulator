using Ardalis.SmartEnum;

namespace Dataverse.Emulator.Domain.Queries;

public sealed class ConditionOperator(string name, int value) : SmartEnum<ConditionOperator>(name, value)
{
    public static readonly ConditionOperator Equal = new(nameof(Equal), 0);
    public static readonly ConditionOperator NotEqual = new(nameof(NotEqual), 1);
    public static readonly ConditionOperator Null = new(nameof(Null), 2);
    public static readonly ConditionOperator NotNull = new(nameof(NotNull), 3);
    public static readonly ConditionOperator Like = new(nameof(Like), 4);
    public static readonly ConditionOperator BeginsWith = new(nameof(BeginsWith), 5);
    public static readonly ConditionOperator EndsWith = new(nameof(EndsWith), 6);
    public static readonly ConditionOperator GreaterThan = new(nameof(GreaterThan), 7);
    public static readonly ConditionOperator GreaterThanOrEqual = new(nameof(GreaterThanOrEqual), 8);
    public static readonly ConditionOperator LessThan = new(nameof(LessThan), 9);
    public static readonly ConditionOperator LessThanOrEqual = new(nameof(LessThanOrEqual), 10);
    public static readonly ConditionOperator In = new(nameof(In), 11);
}
