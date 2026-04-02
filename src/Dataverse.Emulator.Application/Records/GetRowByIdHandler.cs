using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Application.Records;

public sealed class GetRowByIdHandler(IRecordRepository recordRepository)
{
    public ValueTask<EntityRecord?> HandleAsync(
        GetRowByIdQuery query,
        CancellationToken cancellationToken = default)
        => recordRepository.GetAsync(query.TableLogicalName, query.Id, cancellationToken);
}
