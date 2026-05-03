using System.Net.Http.Json;

namespace Dataverse.Emulator.Host.Telemetry;

public sealed class DataverseCompatibilityTelemetryHttpClient(HttpClient httpClient)
{
    public async Task SendAsync(
        Uri endpoint,
        DataverseCompatibilityTelemetryEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(envelope);

        using var response = await httpClient.PostAsJsonAsync(endpoint, envelope, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
