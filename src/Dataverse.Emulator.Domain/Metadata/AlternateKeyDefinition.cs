namespace Dataverse.Emulator.Domain.Metadata;

public sealed record AlternateKeyDefinition(
    string LogicalName,
    IReadOnlyList<string> ColumnLogicalNames);
