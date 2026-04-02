namespace Dataverse.Emulator.Domain.Queries;

public sealed record QueryCondition(
    string ColumnLogicalName,
    ConditionOperator Operator,
    object? Value);
