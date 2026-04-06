using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using Dataverse.Emulator.Domain.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;

namespace Dataverse.Emulator.Protocols.Xrm.Metadata;

internal static class DataverseXrmMetadataMapper
{
    private const int DefaultLanguageCode = 1033;
    private const int DefaultStringLength = 200;

    public static EntityMetadata ToEntityMetadata(
        TableDefinition table,
        EntityFilters filters,
        IReadOnlyCollection<LookupRelationshipDefinition>? relationships = null)
    {
        var includeAttributes = IncludesAttributes(filters);
        var includeRelationships = IncludesRelationships(filters);
        var metadata = new EntityMetadata();
        SetProperty(metadata, nameof(EntityMetadata.MetadataId), CreateDeterministicGuid($"table:{table.LogicalName}"));
        SetProperty(metadata, nameof(EntityMetadata.LogicalName), table.LogicalName);
        SetProperty(metadata, nameof(EntityMetadata.LogicalCollectionName), table.EntitySetName);
        SetProperty(metadata, nameof(EntityMetadata.EntitySetName), table.EntitySetName);
        SetProperty(metadata, nameof(EntityMetadata.SchemaName), ToSchemaName(table.LogicalName));
        SetProperty(metadata, nameof(EntityMetadata.CollectionSchemaName), ToSchemaName(table.EntitySetName));
        SetProperty(metadata, nameof(EntityMetadata.DisplayName), CreateLabel(ToDisplayName(table.LogicalName)));
        SetProperty(metadata, nameof(EntityMetadata.DisplayCollectionName), CreateLabel(ToDisplayName(table.EntitySetName)));
        SetProperty(metadata, nameof(EntityMetadata.Description), CreateLabel($"Local emulator metadata for table '{table.LogicalName}'."));
        SetProperty(metadata, nameof(EntityMetadata.PrimaryIdAttribute), table.PrimaryIdAttribute);
        SetProperty(metadata, nameof(EntityMetadata.PrimaryNameAttribute), table.PrimaryNameAttribute);
        SetProperty(metadata, nameof(EntityMetadata.IsCustomEntity), false);
        SetProperty(metadata, nameof(EntityMetadata.IsManaged), false);
        SetProperty(metadata, nameof(EntityMetadata.IsIntersect), false);
        SetProperty(metadata, nameof(EntityMetadata.IsLogicalEntity), false);
        SetProperty(metadata, nameof(EntityMetadata.IsActivity), false);
        SetProperty(metadata, nameof(EntityMetadata.IsValidForAdvancedFind), true);
        SetProperty(metadata, nameof(EntityMetadata.ObjectTypeCode), ResolveObjectTypeCode(table));
        SetProperty(metadata, nameof(EntityMetadata.OwnershipType), OwnershipTypes.UserOwned);
        SetProperty(metadata, nameof(EntityMetadata.IsCustomizable), new BooleanManagedProperty(true));
        SetProperty(metadata, nameof(EntityMetadata.IsAuditEnabled), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.IsRenameable), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.IsMappable), new BooleanManagedProperty(true));
        SetProperty(metadata, nameof(EntityMetadata.IsValidForQueue), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.IsConnectionsEnabled), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.IsDuplicateDetectionEnabled), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.IsMailMergeEnabled), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.IsVisibleInMobile), new BooleanManagedProperty(true));
        SetProperty(metadata, nameof(EntityMetadata.IsVisibleInMobileClient), new BooleanManagedProperty(true));
        SetProperty(metadata, nameof(EntityMetadata.IsReadOnlyInMobileClient), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanCreateAttributes), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanCreateCharts), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanCreateForms), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanCreateViews), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanModifyAdditionalSettings), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanBeInManyToMany), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanBeInCustomEntityAssociation), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanBePrimaryEntityInRelationship), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanBeRelatedEntityInRelationship), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanChangeHierarchicalRelationship), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanChangeTrackingBeEnabled), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(EntityMetadata.CanEnableSyncToExternalSearchIndex), new BooleanManagedProperty(false));
        SetProperty(
            metadata,
            nameof(EntityMetadata.Attributes),
            includeAttributes
                ? table.Columns.Select(column => ToAttributeMetadata(table, column)).ToArray()
                : Array.Empty<AttributeMetadata>());
        SetProperty(
            metadata,
            nameof(EntityMetadata.OneToManyRelationships),
            includeRelationships
                ? (relationships ?? Array.Empty<LookupRelationshipDefinition>())
                .Where(relationship => relationship.ReferencedTableLogicalName.Equals(table.LogicalName, StringComparison.OrdinalIgnoreCase))
                .Select(ToRelationshipMetadata)
                .ToArray()
                : Array.Empty<OneToManyRelationshipMetadata>());
        SetProperty(
            metadata,
            nameof(EntityMetadata.ManyToOneRelationships),
            includeRelationships
                ? (relationships ?? Array.Empty<LookupRelationshipDefinition>())
                .Where(relationship => relationship.ReferencingTableLogicalName.Equals(table.LogicalName, StringComparison.OrdinalIgnoreCase))
                .Select(ToRelationshipMetadata)
                .ToArray()
                : Array.Empty<OneToManyRelationshipMetadata>());
        SetProperty(metadata, nameof(EntityMetadata.ManyToManyRelationships), Array.Empty<ManyToManyRelationshipMetadata>());
        SetProperty(metadata, nameof(EntityMetadata.Privileges), Array.Empty<SecurityPrivilegeMetadata>());
        SetProperty(metadata, nameof(EntityMetadata.Keys), Array.Empty<EntityKeyMetadata>());
        return metadata;
    }

    public static AttributeMetadata ToAttributeMetadata(TableDefinition table, ColumnDefinition column)
    {
        var metadata = CreateAttributeMetadata(column);
        SetProperty(metadata, nameof(AttributeMetadata.MetadataId), CreateDeterministicGuid($"table:{table.LogicalName}:column:{column.LogicalName}"));
        SetProperty(metadata, nameof(AttributeMetadata.EntityLogicalName), table.LogicalName);
        SetProperty(metadata, nameof(AttributeMetadata.LogicalName), column.LogicalName);
        SetProperty(metadata, nameof(AttributeMetadata.SchemaName), ToSchemaName(column.LogicalName));
        SetProperty(metadata, nameof(AttributeMetadata.DisplayName), CreateLabel(ToDisplayName(column.LogicalName)));
        SetProperty(metadata, nameof(AttributeMetadata.Description), CreateLabel($"Local emulator metadata for column '{column.LogicalName}'."));
        SetProperty(metadata, nameof(AttributeMetadata.RequiredLevel), new AttributeRequiredLevelManagedProperty(ToRequiredLevel(column.RequiredLevel)));
        SetProperty(metadata, nameof(AttributeMetadata.IsCustomAttribute), false);
        SetProperty(metadata, nameof(AttributeMetadata.IsManaged), false);
        SetProperty(metadata, nameof(AttributeMetadata.IsLogical), false);
        SetProperty(metadata, nameof(AttributeMetadata.IsPrimaryId), column.IsPrimaryId);
        SetProperty(metadata, nameof(AttributeMetadata.IsPrimaryName), column.IsPrimaryName);
        SetProperty(metadata, nameof(AttributeMetadata.IsRetrievable), true);
        SetProperty(metadata, nameof(AttributeMetadata.IsValidForRead), true);
        SetProperty(metadata, nameof(AttributeMetadata.IsValidForCreate), !column.IsPrimaryId);
        SetProperty(metadata, nameof(AttributeMetadata.IsValidForUpdate), !column.IsPrimaryId);
        SetProperty(metadata, nameof(AttributeMetadata.IsValidForForm), true);
        SetProperty(metadata, nameof(AttributeMetadata.IsValidForGrid), true);
        SetProperty(metadata, nameof(AttributeMetadata.IsFilterable), true);
        SetProperty(metadata, nameof(AttributeMetadata.IsSearchable), column.AttributeType == AttributeType.String);
        SetProperty(metadata, nameof(AttributeMetadata.IsSecured), false);
        SetProperty(metadata, nameof(AttributeMetadata.IsRequiredForForm), column.RequiredLevel != RequiredLevel.None);
        SetProperty(metadata, nameof(AttributeMetadata.IsAuditEnabled), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(AttributeMetadata.IsCustomizable), new BooleanManagedProperty(true));
        SetProperty(metadata, nameof(AttributeMetadata.IsRenameable), new BooleanManagedProperty(!column.IsPrimaryId));
        SetProperty(metadata, nameof(AttributeMetadata.IsSortableEnabled), new BooleanManagedProperty(true));
        SetProperty(metadata, nameof(AttributeMetadata.IsValidForAdvancedFind), new BooleanManagedProperty(true));
        SetProperty(metadata, nameof(AttributeMetadata.IsGlobalFilterEnabled), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(AttributeMetadata.CanModifyAdditionalSettings), new BooleanManagedProperty(false));
        SetProperty(metadata, nameof(AttributeMetadata.AttributeType), ToAttributeTypeCode(column.AttributeType));

        ApplyAttributeSpecificSettings(metadata, column);
        return metadata;
    }

    public static Guid CreateTableMetadataId(string logicalName)
        => CreateDeterministicGuid($"table:{logicalName}");

    public static Guid CreateColumnMetadataId(string tableLogicalName, string columnLogicalName)
        => CreateDeterministicGuid($"table:{tableLogicalName}:column:{columnLogicalName}");

    public static Guid CreateRelationshipMetadataId(string schemaName)
        => CreateDeterministicGuid($"relationship:{schemaName}");

    public static OneToManyRelationshipMetadata ToRelationshipMetadata(
        LookupRelationshipDefinition relationship)
    {
        var metadata = new OneToManyRelationshipMetadata();
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.MetadataId), CreateRelationshipMetadataId(relationship.SchemaName));
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.SchemaName), relationship.SchemaName);
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.ReferencedEntity), relationship.ReferencedTableLogicalName);
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.ReferencedAttribute), relationship.ReferencedAttributeLogicalName);
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.ReferencingEntity), relationship.ReferencingTableLogicalName);
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.ReferencingAttribute), relationship.ReferencingAttributeLogicalName);
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.IsCustomRelationship), false);
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.IsManaged), false);
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.IsHierarchical), false);
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.AssociatedMenuConfiguration), new AssociatedMenuConfiguration());
        SetProperty(metadata, nameof(OneToManyRelationshipMetadata.CascadeConfiguration), new CascadeConfiguration());
        return metadata;
    }

    private static AttributeMetadata CreateAttributeMetadata(ColumnDefinition column)
        => column.AttributeType.Name switch
        {
            nameof(AttributeType.UniqueIdentifier) => new UniqueIdentifierAttributeMetadata(),
            nameof(AttributeType.String) => new StringAttributeMetadata(),
            nameof(AttributeType.Integer) => new IntegerAttributeMetadata(),
            nameof(AttributeType.Decimal) => new DecimalAttributeMetadata(),
            nameof(AttributeType.Boolean) => new BooleanAttributeMetadata(CreateBooleanOptionSet()),
            nameof(AttributeType.DateTime) => new DateTimeAttributeMetadata(DateTimeFormat.DateAndTime),
            nameof(AttributeType.Lookup) => new LookupAttributeMetadata(),
            _ => new StringAttributeMetadata()
        };

    private static void ApplyAttributeSpecificSettings(AttributeMetadata metadata, ColumnDefinition column)
    {
        switch (metadata)
        {
            case StringAttributeMetadata stringMetadata:
                SetProperty(stringMetadata, nameof(StringAttributeMetadata.MaxLength), column.IsPrimaryName ? 160 : DefaultStringLength);
                SetProperty(stringMetadata, nameof(StringAttributeMetadata.Format), StringFormat.Text);
                break;

            case IntegerAttributeMetadata integerMetadata:
                SetProperty(integerMetadata, nameof(IntegerAttributeMetadata.Format), IntegerFormat.None);
                break;

            case DecimalAttributeMetadata decimalMetadata:
                SetProperty(decimalMetadata, nameof(DecimalAttributeMetadata.Precision), 2);
                SetProperty(decimalMetadata, nameof(DecimalAttributeMetadata.MinValue), decimal.MinValue);
                SetProperty(decimalMetadata, nameof(DecimalAttributeMetadata.MaxValue), decimal.MaxValue);
                break;

            case BooleanAttributeMetadata booleanMetadata:
                SetProperty(booleanMetadata, nameof(BooleanAttributeMetadata.OptionSet), CreateBooleanOptionSet());
                break;

            case DateTimeAttributeMetadata dateTimeMetadata:
                SetProperty(dateTimeMetadata, nameof(DateTimeAttributeMetadata.Format), DateTimeFormat.DateAndTime);
                SetProperty(dateTimeMetadata, nameof(DateTimeAttributeMetadata.DateTimeBehavior), DateTimeBehavior.UserLocal);
                break;

            case LookupAttributeMetadata lookupMetadata:
                SetProperty(
                    lookupMetadata,
                    nameof(LookupAttributeMetadata.Targets),
                    string.IsNullOrWhiteSpace(column.LookupTargetTable)
                        ? Array.Empty<string>()
                        : [column.LookupTargetTable]);
                break;
        }
    }

    private static bool IncludesAttributes(EntityFilters filters)
        => filters == EntityFilters.All
            || filters == EntityFilters.Attributes
            || filters == (EntityFilters.Entity | EntityFilters.Attributes)
            || filters == (EntityFilters.Default | EntityFilters.Attributes);

    private static bool IncludesRelationships(EntityFilters filters)
        => filters == EntityFilters.All
            || filters == EntityFilters.Relationships
            || filters == (EntityFilters.Entity | EntityFilters.Relationships)
            || filters == (EntityFilters.Default | EntityFilters.Relationships);

    private static Label CreateLabel(string value)
        => new(value, DefaultLanguageCode);

    private static AttributeRequiredLevel ToRequiredLevel(RequiredLevel requiredLevel)
        => requiredLevel.Name switch
        {
            nameof(RequiredLevel.SystemRequired) => AttributeRequiredLevel.SystemRequired,
            nameof(RequiredLevel.ApplicationRequired) => AttributeRequiredLevel.ApplicationRequired,
            _ => AttributeRequiredLevel.None
        };

    private static AttributeTypeCode ToAttributeTypeCode(AttributeType attributeType)
        => attributeType.Name switch
        {
            nameof(AttributeType.UniqueIdentifier) => AttributeTypeCode.Uniqueidentifier,
            nameof(AttributeType.String) => AttributeTypeCode.String,
            nameof(AttributeType.Integer) => AttributeTypeCode.Integer,
            nameof(AttributeType.Decimal) => AttributeTypeCode.Decimal,
            nameof(AttributeType.Boolean) => AttributeTypeCode.Boolean,
            nameof(AttributeType.DateTime) => AttributeTypeCode.DateTime,
            nameof(AttributeType.Lookup) => AttributeTypeCode.Lookup,
            _ => AttributeTypeCode.String
        };

    private static BooleanOptionSetMetadata CreateBooleanOptionSet()
        => new(
            new OptionMetadata(CreateLabel("Yes"), 1),
            new OptionMetadata(CreateLabel("No"), 0));

    private static int ResolveObjectTypeCode(TableDefinition table)
        => table.LogicalName.Equals("account", StringComparison.OrdinalIgnoreCase)
            ? 1
            : Math.Abs(BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(table.LogicalName)), 0));

    private static Guid CreateDeterministicGuid(string value)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash);
    }

    private static void SetProperty(object target, string propertyName, object? value)
    {
        var property = target.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property?.SetValue(target, value);
    }

    private static string ToSchemaName(string value)
        => string.Concat(
            value.Split(['_', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => char.ToUpperInvariant(segment[0]) + segment[1..]));

    private static string ToDisplayName(string value)
    {
        var normalized = value.Replace('_', ' ');
        var builder = new StringBuilder(normalized.Length * 2);

        for (var index = 0; index < normalized.Length; index++)
        {
            var current = normalized[index];
            if (index > 0
                && char.IsUpper(current)
                && normalized[index - 1] != ' '
                && char.IsLower(normalized[index - 1]))
            {
                builder.Append(' ');
            }

            if (index == 0 || normalized[index - 1] == ' ')
            {
                builder.Append(char.ToUpperInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString().Trim();
    }
}
