using System.Security.Cryptography;
using System.Text;
using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Protocols.Xrm.Operations.Metadata;

internal static class DataverseXrmMetadataServerVersionStamp
{
    public static string Create(
        IReadOnlyCollection<TableDefinition> tables,
        IReadOnlyCollection<LookupRelationshipDefinition> relationships)
    {
        var builder = new StringBuilder();

        foreach (var table in tables.OrderBy(table => table.LogicalName, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(table.LogicalName)
                .Append('|')
                .Append(table.EntitySetName)
                .Append('|')
                .Append(table.PrimaryIdAttribute)
                .Append('|')
                .Append(table.PrimaryNameAttribute)
                .Append(';');

            foreach (var column in table.Columns.OrderBy(column => column.LogicalName, StringComparer.OrdinalIgnoreCase))
            {
                builder.Append(column.LogicalName)
                    .Append(':')
                    .Append(column.AttributeType.Name)
                    .Append(':')
                    .Append(column.RequiredLevel.Name)
                    .Append(';');
            }
        }

        foreach (var relationship in relationships.OrderBy(relationship => relationship.SchemaName, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append(relationship.SchemaName)
                .Append('|')
                .Append(relationship.ReferencedTableLogicalName)
                .Append('|')
                .Append(relationship.ReferencingTableLogicalName)
                .Append(';');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }
}
