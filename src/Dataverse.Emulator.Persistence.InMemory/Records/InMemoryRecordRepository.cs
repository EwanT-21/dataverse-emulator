using Ardalis.Specification;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Persistence.InMemory.Records;

public sealed class InMemoryRecordRepository : InMemoryRepository<EntityRecord>, IRecordQueryService
{
    private readonly RecordQueryExecutionService recordQueryExecutionService;

    public InMemoryRecordRepository()
        : this(new RecordQueryExecutionService())
    {
    }

    public InMemoryRecordRepository(RecordQueryExecutionService recordQueryExecutionService)
    {
        this.recordQueryExecutionService = recordQueryExecutionService;
    }

    public ValueTask<PageResult<EntityRecord>> ListAsync(
        RecordQuery query,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            recordQueryExecutionService.Execute(
                query,
                Snapshot()));
    }

    protected override string GetStorageKey(EntityRecord entity)
        => $"{entity.TableLogicalName}|{entity.Id:N}";

    protected override bool MatchesId<TId>(EntityRecord entity, TId id)
    {
        return id switch
        {
            Guid guid => entity.Id == guid,
            string compositeKey => string.Equals(GetStorageKey(entity), compositeKey, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
