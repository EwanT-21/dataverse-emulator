using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using ErrorOr;
using FluentValidation;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class GetTableDefinitionHandler(
    IMetadataRepository metadataRepository,
    IValidator<GetTableDefinitionQuery> validator)
{
    public async ValueTask<ErrorOr<TableDefinition>> HandleAsync(
        GetTableDefinitionQuery query,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(query, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var table = await metadataRepository.GetTableAsync(query.LogicalName, cancellationToken);
        return table is not null
            ? table
            : DomainErrors.UnknownTable(query.LogicalName);
    }
}
