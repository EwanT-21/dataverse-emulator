using Dataverse.Emulator.Application.Abstractions;

namespace Dataverse.Emulator.Application.Records;

public sealed class DeleteRowCommandHandler(IRecordRepository recordRepository)
{
    public ValueTask<bool> HandleAsync(
        DeleteRowCommand command,
        CancellationToken cancellationToken = default)
        => recordRepository.DeleteAsync(command.TableLogicalName, command.Id, cancellationToken);
}
