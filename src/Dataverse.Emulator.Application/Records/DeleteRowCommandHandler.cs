using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class DeleteRowCommandHandler(
    IRepository<EntityRecord> recordRepository)
    : ICommandHandler<DeleteRowCommand, ErrorOr<bool>>
{
    public async ValueTask<ErrorOr<bool>> Handle(
        DeleteRowCommand command,
        CancellationToken cancellationToken = default)
    {
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
