namespace Dataverse.Emulator.Domain.Metadata;

public sealed record ColumnDefinition(
    string LogicalName,
    AttributeType AttributeType,
    RequiredLevel RequiredLevel,
    bool IsPrimaryId = false,
    bool IsPrimaryName = false,
    string? LookupTargetTable = null);
