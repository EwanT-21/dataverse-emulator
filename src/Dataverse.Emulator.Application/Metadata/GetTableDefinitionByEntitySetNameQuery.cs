using Dataverse.Emulator.Domain.Metadata;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Metadata;

public sealed record GetTableDefinitionByEntitySetNameQuery(string EntitySetName) : IQuery<ErrorOr<TableDefinition>>;
