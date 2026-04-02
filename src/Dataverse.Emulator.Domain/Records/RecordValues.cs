using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Records;

public sealed class RecordValues
{
    private readonly Dictionary<string, object?> items;

    internal RecordValues(IEnumerable<KeyValuePair<string, object?>> items)
    {
        this.items = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            this.items[item.Key] = item.Value;
        }
    }

    public static RecordValues Empty { get; } = new(Array.Empty<KeyValuePair<string, object?>>());

    public IReadOnlyDictionary<string, object?> Items => items;

    public object? this[string logicalName] => items[logicalName];

    public bool Contains(string logicalName) => items.ContainsKey(logicalName);

    public bool TryGetValue(string logicalName, out object? value) => items.TryGetValue(logicalName, out value);

    public static ErrorOr<RecordValues> Create(IEnumerable<KeyValuePair<string, object?>> items)
    {
        if (items is null)
        {
            return DomainErrors.Validation(
                "Records.Values.Required",
                "Record values are required.");
        }

        var valueArray = items.ToArray();
        if (valueArray.Any(item => string.IsNullOrWhiteSpace(item.Key)))
        {
            return DomainErrors.Validation(
                "Records.Values.KeyRequired",
                "Record value keys must be non-empty.");
        }

        return new RecordValues(valueArray);
    }

    public RecordValues Merge(IEnumerable<KeyValuePair<string, object?>> changes)
    {
        var merged = new Dictionary<string, object?>(items, StringComparer.OrdinalIgnoreCase);

        foreach (var change in changes)
        {
            merged[change.Key] = change.Value;
        }

        return new RecordValues(merged);
    }

    internal RecordValues Select(IReadOnlyList<string> logicalNames)
    {
        var values = logicalNames
            .Where(Contains)
            .Select(logicalName => new KeyValuePair<string, object?>(logicalName, items[logicalName]));

        return new RecordValues(values);
    }
}
