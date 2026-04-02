using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Application.Abstractions;

public interface IRecordRepository
{
    ValueTask<EntityRecord?> GetAsync(
        string tableLogicalName,
        Guid id,
        CancellationToken cancellationToken = default);

    ValueTask<PageResult<EntityRecord>> ListAsync(
        RecordQuery query,
        CancellationToken cancellationToken = default);

    ValueTask CreateAsync(
        EntityRecord record,
        CancellationToken cancellationToken = default);

    ValueTask UpdateAsync(
        EntityRecord record,
        CancellationToken cancellationToken = default);

    ValueTask<bool> DeleteAsync(
        string tableLogicalName,
        Guid id,
        CancellationToken cancellationToken = default);

    ValueTask ResetAsync(
        IEnumerable<EntityRecord> records,
        CancellationToken cancellationToken = default);
}
