namespace Dataverse.Emulator.Application.Seeding;

public sealed record DataverseEmulatorBaselineSettings(
    string SeedScenarioName,
    string? SnapshotPath)
{
    public const string DefaultSeedScenarioName = "default-seed";
    public const string EmptySeedScenarioName = "empty";
}
