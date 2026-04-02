using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Queries;

public sealed record QueryCondition
{
    internal QueryCondition(
        string columnLogicalName,
        ConditionOperator @operator,
        object? value)
    {
        ColumnLogicalName = columnLogicalName;
        Operator = @operator;
        Value = value;
    }

    public string ColumnLogicalName { get; }

    public ConditionOperator Operator { get; }

    public object? Value { get; }

    public static ErrorOr<QueryCondition> Create(
        string columnLogicalName,
        ConditionOperator @operator,
        object? value)
    {
        if (string.IsNullOrWhiteSpace(columnLogicalName))
        {
            return DomainErrors.Validation(
                "Query.Condition.ColumnRequired",
                "Condition column logical name is required.");
        }

        return new QueryCondition(columnLogicalName, @operator, value);
    }
}
