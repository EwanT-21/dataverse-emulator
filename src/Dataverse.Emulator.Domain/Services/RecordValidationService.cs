using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Domain.Services;

public sealed class RecordValidationService
{
    public IReadOnlyCollection<string> ValidateCreate(
        TableDefinition table,
        IReadOnlyDictionary<string, object?> values)
    {
        var errors = new List<string>();

        foreach (var pair in values)
        {
            var column = table.FindColumn(pair.Key);
            if (column is null)
            {
                errors.Add($"Column '{pair.Key}' does not exist on table '{table.LogicalName}'.");
                continue;
            }

            if (!IsCompatible(column, pair.Value))
            {
                errors.Add($"Column '{pair.Key}' does not accept values of type '{pair.Value?.GetType().Name ?? "null"}'.");
            }
        }

        foreach (var column in table.Columns.Where(column => column.RequiredLevel is RequiredLevel.ApplicationRequired or RequiredLevel.SystemRequired))
        {
            if (!values.ContainsKey(column.LogicalName))
            {
                errors.Add($"Column '{column.LogicalName}' is required on table '{table.LogicalName}'.");
            }
        }

        return errors;
    }

    public IReadOnlyCollection<string> ValidateUpdate(
        TableDefinition table,
        IReadOnlyDictionary<string, object?> values)
    {
        var errors = new List<string>();

        foreach (var pair in values)
        {
            var column = table.FindColumn(pair.Key);
            if (column is null)
            {
                errors.Add($"Column '{pair.Key}' does not exist on table '{table.LogicalName}'.");
                continue;
            }

            if (column.IsPrimaryId || pair.Key.Equals(table.PrimaryIdAttribute, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Column '{pair.Key}' is immutable.");
                continue;
            }

            if (!IsCompatible(column, pair.Value))
            {
                errors.Add($"Column '{pair.Key}' does not accept values of type '{pair.Value?.GetType().Name ?? "null"}'.");
            }
        }

        return errors;
    }

    private static bool IsCompatible(ColumnDefinition column, object? value)
    {
        if (value is null)
        {
            return column.RequiredLevel == RequiredLevel.None;
        }

        return column.AttributeType switch
        {
            AttributeType.UniqueIdentifier => value is Guid,
            AttributeType.String => value is string,
            AttributeType.Integer => value is int,
            AttributeType.Decimal => value is decimal or double or float,
            AttributeType.Boolean => value is bool,
            AttributeType.DateTime => value is DateTime or DateTimeOffset,
            AttributeType.Lookup => value is Guid,
            _ => false
        };
    }
}
