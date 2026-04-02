using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Records;
using ErrorOr;
using FluentValidation;

namespace Dataverse.Emulator.Application.Records;

public sealed class GetRowByIdHandler(
    IRecordRepository recordRepository,
    IValidator<GetRowByIdQuery> validator)
{
    public async ValueTask<ErrorOr<EntityRecord>> HandleAsync(
        GetRowByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var record = await recordRepository.GetAsync(query.TableLogicalName, query.Id, cancellationToken);
        return record is not null
            ? record
            : DomainErrors.RowNotFound(query.TableLogicalName, query.Id);
    }
}
