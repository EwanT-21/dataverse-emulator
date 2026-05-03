using System.Runtime.InteropServices;
using System.Threading.Channels;
using Dataverse.Emulator.Protocols.Common.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dataverse.Emulator.Host.Telemetry;

public sealed class DataverseCompatibilityTelemetryDispatcher(
    DataverseCompatibilityTelemetryOptions options,
    DataverseCompatibilityTelemetryHttpClient telemetryHttpClient,
    ILogger<DataverseCompatibilityTelemetryDispatcher> logger)
    : BackgroundService, IDataverseCompatibilityTelemetry
{
    private static readonly string EmulatorVersion = ResolveEmulatorVersion();
    private static readonly string RuntimeVersion = RuntimeInformation.FrameworkDescription;
    private readonly Channel<DataverseCompatibilityTelemetryEvent> events = Channel.CreateBounded<DataverseCompatibilityTelemetryEvent>(
        new BoundedChannelOptions(256)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

    public void Record(DataverseCompatibilityTelemetryEvent telemetryEvent)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);

        if (!options.IsActive)
        {
            return;
        }

        if (!events.Writer.TryWrite(telemetryEvent))
        {
            logger.LogDebug(
                "Dropped compatibility telemetry event for {Protocol}/{CapabilityKind}.",
                telemetryEvent.Protocol,
                telemetryEvent.CapabilityKind);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.IsActive)
        {
            return;
        }

        await foreach (var compatibilityEvent in events.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var envelope = DataverseCompatibilityTelemetryEnvelope.FromEvent(
                    compatibilityEvent,
                    EmulatorVersion,
                    RuntimeVersion);
                await telemetryHttpClient.SendAsync(options.Endpoint!, envelope, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Compatibility telemetry delivery failed for {Protocol}/{CapabilityKind}.",
                    compatibilityEvent.Protocol,
                    compatibilityEvent.CapabilityKind);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        events.Writer.TryComplete();
        await base.StopAsync(cancellationToken);
    }

    private static string ResolveEmulatorVersion()
        => typeof(DataverseCompatibilityTelemetryDispatcher).Assembly
            .GetName()
            .Version?
            .ToString()
            ?? "0.0.0.0";
}
