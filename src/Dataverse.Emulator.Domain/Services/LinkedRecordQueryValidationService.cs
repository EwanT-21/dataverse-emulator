using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Services;

public sealed class LinkedRecordQueryValidationService
{
    public IReadOnlyCollection<Error> Validate(
        TableDefinition rootTable,
        IReadOnlyDictionary<string, TableDefinition> linkedTablesByAlias,
        LinkedRecordQuery query)
    {
        var errors = new List<Error>();

        foreach (var selectedColumn in query.RootSelectedColumns)
        {
            if (!rootTable.HasColumn(selectedColumn))
            {
                errors.Add(DomainErrors.UnknownColumn(rootTable.LogicalName, selectedColumn));
            }
        }

        foreach (var join in query.Joins)
        {
            if (!rootTable.HasColumn(join.FromAttributeName))
            {
                errors.Add(DomainErrors.UnknownColumn(rootTable.LogicalName, join.FromAttributeName));
            }

            if (!linkedTablesByAlias.TryGetValue(join.Alias, out var linkedTable))
            {
                errors.Add(DomainErrors.Validation(
                    "Query.Scope.Unknown",
                    $"Linked query scope '{join.Alias}' does not exist."));
                continue;
            }

            if (!linkedTable.HasColumn(join.ToAttributeName))
            {
                errors.Add(DomainErrors.UnknownColumn(linkedTable.LogicalName, join.ToAttributeName));
            }

            foreach (var selectedColumn in join.SelectedColumns)
            {
                if (!linkedTable.HasColumn(selectedColumn))
                {
                    errors.Add(DomainErrors.UnknownColumn(linkedTable.LogicalName, selectedColumn));
                }
            }
        }

        var scopeRegistry = BuildScopeRegistry(rootTable, linkedTablesByAlias);
        ValidateFilter(query.Filter, scopeRegistry, errors);

        foreach (var join in query.Joins)
        {
            ValidateFilter(join.Filter, scopeRegistry, errors);
        }

        foreach (var sort in query.Sorts)
        {
            if (!scopeRegistry.TryGetValue(sort.ScopeName, out var table))
            {
                errors.Add(DomainErrors.Validation(
                    "Query.Scope.Unknown",
                    $"Linked query scope '{sort.ScopeName}' does not exist."));
                continue;
            }

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

    private static IReadOnlyDictionary<string, TableDefinition> BuildScopeRegistry(
        TableDefinition rootTable,
        IReadOnlyDictionary<string, TableDefinition> linkedTablesByAlias)
    {
        var scopeRegistry = new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [rootTable.LogicalName] = rootTable
        };

        foreach (var pair in linkedTablesByAlias)
        {
            scopeRegistry[pair.Key] = pair.Value;
        }

        return scopeRegistry;
    }

    private static void ValidateFilter(
        LinkedRecordFilter? filter,
        IReadOnlyDictionary<string, TableDefinition> scopeRegistry,
        ICollection<Error> errors)
    {
        if (filter is null)
        {
            return;
        }

        foreach (var condition in filter.Conditions)
        {
            if (!scopeRegistry.TryGetValue(condition.ScopeName, out var table))
            {
                errors.Add(DomainErrors.Validation(
                    "Query.Scope.Unknown",
                    $"Linked query scope '{condition.ScopeName}' does not exist."));
                continue;
            }

            if (!table.HasColumn(condition.ColumnLogicalName))
            {
                errors.Add(DomainErrors.UnknownColumn(table.LogicalName, condition.ColumnLogicalName));
            }
        }

        foreach (var childFilter in filter.Filters)
        {
            ValidateFilter(childFilter, scopeRegistry, errors);
        }
    }
}
