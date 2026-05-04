using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Protocols.Xrm.Tracing;
using Dataverse.Emulator.Protocols.Xrm.Runtime;

namespace Dataverse.Emulator.Host.Composition;

public static class DataverseEmulatorHostSettingsResolver
{
    public static string ResolveSeedScenarioName(string? configured)
        => string.IsNullOrWhiteSpace(configured)
            ? DataverseEmulatorBaselineSettings.DefaultSeedScenarioName
            : configured.Trim();

    public static string? ResolveSnapshotPath(string? configured)
        => string.IsNullOrWhiteSpace(configured)
            ? null
            : configured.Trim();

    public static string ResolveOrganizationVersion(string? configured)
        => string.IsNullOrWhiteSpace(configured)
            ? DataverseXrmCompatibilitySettings.DefaultOrganizationVersion
            : configured.Trim();

    public static int ResolveXrmTraceLimit(string? configured)
        => int.TryParse(configured, out var traceLimit) && traceLimit > 0
            ? traceLimit
            : DataverseXrmTraceOptions.DefaultTraceLimit;
}
