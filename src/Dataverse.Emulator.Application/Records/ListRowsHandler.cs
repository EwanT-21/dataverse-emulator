using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Application.Records;

public sealed class ListRowsHandler(
    IMetadataRepository metadataRepository,
    IRecordRepository recordRepository,
    QueryValidationService queryValidationService)
{
    public async ValueTask<PageResult<EntityRecord>> HandleAsync(
        ListRowsQuery query,
        CancellationToken cancellationToken = default)
    {
        var table = await metadataRepository.GetTableAsync(query.Query.TableLogicalName, cancellationToken);
        if (table is null)
        {
            throw new InvalidOperationException($"Unknown table '{query.Query.TableLogicalName}'.");
        }

        var errors = queryValidationService.Validate(table, query.Query);
        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        return await recordRepository.ListAsync(query.Query, cancellationToken);
    }
}
