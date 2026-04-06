using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Services;

public sealed class LookupRelationshipDefinitionService
{
    public LookupRelationshipDefinition[] List(IReadOnlyCollection<TableDefinition> tables)
    {
        ArgumentNullException.ThrowIfNull(tables);

        var tablesByLogicalName = tables.ToDictionary(
            table => table.LogicalName,
            StringComparer.OrdinalIgnoreCase);
        var relationships = new List<LookupRelationshipDefinition>();

        foreach (var referencingTable in tables)
        {
            foreach (var column in referencingTable.Columns.Where(column =>
                         column.AttributeType == AttributeType.Lookup
                         && !string.IsNullOrWhiteSpace(column.LookupTargetTable)))
            {
                if (!tablesByLogicalName.TryGetValue(column.LookupTargetTable!, out var referencedTable))
                {
                    continue;
                }

                relationships.Add(new LookupRelationshipDefinition(
                    ResolveSchemaName(referencingTable, column, referencedTable),
                    referencedTable.LogicalName,
                    referencedTable.PrimaryIdAttribute,
                    referencingTable.LogicalName,
                    column.LogicalName));
            }
        }

        return relationships
            .OrderBy(relationship => relationship.SchemaName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public ErrorOr<LookupRelationshipDefinition> Resolve(
        IReadOnlyCollection<TableDefinition> tables,
        string schemaName)
    {
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            return DomainErrors.Validation(
                "Metadata.Relationship.SchemaNameRequired",
                "Relationship schema name is required.");
        }

        var relationship = List(tables).SingleOrDefault(candidate =>
            candidate.SchemaName.Equals(schemaName, StringComparison.OrdinalIgnoreCase));

        return relationship is not null
            ? relationship
            : DomainErrors.Validation(
                "Metadata.Relationship.Unknown",
                $"Relationship '{schemaName}' is not known to the local Dataverse emulator.");
    }

    private static string ResolveSchemaName(
        TableDefinition referencingTable,
        ColumnDefinition column,
        TableDefinition referencedTable)
        => !string.IsNullOrWhiteSpace(column.LookupRelationshipName)
            ? column.LookupRelationshipName
            : $"{referencingTable.LogicalName}_{column.LogicalName}_{referencedTable.LogicalName}";
}
