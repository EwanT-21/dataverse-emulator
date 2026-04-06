using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class AssociateRowsCommandHandler(
    LookupRelationshipAssociationService lookupRelationshipAssociationService)
    : ICommandHandler<AssociateRowsCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(
        AssociateRowsCommand command,
        CancellationToken cancellationToken = default)
        => await lookupRelationshipAssociationService.AssociateAsync(command, cancellationToken);
}
