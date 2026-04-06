namespace Dataverse.Emulator.Domain.Metadata;

public sealed record LookupRelationshipDefinition(
    string SchemaName,
    string ReferencedTableLogicalName,
    string ReferencedAttributeLogicalName,
    string ReferencingTableLogicalName,
    string ReferencingAttributeLogicalName);
