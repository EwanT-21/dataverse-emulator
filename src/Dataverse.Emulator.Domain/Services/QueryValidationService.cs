using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Services;

public sealed class QueryValidationService
{
    public IReadOnlyCollection<Error> Validate(
        TableDefinition table,
        RecordQuery query)
    {
        var errors = new List<Error>();

        foreach (var selectedColumn in query.SelectedColumns)
        {
            if (!table.HasColumn(selectedColumn))
            {
                errors.Add(DomainErrors.UnknownColumn(table.LogicalName, selectedColumn));
            }
        }

        foreach (var condition in query.Conditions)
        {
            if (!table.HasColumn(condition.ColumnLogicalName))
            {
                errors.Add(DomainErrors.UnknownColumn(table.LogicalName, condition.ColumnLogicalName));
            }
        }

        foreach (var sort in query.Sorts)
        {
            if (!table.HasColumn(sort.ColumnLogicalName))
            {
                errors.Add(DomainErrors.UnknownColumn(table.LogicalName, sort.ColumnLogicalName));
            }
        }

        if (query.Top is <= 0)
        {
            errors.Add(Error.Validation("Query.Top.Invalid", "Top must be greater than zero when provided."));
        }

        if (query.Page is { Size: <= 0 })
        {
            errors.Add(Error.Validation("Query.Page.SizeInvalid", "Page size must be greater than zero when provided."));
        }

        return errors;
    }
}
