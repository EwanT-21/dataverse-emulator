using Dataverse.Emulator.Host.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dataverse.Emulator.Host.Endpoints;

public static class DataverseEmulatorDescriptorEndpoints
{
    public static IEndpointRouteBuilder MapDataverseEmulatorDescriptors(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapGet(
            "/",
            () => Results.Ok(
                new EmulatorDescriptor(
                    "Dataverse Emulator",
                    "Local emulator slice implemented for hosted Xrm/C# compatibility, shared account and contact semantics, demand-driven Execute coverage, secondary Web API CRUD, reset plus snapshot workflows, Xrm trace capture, and Aspire-friendly baseline shaping",
                    [
                        "Xrm/C# organization service",
                        "Dataverse Web API"
                    ],
                    "In-memory persistence",
                    "AuthType=AD;Url=http://localhost:{port}/org;Domain=EMULATOR;Username=local;Password=local")));

        routes.MapGet("/status", () => Results.Ok(new HealthDescriptor("healthy", DateTimeOffset.UtcNow)));

        routes.MapGet(
            "/roadmap",
            () => Results.Ok(
                new[]
                {
                    "Primary compatibility target: hosted CrmServiceClient bootstrap against /org with real Xrm/C# CRUD, query, metadata, and demand-driven Execute coverage.",
                    "Current table slice: seeded account and contact metadata, shared single-table and linked-query semantics, and matching Web API CRUD on /api/data/v9.2/accounts and /api/data/v9.2/contacts.",
                    "Current local workflow support: reset the emulator to a configured or named baseline through /_emulator/v1/reset.",
                    "Current local workflow support: export and import snapshot documents through /_emulator/v1/snapshot.",
                    "Current local workflow support: inspect and clear captured Xrm request traces through /_emulator/v1/traces/xrm.",
                    "Optional compatibility telemetry can emit sanitized unsupported-capability events when a telemetry endpoint is configured."
                }));

        return routes;
    }
}
