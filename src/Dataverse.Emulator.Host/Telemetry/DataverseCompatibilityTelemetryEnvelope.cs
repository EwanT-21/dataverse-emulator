using Dataverse.Emulator.Protocols.Common.Telemetry;

namespace Dataverse.Emulator.Host.Telemetry;

public sealed record DataverseCompatibilityTelemetryEnvelope(
    string SchemaVersion,
    string EventKind,
    string Protocol,
    string Source,
    string ErrorCode,
    string CapabilityKind,
    string CapabilityKey,
    DateTimeOffset OccurredAtUtc,
    string EmulatorVersion,
    string RuntimeVersion)
{
    public const string CurrentSchemaVersion = "1.0";

    public static DataverseCompatibilityTelemetryEnvelope FromEvent(
        DataverseCompatibilityTelemetryEvent telemetryEvent,
        string emulatorVersion,
        string runtimeVersion)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        ArgumentException.ThrowIfNullOrWhiteSpace(emulatorVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeVersion);

        return new DataverseCompatibilityTelemetryEnvelope(
            CurrentSchemaVersion,
            telemetryEvent.EventKind,
            telemetryEvent.Protocol,
            telemetryEvent.Source,
            telemetryEvent.ErrorCode,
            telemetryEvent.CapabilityKind,
            telemetryEvent.CapabilityKey,
            telemetryEvent.OccurredAtUtc,
            emulatorVersion,
            runtimeVersion);
    }
}
