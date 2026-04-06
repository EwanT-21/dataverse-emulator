using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Host;

internal sealed class DefaultSeedHostedService(
    IReadRepository<TableDefinition> tableRepository,
    DataverseEmulatorBaselineStateService baselineStateService)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (await tableRepository.AnyAsync(cancellationToken))
        {
            return;
        }

        var restoreResult = await baselineStateService.RestoreConfiguredBaselineAsync(cancellationToken);
        if (restoreResult.IsError)
        {
            throw new InvalidOperationException(
                $"The emulator baseline state could not be restored: {string.Join(" | ", restoreResult.Errors.Select(error => error.Description))}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
