using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Application.Abstractions;

public interface IRecordQueryService
{
    ValueTask<PageResult<EntityRecord>> ListAsync(
        RecordQuery query,
        CancellationToken cancellationToken = default);
}
