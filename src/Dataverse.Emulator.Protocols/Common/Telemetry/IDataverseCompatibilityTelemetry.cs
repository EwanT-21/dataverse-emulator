namespace Dataverse.Emulator.Protocols.Common.Telemetry;

public interface IDataverseCompatibilityTelemetry
{
    void Record(DataverseCompatibilityTelemetryEvent telemetryEvent);
}
