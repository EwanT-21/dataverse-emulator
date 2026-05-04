namespace Dataverse.Emulator.Host.Contracts;

public sealed record EmulatorDescriptor(
    string Name,
    string Status,
    string[] Protocols,
    string Persistence,
    string ConnectionStringTemplate);

public sealed record HealthDescriptor(string Status, DateTimeOffset UtcNow);

public sealed record EmulatorResetDescriptor(
    string Status,
    string BaselineKind,
    string BaselineName,
    DateTimeOffset UtcNow);

public sealed record EmulatorSnapshotImportedDescriptor(
    string Status,
    string SchemaVersion,
    int TableCount,
    int RecordCount,
    DateTimeOffset UtcNow);

public sealed record EmulatorXrmTraceDescriptor(
    int Count,
    EmulatorXrmTraceItem[] Items);

public sealed record EmulatorXrmTraceItem(
    long Sequence,
    string Source,
    string Name,
    bool Succeeded,
    int? ErrorCode,
    string? Message,
    DateTimeOffset StartedAtUtc,
    long DurationMilliseconds);

public sealed record EmulatorTraceResetDescriptor(
    string Status,
    string TraceKind);

public sealed record EmulatorAdminErrorDescriptor(
    string Error,
    EmulatorAdminErrorItem[] Details);

public sealed record EmulatorAdminErrorItem(
    string Code,
    string Description);
