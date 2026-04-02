using Dataverse.Emulator.Domain.Metadata;
using Mediator;

namespace Dataverse.Emulator.Application.Metadata;

public sealed record ListTableDefinitionsQuery : IQuery<IReadOnlyList<TableDefinition>>;
