namespace Dataverse.Emulator.Application.Runtime;

public sealed record DataverseEmulatorRuntimeSettings(
    string SeedScenarioName,
    string? SnapshotPath,
    string OrganizationVersion,
    int XrmTraceLimit)
{
    public const string SeedScenarioEnvironmentVariableName = "DATAVERSE_EMULATOR_SEED_SCENARIO";
    public const string SnapshotPathEnvironmentVariableName = "DATAVERSE_EMULATOR_SNAPSHOT_PATH";
    public const string OrganizationVersionEnvironmentVariableName = "DATAVERSE_EMULATOR_ORGANIZATION_VERSION";
    public const string XrmTraceLimitEnvironmentVariableName = "DATAVERSE_EMULATOR_XRM_TRACE_LIMIT";
    public const string DefaultSeedScenarioName = "default-seed";
    public const string EmptySeedScenarioName = "empty";
    public const string DefaultOrganizationVersion = "9.2.0.0";
    public const int DefaultXrmTraceLimit = 200;
}
