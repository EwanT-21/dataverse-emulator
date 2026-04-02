using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;

namespace Dataverse.Emulator.Application.Metadata;

public sealed class GetTableDefinitionHandler(IMetadataRepository metadataRepository)
{
    public ValueTask<TableDefinition?> HandleAsync(
        GetTableDefinitionQuery query,
        CancellationToken cancellationToken = default)
        => metadataRepository.GetTableAsync(query.LogicalName, cancellationToken);
}
