using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;
using ErrorOr;
using FluentValidation;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class DeleteRowCommandHandler(
    IRepository<EntityRecord> recordRepository,
    IValidator<DeleteRowCommand> validator)
    : ICommandHandler<DeleteRowCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(
        DeleteRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var existing = await recordRepository.SingleOrDefaultAsync(
            new RecordByIdSpecification(command.TableLogicalName, command.Id),
            cancellationToken);

        if (existing is null)
        {
            return DomainErrors.RowNotFound(command.TableLogicalName, command.Id);
        }

        await recordRepository.DeleteAsync(existing, cancellationToken);
        return true;
    }
}
