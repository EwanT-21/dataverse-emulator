using Dataverse.Emulator.Application.Abstractions;

namespace Dataverse.Emulator.Application.Seeding;

public sealed class SeedScenarioExecutor(
    IMetadataRepository metadataRepository,
    IRecordRepository recordRepository)
{
    public async ValueTask ExecuteAsync(
        SeedScenario scenario,
        CancellationToken cancellationToken = default)
    {
        await metadataRepository.ResetAsync(scenario.Tables, cancellationToken);
        await recordRepository.ResetAsync(scenario.Records, cancellationToken);
    }
}
