using Dataverse.Emulator.Application.Runtime;
using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;
using ErrorOr;

namespace Dataverse.Emulator.Host;

internal static class DataverseEmulatorSeedScenarioCatalog
{
    public static IReadOnlyList<string> SupportedScenarioNames =>
    [
        DataverseEmulatorRuntimeSettings.DefaultSeedScenarioName,
        DataverseEmulatorRuntimeSettings.EmptySeedScenarioName
    ];

    public static ErrorOr<SeedScenario> Create(string? scenarioName)
    {
        var normalizedScenarioName = string.IsNullOrWhiteSpace(scenarioName)
            ? DataverseEmulatorRuntimeSettings.DefaultSeedScenarioName
            : scenarioName.Trim();

        if (normalizedScenarioName.Equals(
                DataverseEmulatorRuntimeSettings.DefaultSeedScenarioName,
                StringComparison.OrdinalIgnoreCase))
        {
            return DefaultSeedScenarioFactory.Create();
        }

        if (normalizedScenarioName.Equals(
                DataverseEmulatorRuntimeSettings.EmptySeedScenarioName,
                StringComparison.OrdinalIgnoreCase))
        {
            return new SeedScenario(Array.Empty<TableDefinition>(), Array.Empty<EntityRecord>());
        }

        return Error.Validation(
            "Seeding.Scenario.Unsupported",
            $"Seed scenario '{normalizedScenarioName}' is not supported. Supported scenarios: {string.Join(", ", SupportedScenarioNames)}.");
    }
}
