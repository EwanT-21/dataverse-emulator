namespace Dataverse.Emulator.Host.Telemetry;

public static class DataverseCompatibilityTelemetryConfiguration
{
    public static bool ResolveEnabled(string? configuredTelemetryEnabled)
    {
        if (string.IsNullOrWhiteSpace(configuredTelemetryEnabled))
        {
            return false;
        }

        if (bool.TryParse(configuredTelemetryEnabled, out var telemetryEnabled))
        {
            return telemetryEnabled;
        }

        throw new InvalidOperationException(
            $"Environment variable '{DataverseEmulatorHostEnvironmentVariables.TelemetryEnabledEnvironmentVariableName}' must be 'true' or 'false'.");
    }

    public static Uri? ResolveEndpoint(string? configuredTelemetryEndpoint)
    {
        if (string.IsNullOrWhiteSpace(configuredTelemetryEndpoint))
        {
            return null;
        }

        if (!Uri.TryCreate(configuredTelemetryEndpoint.Trim(), UriKind.Absolute, out var endpoint))
        {
            throw new InvalidOperationException(
                $"Environment variable '{DataverseEmulatorHostEnvironmentVariables.TelemetryEndpointEnvironmentVariableName}' must be an absolute URI.");
        }

        if (endpoint.Scheme != Uri.UriSchemeHttps && !endpoint.IsLoopback)
        {
            throw new InvalidOperationException(
                $"Environment variable '{DataverseEmulatorHostEnvironmentVariables.TelemetryEndpointEnvironmentVariableName}' must use https except for loopback hosts.");
        }

        return endpoint;
    }
}
