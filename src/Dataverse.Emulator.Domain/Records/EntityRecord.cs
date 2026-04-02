using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Records;

public sealed class EntityRecord : AggregateRoot
{
    internal EntityRecord(
        Guid aggregateId,
        string tableLogicalName,
        Guid id,
        RecordValues values,
        long version = 0)
        : base(aggregateId)
    {
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
        => new(AggregateId, TableLogicalName, Id, Values.Merge(changes), Version + 1);

    public EntityRecord Project(IReadOnlyList<string> selectedColumns)
        => selectedColumns.Count == 0
            ? this
            : new EntityRecord(AggregateId, TableLogicalName, Id, Values.Select(selectedColumns), Version);

    public static ErrorOr<EntityRecord> Create(
        string tableLogicalName,
        Guid id,
        RecordValues values,
        long version = 0)
    {
        if (values is null)
        {
            return DomainErrors.Validation(
                "Records.Row.ValuesRequired",
                "Record values are required.");
        }

        if (string.IsNullOrWhiteSpace(tableLogicalName))
        {
            return DomainErrors.Validation(
                "Records.Row.TableLogicalNameRequired",
                "Table logical name is required.");
        }

        if (id == Guid.Empty)
        {
            return DomainErrors.Validation(
                "Records.Row.IdRequired",
                "Row id is required.");
        }

        if (version < 0)
        {
            return DomainErrors.Validation(
                "Records.Row.VersionInvalid",
                "Row version cannot be negative.");
        }

        return new EntityRecord(Guid.NewGuid(), tableLogicalName, id, values, version);
    }
}
