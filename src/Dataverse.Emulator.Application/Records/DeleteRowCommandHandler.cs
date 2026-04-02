using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using ErrorOr;
using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class DeleteRowCommandHandler(
    IRecordRepository recordRepository,
    IValidator<DeleteRowCommand> validator)
{
    public async ValueTask<ErrorOr<bool>> HandleAsync(
        DeleteRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var deleted = await recordRepository.DeleteAsync(command.TableLogicalName, command.Id, cancellationToken);
        return deleted
            ? true
            : DomainErrors.RowNotFound(command.TableLogicalName, command.Id);
    }
}
