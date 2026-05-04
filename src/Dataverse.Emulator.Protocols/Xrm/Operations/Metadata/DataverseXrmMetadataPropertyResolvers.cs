using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Protocols.Xrm.Metadata;
using ErrorOr;

namespace Dataverse.Emulator.Protocols.Xrm.Operations.Metadata;

internal static class DataverseXrmMetadataPropertyResolvers
{
    public static ErrorOr<object?> ResolveTableMetadataPropertyValue(
        TableDefinition table,
        string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return DataverseXrmErrors.ParameterRequired("MetadataConditionExpression.PropertyName");
        }

        return propertyName switch
        {
            "LogicalName" => table.LogicalName,
            "PrimaryIdAttribute" => table.PrimaryIdAttribute,
            "PrimaryNameAttribute" => table.PrimaryNameAttribute,
            "LogicalCollectionName" => table.EntitySetName,
            "EntitySetName" => table.EntitySetName,
            "MetadataId" => DataverseXrmMetadataMapper.CreateTableMetadataId(table.LogicalName),
            _ => DataverseXrmErrors.UnsupportedOperation(
                $"RetrieveMetadataChanges metadata property '{propertyName}'")
        };
    }

    public static ErrorOr<object?> ResolveColumnMetadataPropertyValue(
        (TableDefinition Table, ColumnDefinition Column) candidate,
        string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return DataverseXrmErrors.ParameterRequired("MetadataConditionExpression.PropertyName");
        }

        var table = candidate.Table;
        var column = candidate.Column;

        return propertyName switch
        {
            "LogicalName" => column.LogicalName,
            "SchemaName" => ToSchemaName(column.LogicalName),
            "MetadataId" => DataverseXrmMetadataMapper.CreateColumnMetadataId(table.LogicalName, column.LogicalName),
            "EntityLogicalName" => table.LogicalName,
            "AttributeType" => column.AttributeType.Name,
            "RequiredLevel" => column.RequiredLevel.Name,
            "IsPrimaryId" => column.IsPrimaryId,
            "IsPrimaryName" => column.IsPrimaryName,
            _ => DataverseXrmErrors.UnsupportedOperation(
                $"RetrieveMetadataChanges attribute metadata property '{propertyName}'")
        };
    }

    public static ErrorOr<object?> ResolveRelationshipMetadataPropertyValue(
        LookupRelationshipDefinition relationship,
        string? propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return DataverseXrmErrors.ParameterRequired("MetadataConditionExpression.PropertyName");
        }

        return propertyName switch
        {
            "SchemaName" => relationship.SchemaName,
            "MetadataId" => DataverseXrmMetadataMapper.CreateRelationshipMetadataId(relationship.SchemaName),
            "ReferencedEntity" => relationship.ReferencedTableLogicalName,
            "ReferencedAttribute" => relationship.ReferencedAttributeLogicalName,
            "ReferencingEntity" => relationship.ReferencingTableLogicalName,
            "ReferencingAttribute" => relationship.ReferencingAttributeLogicalName,
            _ => DataverseXrmErrors.UnsupportedOperation(
                $"RetrieveMetadataChanges relationship metadata property '{propertyName}'")
        };
    }

    private static string ToSchemaName(string value)
        => string.Concat(
            value.Split(['_', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));
}
