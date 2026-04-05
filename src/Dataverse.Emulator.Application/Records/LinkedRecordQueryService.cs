using Ardalis.Specification;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;

namespace Dataverse.Emulator.Application.Records;

public sealed class LinkedRecordQueryService(
    IReadRepository<TableDefinition> tableRepository,
    IRecordQueryService recordQueryService,
    LinkedRecordQueryValidationService linkedRecordQueryValidationService,
    LinkedRecordQueryExecutionService linkedRecordQueryExecutionService)
{
    public async ValueTask<ErrorOr<PageResult<LinkedEntityRecord>>> ListAsync(
        LinkedRecordQuery query,
        CancellationToken cancellationToken = default)
    {
        var rootTable = await tableRepository.SingleOrDefaultAsync(
            new TableByLogicalNameSpecification(query.RootTableLogicalName),
            cancellationToken);
        if (rootTable is null)
        {
            return DomainErrors.UnknownTable(query.RootTableLogicalName);
        }

        var linkedTablesByAliasResult = await LoadLinkedTablesByAliasAsync(query, cancellationToken);
        if (linkedTablesByAliasResult.IsError)
        {
            return linkedTablesByAliasResult.Errors;
        }

        var errors = linkedRecordQueryValidationService.Validate(
            rootTable,
            linkedTablesByAliasResult.Value,
            query);
        if (errors.Count > 0)
        {
            return errors.ToList();
        }

        var rowsByTableResult = await LoadRowsByTableAsync(
            rootTable,
            linkedTablesByAliasResult.Value.Values,
            cancellationToken);
        if (rowsByTableResult.IsError)
        {
            return rowsByTableResult.Errors;
        }

        return linkedRecordQueryExecutionService.Execute(
            query,
            rootTable,
            rowsByTableResult.Value);
    }

    private async Task<ErrorOr<IReadOnlyDictionary<string, TableDefinition>>> LoadLinkedTablesByAliasAsync(
        LinkedRecordQuery query,
        CancellationToken cancellationToken)
    {
        var tablesByAlias = new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var join in query.Joins)
        {
            if (tablesByAlias.ContainsKey(join.Alias))
            {
                continue;
            }

            var table = await tableRepository.SingleOrDefaultAsync(
                new TableByLogicalNameSpecification(join.TableLogicalName),
                cancellationToken);
            if (table is null)
            {
                return DomainErrors.UnknownTable(join.TableLogicalName);
            }

            tablesByAlias[join.Alias] = table;
        }

        return tablesByAlias;
    }

    private async Task<ErrorOr<IReadOnlyDictionary<string, IReadOnlyList<EntityRecord>>>> LoadRowsByTableAsync(
        TableDefinition rootTable,
        IEnumerable<TableDefinition> linkedTables,
        CancellationToken cancellationToken)
    {
        var rowsByTable = new Dictionary<string, IReadOnlyList<EntityRecord>>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in linkedTables
                     .Prepend(rootTable)
                     .GroupBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First()))
        {
            var queryResult = RecordQuery.Create(table.LogicalName);
            if (queryResult.IsError)
            {
                return queryResult.Errors;
            }

            var rowsResult = await recordQueryService.ListAsync(queryResult.Value, cancellationToken);
            rowsByTable[table.LogicalName] = rowsResult.Items;
        }

        return rowsByTable;
    }
}
