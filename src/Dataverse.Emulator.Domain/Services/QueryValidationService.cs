using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;

namespace Dataverse.Emulator.Domain.Services;

public sealed class QueryValidationService
{
    public IReadOnlyCollection<string> Validate(
        TableDefinition table,
        RecordQuery query)
    {
        var errors = new List<string>();

        foreach (var selectedColumn in query.SelectedColumns)
        {
            if (!table.HasColumn(selectedColumn))
            {
                errors.Add($"Selected column '{selectedColumn}' does not exist on table '{table.LogicalName}'.");
            }
        }

        foreach (var condition in query.Conditions)
        {
            if (!table.HasColumn(condition.ColumnLogicalName))
            {
                errors.Add($"Filter column '{condition.ColumnLogicalName}' does not exist on table '{table.LogicalName}'.");
            }
        }

        foreach (var sort in query.Sorts)
        {
            if (!table.HasColumn(sort.ColumnLogicalName))
            {
                errors.Add($"Sort column '{sort.ColumnLogicalName}' does not exist on table '{table.LogicalName}'.");
            }
        }

        if (query.Top is <= 0)
        {
            errors.Add("Top must be greater than zero when provided.");
        }

        if (query.Page is { Size: <= 0 })
        {
            errors.Add("Page size must be greater than zero when provided.");
        }

        return errors;
    }
}
