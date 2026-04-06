using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;
using ErrorOr;

namespace Dataverse.Emulator.Application.Seeding;

internal static class DataverseEmulatorSeedScenarioCatalog
{
    public static IReadOnlyList<string> SupportedScenarioNames =>
    [
        DataverseEmulatorBaselineSettings.DefaultSeedScenarioName,
        DataverseEmulatorBaselineSettings.EmptySeedScenarioName
    ];

    public static ErrorOr<SeedScenario> Create(string? scenarioName)
    {
        var normalizedScenarioName = string.IsNullOrWhiteSpace(scenarioName)
            ? DataverseEmulatorBaselineSettings.DefaultSeedScenarioName
            : scenarioName.Trim();

        if (normalizedScenarioName.Equals(
                DataverseEmulatorBaselineSettings.DefaultSeedScenarioName,
                StringComparison.OrdinalIgnoreCase))
        {
            return DefaultSeedScenarioFactory.Create();
        }

        if (normalizedScenarioName.Equals(
                DataverseEmulatorBaselineSettings.EmptySeedScenarioName,
                StringComparison.OrdinalIgnoreCase))
        {
            return new SeedScenario(Array.Empty<TableDefinition>(), Array.Empty<EntityRecord>());
        }

        return Error.Validation(
            "Seeding.Scenario.Unsupported",
            $"Seed scenario '{normalizedScenarioName}' is not supported. Supported scenarios: {string.Join(", ", SupportedScenarioNames)}.");
    }
}
