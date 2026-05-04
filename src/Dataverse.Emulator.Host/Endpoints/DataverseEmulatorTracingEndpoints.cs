using Dataverse.Emulator.Host.Contracts;
using Dataverse.Emulator.Protocols.Xrm.Tracing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dataverse.Emulator.Host.Endpoints;

public static class DataverseEmulatorTracingEndpoints
{
    public static IEndpointRouteBuilder MapDataverseEmulatorTracing(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGet(
            "/_emulator/v1/traces/xrm",
            (int? limit, DataverseXrmRequestTraceStore traceStore) =>
            {
                var items = traceStore.List(limit)
                    .Select(entry => new EmulatorXrmTraceItem(
                        entry.Sequence,
                        entry.Source,
                        entry.Name,
                        entry.Succeeded,
                        entry.ErrorCode,
                        entry.Message,
                        entry.StartedAtUtc,
                        entry.DurationMilliseconds))
                    .ToArray();

                return Results.Ok(new EmulatorXrmTraceDescriptor(items.Length, items));
            });

        routes.MapDelete(
            "/_emulator/v1/traces/xrm",
            (DataverseXrmRequestTraceStore traceStore) =>
            {
                traceStore.Clear();
                return Results.Ok(new EmulatorTraceResetDescriptor("cleared", "xrm"));
            });

        return routes;
    }
}
