using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Persistence.InMemory.Metadata;

public sealed class InMemoryMetadataRepository : InMemoryRepository<TableDefinition>
{
    protected override string GetStorageKey(TableDefinition entity)
        => entity.LogicalName;

    protected override bool MatchesId<TId>(TableDefinition entity, TId id)
        => id is string logicalName
            && entity.LogicalName.Equals(logicalName, StringComparison.OrdinalIgnoreCase);
}
