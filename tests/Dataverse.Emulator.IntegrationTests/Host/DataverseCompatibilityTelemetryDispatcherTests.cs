using System.Net;
using Dataverse.Emulator.Host.Telemetry;
using Dataverse.Emulator.Protocols.Common.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class DataverseCompatibilityTelemetryDispatcherTests
{
    [Fact]
    public async Task Dispatcher_Opens_Circuit_After_Threshold_Failures_And_Suppresses_Subsequent_Sends()
    {
        var handler = new ScriptedHttpMessageHandler();
        var dispatcher = CreateDispatcher(handler, out var fakeClock);

        await dispatcher.StartAsync(CancellationToken.None);

        for (var i = 0; i < DataverseCompatibilityTelemetryDispatcher.CircuitBreakerFailureThreshold; i++)
        {
            dispatcher.Record(CreateEvent($"failure-{i}"));
        }

        await WaitForRequestCountAsync(handler, DataverseCompatibilityTelemetryDispatcher.CircuitBreakerFailureThreshold);

        dispatcher.Record(CreateEvent("suppressed"));
        await Task.Delay(75);

        Assert.Equal(DataverseCompatibilityTelemetryDispatcher.CircuitBreakerFailureThreshold, handler.RequestCount);

        fakeClock.Advance(DataverseCompatibilityTelemetryDispatcher.CircuitBreakerCoolDown + TimeSpan.FromSeconds(1));
        handler.QueueResponse(HttpStatusCode.Accepted);
        dispatcher.Record(CreateEvent("recovered"));

        await WaitForRequestCountAsync(handler, DataverseCompatibilityTelemetryDispatcher.CircuitBreakerFailureThreshold + 1);

        await dispatcher.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Dispatcher_Drains_Pending_Events_During_Stop()
    {
        var handler = new ScriptedHttpMessageHandler();
        var dispatcher = CreateDispatcher(handler, out _);

        for (var i = 0; i < 5; i++)
        {
            handler.QueueResponse(HttpStatusCode.Accepted);
        }

        await dispatcher.StartAsync(CancellationToken.None);

        for (var i = 0; i < 5; i++)
        {
            dispatcher.Record(CreateEvent($"event-{i}"));
        }

        await dispatcher.StopAsync(CancellationToken.None);

        Assert.Equal(5, handler.RequestCount);
    }

    private static DataverseCompatibilityTelemetryDispatcher CreateDispatcher(
        ScriptedHttpMessageHandler handler,
        out FakeTimeProvider fakeClock)
    {
        var options = new DataverseCompatibilityTelemetryOptions(
            Enabled: true,
            Endpoint: new Uri("https://telemetry.example.test/v1/events"));
        var httpClient = new DataverseCompatibilityTelemetryHttpClient(new TestHttpClientFactory(handler));
        var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
        fakeClock = clock;
        return new DataverseCompatibilityTelemetryDispatcher(
            options,
            httpClient,
            NullLogger<DataverseCompatibilityTelemetryDispatcher>.Instance,
            clock);
    }

    private static DataverseCompatibilityTelemetryEvent CreateEvent(string capabilityKey)
        => new(
            "unsupported-capability",
            "xrm",
            "TestRequest",
            "Protocol.Xrm.Execute.Unsupported",
            "organization-request",
            capabilityKey,
            DateTimeOffset.UtcNow);

    private static async Task WaitForRequestCountAsync(ScriptedHttpMessageHandler handler, int expectedCount)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (handler.RequestCount < expectedCount && DateTime.UtcNow < deadline)
        {
            await Task.Delay(15);
        }

        Assert.Equal(expectedCount, handler.RequestCount);
    }

    private sealed class ScriptedHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpStatusCode> scriptedResponses = new();
        private int requestCount;

        public int RequestCount => requestCount;

        public void QueueResponse(HttpStatusCode status) => scriptedResponses.Enqueue(status);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref requestCount);
            var status = scriptedResponses.Count > 0
                ? scriptedResponses.Dequeue()
                : HttpStatusCode.InternalServerError;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }

    private sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset now = start;

        public override DateTimeOffset GetUtcNow() => now;

        public void Advance(TimeSpan delta) => now = now.Add(delta);
    }
}
