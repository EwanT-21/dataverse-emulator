using System.Text.Json;
using Dataverse.Emulator.Application.Runtime;
using Dataverse.Emulator.Application.Seeding;
using ErrorOr;

namespace Dataverse.Emulator.Host;

internal sealed class DataverseEmulatorBaselineStateService(
    DataverseEmulatorRuntimeSettings runtimeSettings,
    SeedScenarioExecutor seedScenarioExecutor,
    SeedScenarioSnapshotService snapshotService)
{
    public async ValueTask<ErrorOr<DataverseEmulatorBaselineState>> RestoreConfiguredBaselineAsync(
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(runtimeSettings.SnapshotPath))
        {
            return await RestoreSnapshotFileAsync(runtimeSettings.SnapshotPath, cancellationToken);
        }

        return await RestoreScenarioAsync(runtimeSettings.SeedScenarioName, cancellationToken);
    }

    public async ValueTask<ErrorOr<DataverseEmulatorBaselineState>> RestoreScenarioAsync(
        string? scenarioName,
        CancellationToken cancellationToken = default)
    {
        var scenarioResult = DataverseEmulatorSeedScenarioCatalog.Create(scenarioName);
        if (scenarioResult.IsError)
        {
            return scenarioResult.Errors;
        }

        await seedScenarioExecutor.ExecuteAsync(scenarioResult.Value, cancellationToken);

        var normalizedScenarioName = string.IsNullOrWhiteSpace(scenarioName)
            ? DataverseEmulatorRuntimeSettings.DefaultSeedScenarioName
            : scenarioName.Trim();

        return new DataverseEmulatorBaselineState("scenario", normalizedScenarioName);
    }

    private async ValueTask<ErrorOr<DataverseEmulatorBaselineState>> RestoreSnapshotFileAsync(
        string snapshotPath,
        CancellationToken cancellationToken)
    {
        var resolvedPath = Path.GetFullPath(snapshotPath);
        if (!File.Exists(resolvedPath))
        {
            return Error.Validation(
                "Seeding.Snapshot.FileNotFound",
                $"Snapshot file '{resolvedPath}' does not exist.");
        }

        SeedScenarioSnapshotDocument? document;
        await using (var stream = File.OpenRead(resolvedPath))
        {
            document = await JsonSerializer.DeserializeAsync<SeedScenarioSnapshotDocument>(stream, cancellationToken: cancellationToken);
        }

        if (document is null)
        {
            return Error.Validation(
                "Seeding.Snapshot.Invalid",
                $"Snapshot file '{resolvedPath}' could not be deserialized.");
        }

        var restoreResult = await snapshotService.RestoreAsync(document, cancellationToken);
        if (restoreResult.IsError)
        {
            return restoreResult.Errors;
        }

        return new DataverseEmulatorBaselineState("snapshot", resolvedPath);
    }
}

internal sealed record DataverseEmulatorBaselineState(
    string Kind,
    string Name);
