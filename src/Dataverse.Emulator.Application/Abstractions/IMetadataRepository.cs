using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Application.Abstractions;

public interface IMetadataRepository
{
    ValueTask<TableDefinition?> GetTableAsync(
        string logicalName,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyCollection<TableDefinition>> GetTablesAsync(
        CancellationToken cancellationToken = default);

    ValueTask StoreAsync(
        TableDefinition table,
        CancellationToken cancellationToken = default);

    ValueTask ResetAsync(
        IEnumerable<TableDefinition> tables,
        CancellationToken cancellationToken = default);
}
