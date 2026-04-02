using System.Collections.Concurrent;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Persistence.InMemory.Metadata;

public sealed class InMemoryMetadataRepository : IMetadataRepository
{
    private readonly ConcurrentDictionary<string, TableDefinition> tables = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask<TableDefinition?> GetTableAsync(
        string logicalName,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            tables.TryGetValue(logicalName, out var table)
                ? table
                : null);
    }

    public ValueTask<IReadOnlyCollection<TableDefinition>> GetTablesAsync(
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<IReadOnlyCollection<TableDefinition>>(tables.Values.ToArray());
    }

    public ValueTask StoreAsync(
        TableDefinition table,
        CancellationToken cancellationToken = default)
    {
        tables[table.LogicalName] = table;
        return ValueTask.CompletedTask;
    }

    public ValueTask ResetAsync(
        IEnumerable<TableDefinition> tables,
        CancellationToken cancellationToken = default)
    {
        this.tables.Clear();

        foreach (var table in tables)
        {
            this.tables[table.LogicalName] = table;
        }

        return ValueTask.CompletedTask;
    }
}
