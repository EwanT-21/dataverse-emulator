using System.Collections.Concurrent;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Persistence.InMemory.Records;

internal sealed class InMemoryTableStore
{
    private readonly ConcurrentDictionary<Guid, EntityRecord> records = new();

    public IReadOnlyCollection<EntityRecord> Snapshot() => records.Values.ToArray();

    public EntityRecord? Get(Guid id)
    {
        return records.TryGetValue(id, out var record)
            ? record
            : null;
    }

    public void Create(EntityRecord record)
    {
        if (!records.TryAdd(record.Id, record))
        {
            throw new InvalidOperationException($"Record '{record.Id}' already exists.");
        }
    }

    public void Update(EntityRecord record)
    {
        if (!records.ContainsKey(record.Id))
        {
            throw new InvalidOperationException($"Record '{record.Id}' does not exist.");
        }

        records[record.Id] = record;
    }

    public bool Delete(Guid id) => records.TryRemove(id, out _);

    public void Reset(IEnumerable<EntityRecord> records)
    {
        this.records.Clear();

        foreach (var record in records)
        {
            this.records[record.Id] = record;
        }
    }
}
