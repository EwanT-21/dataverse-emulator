using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Metadata;

public sealed class TableDefinition : AggregateRoot
{
    private readonly Dictionary<string, ColumnDefinition> columns;

    internal TableDefinition(
        Guid aggregateId,
        string logicalName,
        string entitySetName,
        string primaryIdAttribute,
        string? primaryNameAttribute,
        IEnumerable<ColumnDefinition> columns,
        IEnumerable<AlternateKeyDefinition>? alternateKeys = null)
        : base(aggregateId)
    {
        LogicalName = logicalName;
        EntitySetName = entitySetName;
        PrimaryIdAttribute = primaryIdAttribute;
        PrimaryNameAttribute = primaryNameAttribute;

        this.columns = new Dictionary<string, ColumnDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in columns)
        {
            this.columns[column.LogicalName] = column;
        }

        AlternateKeys = (alternateKeys ?? Array.Empty<AlternateKeyDefinition>()).ToArray();
    }

    public string LogicalName { get; }

    public string EntitySetName { get; }

    public string PrimaryIdAttribute { get; }

    public string? PrimaryNameAttribute { get; }

    public IReadOnlyCollection<ColumnDefinition> Columns => columns.Values.ToArray();

    public IReadOnlyCollection<AlternateKeyDefinition> AlternateKeys { get; }

    public static ErrorOr<TableDefinition> Create(
        string logicalName,
        string entitySetName,
        string primaryIdAttribute,
        string? primaryNameAttribute,
        IEnumerable<ColumnDefinition> columns,
        IEnumerable<AlternateKeyDefinition>? alternateKeys = null)
    {
        if (columns is null)
        {
            return DomainErrors.Validation(
                "Metadata.Table.ColumnsRequired",
                "Columns are required.");
        }

        if (string.IsNullOrWhiteSpace(logicalName))
        {
            return DomainErrors.Validation(
                "Metadata.Table.LogicalNameRequired",
                "Logical name is required.");
        }

        if (string.IsNullOrWhiteSpace(entitySetName))
        {
            return DomainErrors.Validation(
                "Metadata.Table.EntitySetNameRequired",
                "Entity set name is required.");
        }

        if (string.IsNullOrWhiteSpace(primaryIdAttribute))
        {
            return DomainErrors.Validation(
                "Metadata.Table.PrimaryIdRequired",
                "Primary id attribute is required.");
        }

        var columnArray = columns.ToArray();
        if (columnArray.Length == 0)
        {
            return DomainErrors.Validation(
                "Metadata.Table.ColumnsRequired",
                "At least one column is required.");
        }

        var columnNames = new HashSet<string>(
            columnArray.Select(column => column.LogicalName),
            StringComparer.OrdinalIgnoreCase);

        if (!columnNames.Contains(primaryIdAttribute))
        {
            return DomainErrors.Validation(
                "Metadata.Table.PrimaryIdColumnMissing",
                $"Primary id attribute '{primaryIdAttribute}' must exist in the table definition.");
        }

        if (!string.IsNullOrWhiteSpace(primaryNameAttribute) && !columnNames.Contains(primaryNameAttribute))
        {
            return DomainErrors.Validation(
                "Metadata.Table.PrimaryNameColumnMissing",
                $"Primary name attribute '{primaryNameAttribute}' must exist in the table definition.");
        }

        var alternateKeyArray = (alternateKeys ?? Array.Empty<AlternateKeyDefinition>()).ToArray();
        foreach (var alternateKey in alternateKeyArray)
        {
            if (alternateKey.ColumnLogicalNames.Any(columnName => !columnNames.Contains(columnName)))
            {
                return DomainErrors.Validation(
                    "Metadata.Table.AlternateKeyColumnMissing",
                    $"Alternate key '{alternateKey.LogicalName}' references one or more unknown columns.");
            }
        }

        return new TableDefinition(
            Guid.NewGuid(),
            logicalName,
            entitySetName,
            primaryIdAttribute,
            primaryNameAttribute,
            columnArray,
            alternateKeyArray);
    }

    public bool HasColumn(string logicalName) => columns.ContainsKey(logicalName);

    public ColumnDefinition? FindColumn(string logicalName)
    {
        return columns.TryGetValue(logicalName, out var column)
            ? column
            : null;
    }
}
