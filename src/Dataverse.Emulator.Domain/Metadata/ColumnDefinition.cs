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
        string? lookupTargetTable = null)
    {
        LogicalName = logicalName;
        AttributeType = attributeType;
        RequiredLevel = requiredLevel;
        IsPrimaryId = isPrimaryId;
        IsPrimaryName = isPrimaryName;
        LookupTargetTable = lookupTargetTable;
    }

    public string LogicalName { get; }

    public AttributeType AttributeType { get; }

    public RequiredLevel RequiredLevel { get; }

    public bool IsPrimaryId { get; }

    public bool IsPrimaryName { get; }

    public string? LookupTargetTable { get; }

    public static ErrorOr<ColumnDefinition> Create(
        string logicalName,
        AttributeType attributeType,
        RequiredLevel requiredLevel,
        bool isPrimaryId = false,
        bool isPrimaryName = false,
        string? lookupTargetTable = null)
    {
        if (string.IsNullOrWhiteSpace(logicalName))
        {
            return DomainErrors.Validation(
                "Metadata.Column.LogicalNameRequired",
                "Column logical name is required.");
        }

        return new ColumnDefinition(
            logicalName,
            attributeType,
            requiredLevel,
            isPrimaryId,
            isPrimaryName,
            lookupTargetTable);
    }
}
