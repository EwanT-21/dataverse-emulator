using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Queries;

public sealed record QueryCondition
{
    internal QueryCondition(
        string columnLogicalName,
        ConditionOperator @operator,
        IReadOnlyList<object?> values)
    {
        ColumnLogicalName = columnLogicalName;
        Operator = @operator;
        Values = values;
    }

    public string ColumnLogicalName { get; }

    public ConditionOperator Operator { get; }

    public IReadOnlyList<object?> Values { get; }

    public object? Value => Values.Count == 0 ? null : Values[0];

    public static ErrorOr<QueryCondition> Create(
        string columnLogicalName,
        ConditionOperator @operator,
        object? value)
        => Create(columnLogicalName, @operator, [value]);

    public static ErrorOr<QueryCondition> Create(
        string columnLogicalName,
        ConditionOperator @operator,
        IReadOnlyList<object?>? values)
    {
        if (string.IsNullOrWhiteSpace(columnLogicalName))
        {
            return DomainErrors.Validation(
                "Query.Condition.ColumnRequired",
                "Condition column logical name is required.");
        }

        var normalizedValues = values?.ToArray() ?? Array.Empty<object?>();

        if (@operator == ConditionOperator.Null || @operator == ConditionOperator.NotNull)
        {
            if (normalizedValues.Length > 0)
            {
                return DomainErrors.Validation(
                    "Query.Condition.ValuesUnsupported",
                    $"Condition operator '{@operator.Name}' does not accept values.");
            }
        }
        else if (@operator == ConditionOperator.In)
        {
            if (normalizedValues.Length == 0)
            {
                return DomainErrors.Validation(
                    "Query.Condition.ValuesRequired",
                    $"Condition operator '{@operator.Name}' requires one or more values.");
            }
        }
        else if (normalizedValues.Length != 1)
        {
            return DomainErrors.Validation(
                "Query.Condition.ValueRequired",
                $"Condition operator '{@operator.Name}' requires exactly one value.");
        }

        return new QueryCondition(columnLogicalName, @operator, normalizedValues);
    }
}
