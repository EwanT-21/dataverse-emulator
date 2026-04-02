using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;
using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class ListRowsHandler(
    IMetadataRepository metadataRepository,
    IRecordRepository recordRepository,
    IValidator<ListRowsQuery> validator,
    QueryValidationService queryValidationService)
{
    public async ValueTask<ErrorOr<PageResult<EntityRecord>>> HandleAsync(
        ListRowsQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var table = await metadataRepository.GetTableAsync(query.Query.TableLogicalName, cancellationToken);
        if (table is null)
        {
            return DomainErrors.UnknownTable(query.Query.TableLogicalName);
        }

        var errors = queryValidationService.Validate(table, query.Query);
        if (errors.Count > 0)
        {
            return errors.ToList();
        }

        return await recordRepository.ListAsync(query.Query, cancellationToken);
    }
}
