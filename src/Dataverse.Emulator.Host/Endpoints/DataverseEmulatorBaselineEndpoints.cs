using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Host.Contracts;
using ErrorOr;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Dataverse.Emulator.Host.Endpoints;

public static class DataverseEmulatorBaselineEndpoints
{
    public static IEndpointRouteBuilder MapDataverseEmulatorBaseline(this IEndpointRouteBuilder routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        routes.MapPost(
            "/_emulator/v1/reset",
            async (
                string? scenario,
                DataverseEmulatorBaselineStateService baselineStateService,
                CancellationToken cancellationToken) =>
            {
                var restoreResult = string.IsNullOrWhiteSpace(scenario)
                    ? await baselineStateService.RestoreConfiguredBaselineAsync(cancellationToken)
                    : await baselineStateService.RestoreScenarioAsync(scenario, cancellationToken);

                return restoreResult.IsError
                    ? DataverseEmulatorAdminResults.ToAdminErrorResult(restoreResult.Errors)
                    : Results.Ok(new EmulatorResetDescriptor(
                        "reset",
                        restoreResult.Value.Kind,
                        restoreResult.Value.Name,
                        DateTimeOffset.UtcNow));
            });

        routes.MapGet(
            "/_emulator/v1/snapshot",
            async (SeedScenarioSnapshotService snapshotService, CancellationToken cancellationToken) =>
            {
                var snapshotResult = await snapshotService.CaptureAsync(cancellationToken);
                return snapshotResult.IsError
                    ? DataverseEmulatorAdminResults.ToAdminErrorResult(snapshotResult.Errors)
                    : Results.Ok(snapshotResult.Value);
            });

        routes.MapPost(
            "/_emulator/v1/snapshot",
            async (
                SeedScenarioSnapshotDocument? snapshot,
                SeedScenarioSnapshotService snapshotService,
                CancellationToken cancellationToken) =>
            {
                if (snapshot is null)
                {
                    return DataverseEmulatorAdminResults.ToAdminErrorResult(
                    [
                        Error.Validation(
                            "Seeding.Snapshot.Required",
                            "Snapshot payload is required.")
                    ]);
                }

                var restoreResult = await snapshotService.RestoreAsync(snapshot, cancellationToken);
                if (restoreResult.IsError)
                {
                    return DataverseEmulatorAdminResults.ToAdminErrorResult(restoreResult.Errors);
                }

                return Results.Ok(new EmulatorSnapshotImportedDescriptor(
                    "imported",
                    snapshot.SchemaVersion,
                    restoreResult.Value.Tables.Count,
                    restoreResult.Value.Records.Count,
                    DateTimeOffset.UtcNow));
            });

        return routes;
    }
}
