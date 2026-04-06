using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class DisassociateRowsCommandHandler(
    LookupRelationshipAssociationService lookupRelationshipAssociationService)
    : ICommandHandler<DisassociateRowsCommand, ErrorOr<Success>>
{
    public async ValueTask<ErrorOr<Success>> Handle(
        DisassociateRowsCommand command,
        CancellationToken cancellationToken = default)
        => await lookupRelationshipAssociationService.DisassociateAsync(command, cancellationToken);
}
