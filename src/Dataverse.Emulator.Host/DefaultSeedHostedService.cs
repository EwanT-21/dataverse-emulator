using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Host;

public sealed class DefaultSeedHostedService(
    IReadRepository<TableDefinition> tableRepository,
    SeedScenarioExecutor seedScenarioExecutor)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await tableRepository.AnyAsync(cancellationToken))
        {
            return;
        }

        await seedScenarioExecutor.ExecuteAsync(DefaultSeedScenarioFactory.Create(), cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
