using System.Runtime.InteropServices;
using System.Threading.Channels;
using Dataverse.Emulator.Protocols.Common.Telemetry;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dataverse.Emulator.Host.Telemetry;

public sealed class DataverseCompatibilityTelemetryDispatcher(
    DataverseCompatibilityTelemetryOptions options,
    DataverseCompatibilityTelemetryHttpClient telemetryHttpClient,
    ILogger<DataverseCompatibilityTelemetryDispatcher> logger,
    TimeProvider? timeProvider = null)
    : BackgroundService, IDataverseCompatibilityTelemetry
{
    public const int CircuitBreakerFailureThreshold = 5;
    public static readonly TimeSpan CircuitBreakerCoolDown = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan ShutdownDrainTimeout = TimeSpan.FromSeconds(2);

    private static readonly string EmulatorVersion = ResolveEmulatorVersion();
    private static readonly string RuntimeVersion = RuntimeInformation.FrameworkDescription;

    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly Channel<DataverseCompatibilityTelemetryEvent> events = Channel.CreateBounded<DataverseCompatibilityTelemetryEvent>(
        new BoundedChannelOptions(256)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest
        });

    private int consecutiveFailures;
    private DateTimeOffset circuitOpenUntilUtc;

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
            await TryDeliverAsync(compatibilityEvent, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        events.Writer.TryComplete();

        if (options.IsActive)
        {
            using var drainCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            drainCts.CancelAfter(ShutdownDrainTimeout);
            try
            {
                while (events.Reader.TryRead(out var pendingEvent))
                {
                    await TryDeliverAsync(pendingEvent, drainCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Drain budget exceeded; remaining events are discarded.
            }
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task TryDeliverAsync(
        DataverseCompatibilityTelemetryEvent compatibilityEvent,
        CancellationToken cancellationToken)
    {
        if (clock.GetUtcNow() < circuitOpenUntilUtc)
        {
            return;
        }

        try
        {
            var envelope = DataverseCompatibilityTelemetryEnvelope.FromEvent(
                compatibilityEvent,
                EmulatorVersion,
                RuntimeVersion);
            await telemetryHttpClient.SendAsync(options.Endpoint!, envelope, cancellationToken);
            consecutiveFailures = 0;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            consecutiveFailures++;
            if (consecutiveFailures >= CircuitBreakerFailureThreshold)
            {
                circuitOpenUntilUtc = clock.GetUtcNow().Add(CircuitBreakerCoolDown);
                logger.LogWarning(
                    ex,
                    "Compatibility telemetry circuit opened after {FailureCount} consecutive failures; suppressing delivery for {CoolDownSeconds}s.",
                    consecutiveFailures,
                    CircuitBreakerCoolDown.TotalSeconds);
                consecutiveFailures = 0;
            }
            else
            {
                logger.LogWarning(
                    ex,
                    "Compatibility telemetry delivery failed for {Protocol}/{CapabilityKind}.",
                    compatibilityEvent.Protocol,
                    compatibilityEvent.CapabilityKind);
            }
        }
    }

    private static string ResolveEmulatorVersion()
        => typeof(DataverseCompatibilityTelemetryDispatcher).Assembly
            .GetName()
            .Version?
            .ToString()
            ?? "0.0.0.0";
}
