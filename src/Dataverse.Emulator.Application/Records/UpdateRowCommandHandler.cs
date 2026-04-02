using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;
using FluentValidation;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class UpdateRowCommandHandler(
    IReadRepository<TableDefinition> tableRepository,
    IRepository<EntityRecord> recordRepository,
    IValidator<UpdateRowCommand> validator,
    RecordValidationService recordValidationService)
    : ICommandHandler<UpdateRowCommand, ErrorOr<EntityRecord>>
{
    public async ValueTask<ErrorOr<EntityRecord>> Handle(
        UpdateRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var table = await tableRepository.SingleOrDefaultAsync(
            new TableByLogicalNameSpecification(command.TableLogicalName),
            cancellationToken);

        if (table is null)
        {
            return DomainErrors.UnknownTable(command.TableLogicalName);
        }

        var existing = await recordRepository.SingleOrDefaultAsync(
            new RecordByIdSpecification(command.TableLogicalName, command.Id),
            cancellationToken);

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
