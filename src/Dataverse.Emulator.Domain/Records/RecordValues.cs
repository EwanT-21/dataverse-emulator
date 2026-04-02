namespace Dataverse.Emulator.Domain.Records;

public sealed class RecordValues
{
    private readonly Dictionary<string, object?> items;

    public RecordValues()
        : this(Array.Empty<KeyValuePair<string, object?>>())
    {
    }

    public RecordValues(IEnumerable<KeyValuePair<string, object?>> items)
    {
        this.items = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            this.items[item.Key] = item.Value;
        }
    }

    public IReadOnlyDictionary<string, object?> Items => items;

    public object? this[string logicalName] => items[logicalName];

    public bool Contains(string logicalName) => items.ContainsKey(logicalName);

    public bool TryGetValue(string logicalName, out object? value) => items.TryGetValue(logicalName, out value);

    public RecordValues Merge(IEnumerable<KeyValuePair<string, object?>> changes)
    {
        var merged = new Dictionary<string, object?>(items, StringComparer.OrdinalIgnoreCase);

        foreach (var change in changes)
        {
            merged[change.Key] = change.Value;
        }

        return new RecordValues(merged);
    }
}
