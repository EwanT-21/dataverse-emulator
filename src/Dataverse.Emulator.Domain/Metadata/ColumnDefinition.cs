using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Metadata;

public sealed record ColumnDefinition
{
    internal ColumnDefinition(
        string logicalName,
        AttributeType attributeType,
        RequiredLevel requiredLevel,
        bool isPrimaryId = false,
        bool isPrimaryName = false,
        string? lookupTargetTable = null,
        string? lookupRelationshipName = null)
    {
        LogicalName = logicalName;
        AttributeType = attributeType;
        RequiredLevel = requiredLevel;
        IsPrimaryId = isPrimaryId;
        IsPrimaryName = isPrimaryName;
        LookupTargetTable = lookupTargetTable;
        LookupRelationshipName = lookupRelationshipName;
    }

    public string LogicalName { get; }

    public AttributeType AttributeType { get; }

    public RequiredLevel RequiredLevel { get; }

    public bool IsPrimaryId { get; }

    public bool IsPrimaryName { get; }

    public string? LookupTargetTable { get; }

    public string? LookupRelationshipName { get; }

    public static ErrorOr<ColumnDefinition> Create(
        string logicalName,
        AttributeType attributeType,
        RequiredLevel requiredLevel,
        bool isPrimaryId = false,
        bool isPrimaryName = false,
        string? lookupTargetTable = null,
        string? lookupRelationshipName = null)
    {
        if (string.IsNullOrWhiteSpace(logicalName))
        {
            return DomainErrors.Validation(
                "Metadata.Column.LogicalNameRequired",
                "Column logical name is required.");
        }

        if (attributeType != AttributeType.Lookup
            && (!string.IsNullOrWhiteSpace(lookupTargetTable) || !string.IsNullOrWhiteSpace(lookupRelationshipName)))
        {
            return DomainErrors.Validation(
                "Metadata.Column.LookupConfigurationInvalid",
                $"Column '{logicalName}' can only declare lookup metadata when its attribute type is Lookup.");
        }

        if (!string.IsNullOrWhiteSpace(lookupRelationshipName)
            && string.IsNullOrWhiteSpace(lookupTargetTable))
        {
            return DomainErrors.Validation(
                "Metadata.Column.LookupTargetTableRequired",
                $"Column '{logicalName}' requires a lookup target table when a lookup relationship name is provided.");
        }

        return new ColumnDefinition(
            logicalName,
            attributeType,
            requiredLevel,
            isPrimaryId,
            isPrimaryName,
            lookupTargetTable,
            lookupRelationshipName);
    }
}
