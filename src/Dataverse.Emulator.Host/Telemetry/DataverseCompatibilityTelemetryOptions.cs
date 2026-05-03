namespace Dataverse.Emulator.Host.Telemetry;

public sealed record DataverseCompatibilityTelemetryOptions(
    bool Enabled,
    Uri? Endpoint)
{
    public bool IsActive => Enabled && Endpoint is not null;
}
