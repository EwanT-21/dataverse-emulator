using Dataverse.Emulator.Domain.Common;

namespace Dataverse.Emulator.Domain.Metadata;

public sealed class TableDefinition : IAggregateRoot
{
    private readonly Dictionary<string, ColumnDefinition> columns;

    public TableDefinition(
        string logicalName,
        string entitySetName,
        string primaryIdAttribute,
        string? primaryNameAttribute,
        IEnumerable<ColumnDefinition> columns,
        IEnumerable<AlternateKeyDefinition>? alternateKeys = null)
    {
        if (string.IsNullOrWhiteSpace(logicalName))
        {
            throw new ArgumentException("Logical name is required.", nameof(logicalName));
        }

        if (string.IsNullOrWhiteSpace(entitySetName))
        {
            throw new ArgumentException("Entity set name is required.", nameof(entitySetName));
        }

        if (string.IsNullOrWhiteSpace(primaryIdAttribute))
        {
            throw new ArgumentException("Primary id attribute is required.", nameof(primaryIdAttribute));
        }

        LogicalName = logicalName;
        EntitySetName = entitySetName;
        PrimaryIdAttribute = primaryIdAttribute;
        PrimaryNameAttribute = primaryNameAttribute;

        this.columns = new Dictionary<string, ColumnDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var column in columns)
        {
            if (string.IsNullOrWhiteSpace(column.LogicalName))
            {
                throw new ArgumentException("Column logical names are required.", nameof(columns));
            }

            this.columns[column.LogicalName] = column;
        }

        if (!this.columns.ContainsKey(primaryIdAttribute))
        {
            throw new ArgumentException(
                $"Primary id attribute '{primaryIdAttribute}' must exist in the table definition.",
                nameof(columns));
        }

        if (!string.IsNullOrWhiteSpace(primaryNameAttribute) && !this.columns.ContainsKey(primaryNameAttribute))
        {
            throw new ArgumentException(
                $"Primary name attribute '{primaryNameAttribute}' must exist in the table definition.",
                nameof(columns));
        }

        AlternateKeys = (alternateKeys ?? Array.Empty<AlternateKeyDefinition>()).ToArray();
    }

    public string LogicalName { get; }

    public string EntitySetName { get; }

    public string PrimaryIdAttribute { get; }

    public string? PrimaryNameAttribute { get; }

    public IReadOnlyCollection<ColumnDefinition> Columns => columns.Values.ToArray();

    public IReadOnlyCollection<AlternateKeyDefinition> AlternateKeys { get; }

    public bool HasColumn(string logicalName) => columns.ContainsKey(logicalName);

    public ColumnDefinition? FindColumn(string logicalName)
    {
        return columns.TryGetValue(logicalName, out var column)
            ? column
            : null;
    }
}
