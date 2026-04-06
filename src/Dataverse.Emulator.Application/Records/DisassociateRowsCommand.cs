using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed record DisassociateRowsCommand(
    string PrimaryTableLogicalName,
    Guid PrimaryId,
    string RelationshipSchemaName,
    IReadOnlyList<RelatedRowReference> RelatedEntities)
    : ICommand<ErrorOr<Success>>;
