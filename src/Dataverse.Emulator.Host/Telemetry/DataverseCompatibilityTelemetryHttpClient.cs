using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Dataverse.Emulator.Host.Telemetry;

public sealed class DataverseCompatibilityTelemetryHttpClient(IHttpClientFactory httpClientFactory)
{
    public const string HttpClientName = "DataverseCompatibilityTelemetry";
    public const string UserAgentProductName = "Dataverse-Emulator-CompatibilityTelemetry";

    public async Task SendAsync(
        Uri endpoint,
        DataverseCompatibilityTelemetryEnvelope envelope,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(envelope);

        using var httpClient = httpClientFactory.CreateClient(HttpClientName);
        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue(UserAgentProductName, envelope.EmulatorVersion));
        }

        using var response = await httpClient.PostAsJsonAsync(endpoint, envelope, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
