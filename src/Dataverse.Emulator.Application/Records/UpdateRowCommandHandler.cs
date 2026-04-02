using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Application.Records;

public sealed class UpdateRowCommandHandler(
    IMetadataRepository metadataRepository,
    IRecordRepository recordRepository,
    RecordValidationService recordValidationService)
{
    public async ValueTask<EntityRecord?> HandleAsync(
        UpdateRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var table = await metadataRepository.GetTableAsync(command.TableLogicalName, cancellationToken);
        if (table is null)
        {
            throw new InvalidOperationException($"Unknown table '{command.TableLogicalName}'.");
        }

        var existing = await recordRepository.GetAsync(command.TableLogicalName, command.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var errors = recordValidationService.ValidateUpdate(table, command.Values);
        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        var updated = existing.ApplyChanges(command.Values);
        await recordRepository.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}
