using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;
using ErrorOr;

namespace Dataverse.Emulator.Application.Seeding;

public sealed class SeedScenarioSnapshotService(
    IReadRepository<TableDefinition> tableRepository,
    IReadRepository<EntityRecord> recordRepository,
    SeedScenarioSnapshotMapper snapshotMapper,
    SeedScenarioExecutor seedScenarioExecutor)
{
    public async ValueTask<ErrorOr<SeedScenarioSnapshotDocument>> CaptureAsync(
        CancellationToken cancellationToken = default)
    {
        var tables = await tableRepository.ListAsync(new AllTablesSpecification(), cancellationToken);
        var records = await recordRepository.ListAsync(new AllRecordsSpecification(), cancellationToken);

        return snapshotMapper.ToDocument(
            new SeedScenario(tables, records),
            DateTimeOffset.UtcNow);
    }

    public async ValueTask<ErrorOr<SeedScenario>> RestoreAsync(
        SeedScenarioSnapshotDocument document,
        CancellationToken cancellationToken = default)
    {
        var scenarioResult = snapshotMapper.ToScenario(document);
        if (scenarioResult.IsError)
        {
            return scenarioResult.Errors;
        }

        await seedScenarioExecutor.ExecuteAsync(scenarioResult.Value, cancellationToken);
        return scenarioResult.Value;
    }
}
