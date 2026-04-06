using System.Text.Json;
using ErrorOr;

namespace Dataverse.Emulator.Application.Seeding;

public sealed class DataverseEmulatorBaselineStateService(
    DataverseEmulatorBaselineSettings baselineSettings,
    SeedScenarioExecutor seedScenarioExecutor,
    SeedScenarioSnapshotService snapshotService)
{
    public async ValueTask<ErrorOr<DataverseEmulatorBaselineState>> RestoreConfiguredBaselineAsync(
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(baselineSettings.SnapshotPath))
        {
            return await RestoreSnapshotFileAsync(baselineSettings.SnapshotPath, cancellationToken);
        }

        return await RestoreScenarioAsync(baselineSettings.SeedScenarioName, cancellationToken);
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
            ? DataverseEmulatorBaselineSettings.DefaultSeedScenarioName
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

public sealed record DataverseEmulatorBaselineState(
    string Kind,
    string Name);
