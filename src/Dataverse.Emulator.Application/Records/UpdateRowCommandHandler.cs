using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;
using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class UpdateRowCommandHandler(
    IMetadataRepository metadataRepository,
    IRecordRepository recordRepository,
    IValidator<UpdateRowCommand> validator,
    RecordValidationService recordValidationService)
{
    public async ValueTask<ErrorOr<EntityRecord>> HandleAsync(
        UpdateRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var table = await metadataRepository.GetTableAsync(command.TableLogicalName, cancellationToken);
        if (table is null)
        {
            return DomainErrors.UnknownTable(command.TableLogicalName);
        }

        var existing = await recordRepository.GetAsync(command.TableLogicalName, command.Id, cancellationToken);
        if (existing is null)
        {
            return DomainErrors.RowNotFound(command.TableLogicalName, command.Id);
        }

        var errors = recordValidationService.ValidateUpdate(table, command.Values);
        if (errors.Count > 0)
        {
            return errors.ToList();
        }

        var updated = existing.ApplyChanges(command.Values);
        await recordRepository.UpdateAsync(updated, cancellationToken);
        return updated;
    }
}
