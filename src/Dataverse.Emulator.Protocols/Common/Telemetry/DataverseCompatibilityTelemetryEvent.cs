namespace Dataverse.Emulator.Protocols.Common.Telemetry;

public sealed record DataverseCompatibilityTelemetryEvent(
    string EventKind,
    string Protocol,
    string Source,
    string ErrorCode,
    string CapabilityKind,
    string CapabilityKey,
    DateTimeOffset OccurredAtUtc);
