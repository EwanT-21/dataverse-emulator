namespace Dataverse.Emulator.Application.Seeding;

public sealed record SeedScenarioSnapshotDocument(
    string SchemaVersion,
    DateTimeOffset CapturedAtUtc,
    IReadOnlyList<SeedScenarioSnapshotTableDocument> Tables,
    IReadOnlyList<SeedScenarioSnapshotRecordDocument> Records);

public sealed record SeedScenarioSnapshotTableDocument(
    string LogicalName,
    string EntitySetName,
    string PrimaryIdAttribute,
    string? PrimaryNameAttribute,
    IReadOnlyList<SeedScenarioSnapshotColumnDocument> Columns,
    IReadOnlyList<SeedScenarioSnapshotAlternateKeyDocument> AlternateKeys);

public sealed record SeedScenarioSnapshotColumnDocument(
    string LogicalName,
    string AttributeType,
    string RequiredLevel,
    bool IsPrimaryId,
    bool IsPrimaryName,
    string? LookupTargetTable,
    string? LookupRelationshipName);

public sealed record SeedScenarioSnapshotAlternateKeyDocument(
    string LogicalName,
    IReadOnlyList<string> ColumnLogicalNames);

public sealed record SeedScenarioSnapshotRecordDocument(
    string TableLogicalName,
    Guid Id,
    long Version,
    IReadOnlyDictionary<string, SeedScenarioSnapshotValueDocument> Values);

public sealed record SeedScenarioSnapshotValueDocument(
    string Kind,
    string? Value);
