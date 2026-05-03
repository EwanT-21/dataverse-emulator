namespace Dataverse.Emulator.Protocols.Common.Telemetry;

public sealed class NoOpDataverseCompatibilityTelemetry : IDataverseCompatibilityTelemetry
{
    public void Record(DataverseCompatibilityTelemetryEvent telemetryEvent)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
    }
}
