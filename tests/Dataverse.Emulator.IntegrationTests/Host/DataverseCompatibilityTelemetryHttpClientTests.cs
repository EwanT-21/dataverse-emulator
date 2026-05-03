using System.Net;
using System.Text;
using Dataverse.Emulator.Host.Telemetry;
using Dataverse.Emulator.Protocols.Common.Telemetry;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class DataverseCompatibilityTelemetryHttpClientTests
{
    [Fact]
    public async Task Http_Client_Posts_Only_The_Sanitized_Telemetry_Envelope()
    {
        var handler = new RecordingHttpMessageHandler();
        using var httpClient = new HttpClient(handler);
        var telemetryHttpClient = new DataverseCompatibilityTelemetryHttpClient(httpClient);
        var compatibilityEvent = new DataverseCompatibilityTelemetryEvent(
            "unsupported-capability",
            "xrm",
            "ExecuteRequest",
            "Protocol.Xrm.Execute.Unsupported",
            "organization-request",
            "custom-or-unknown",
            new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.Zero));
        var envelope = DataverseCompatibilityTelemetryEnvelope.FromEvent(
            compatibilityEvent,
            "1.2.3.4",
            ".NET 10.0.0");

        await telemetryHttpClient.SendAsync(
            new Uri("https://telemetry.example.test/v1/events"),
            envelope,
            CancellationToken.None);

        Assert.NotNull(handler.RequestBody);
        Assert.Equal("POST", handler.Method);
        Assert.Equal("https://telemetry.example.test/v1/events", handler.RequestUri);
        Assert.Contains("\"schemaVersion\":\"1.0\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"eventKind\":\"unsupported-capability\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"capabilityKind\":\"organization-request\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.Contains("\"capabilityKey\":\"custom-or-unknown\"", handler.RequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("ContosoSecretSync", handler.RequestBody, StringComparison.Ordinal);
        Assert.DoesNotContain("\"message\":", handler.RequestBody, StringComparison.Ordinal);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public string? Method { get; private set; }

        public string? RequestUri { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method.Method;
            RequestUri = request.RequestUri?.ToString();
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
            };
        }
    }
}
