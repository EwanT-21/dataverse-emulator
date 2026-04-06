using Dataverse.Emulator.Domain.Metadata;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Metadata;

public sealed record GetRelationshipDefinitionQuery(string SchemaName)
    : IQuery<ErrorOr<LookupRelationshipDefinition>>;
