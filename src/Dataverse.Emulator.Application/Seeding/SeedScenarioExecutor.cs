using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;

namespace Dataverse.Emulator.Application.Seeding;

public sealed class SeedScenarioExecutor(
    IRepository<TableDefinition> tableRepository,
    IRepository<EntityRecord> recordRepository)
{
    public async ValueTask ExecuteAsync(
        SeedScenario scenario,
        CancellationToken cancellationToken = default)
    {
        await recordRepository.DeleteRangeAsync(new AllRecordsSpecification(), cancellationToken);
        await tableRepository.DeleteRangeAsync(new AllTablesSpecification(), cancellationToken);

        await tableRepository.AddRangeAsync(scenario.Tables, cancellationToken);
        await recordRepository.AddRangeAsync(scenario.Records, cancellationToken);
    }
}
