namespace Dataverse.Emulator.Domain.Records;

public sealed class EntityRecord
{
    public EntityRecord(
        string tableLogicalName,
        Guid id,
        RecordValues values,
        long version = 0)
    {
        if (string.IsNullOrWhiteSpace(tableLogicalName))
        {
            throw new ArgumentException("Table logical name is required.", nameof(tableLogicalName));
        }

        TableLogicalName = tableLogicalName;
        Id = id;
        Values = values;
        Version = version;
    }

    public string TableLogicalName { get; }

    public Guid Id { get; }

    public RecordValues Values { get; }

    public long Version { get; }

    public EntityRecord ApplyChanges(IEnumerable<KeyValuePair<string, object?>> changes)
        => new(TableLogicalName, Id, Values.Merge(changes), Version + 1);
}
