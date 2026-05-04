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
            "SchemaName" => DataverseXrmMetadataMapper.ToSchemaName(table.LogicalName),
            "CollectionSchemaName" => DataverseXrmMetadataMapper.ToSchemaName(table.EntitySetName),
            "DisplayName" => DataverseXrmMetadataMapper.ToDisplayName(table.LogicalName),
            "DisplayCollectionName" => DataverseXrmMetadataMapper.ToDisplayName(table.EntitySetName),
            "Description" => $"Local emulator metadata for table '{table.LogicalName}'.",
            "PrimaryIdAttribute" => table.PrimaryIdAttribute,
            "PrimaryNameAttribute" => table.PrimaryNameAttribute,
            "LogicalCollectionName" => table.EntitySetName,
            "EntitySetName" => table.EntitySetName,
            "MetadataId" => DataverseXrmMetadataMapper.CreateTableMetadataId(table.LogicalName),
            "ObjectTypeCode" => DataverseXrmMetadataMapper.ResolveObjectTypeCode(table),
            "OwnershipType" => "UserOwned",
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
            "SchemaName" => DataverseXrmMetadataMapper.ToSchemaName(column.LogicalName),
            "DisplayName" => DataverseXrmMetadataMapper.ToDisplayName(column.LogicalName),
            "Description" => $"Local emulator metadata for column '{column.LogicalName}'.",
            "MetadataId" => DataverseXrmMetadataMapper.CreateColumnMetadataId(table.LogicalName, column.LogicalName),
            "EntityLogicalName" => table.LogicalName,
            "AttributeType" => column.AttributeType.Name,
            "RequiredLevel" => column.RequiredLevel.Name,
            "IsPrimaryId" => column.IsPrimaryId,
            "IsPrimaryName" => column.IsPrimaryName,
            "IsCustomAttribute" => false,
            "IsManaged" => false,
            "IsLogical" => false,
            "IsRetrievable" => true,
            "IsValidForRead" => true,
            "IsValidForCreate" => !column.IsPrimaryId,
            "IsValidForUpdate" => !column.IsPrimaryId,
            "IsValidForForm" => true,
            "IsValidForGrid" => true,
            "IsFilterable" => true,
            "IsSearchable" => column.AttributeType == AttributeType.String,
            "IsSecured" => false,
            "IsRequiredForForm" => column.RequiredLevel != RequiredLevel.None,
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
            "IsCustomRelationship" => false,
            "IsManaged" => false,
            "IsHierarchical" => false,
            _ => DataverseXrmErrors.UnsupportedOperation(
                $"RetrieveMetadataChanges relationship metadata property '{propertyName}'")
        };
    }
}
