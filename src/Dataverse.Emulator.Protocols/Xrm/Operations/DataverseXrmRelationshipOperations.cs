using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Application.Records;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Protocols.Common;
using ErrorOr;
using Mediator;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Operations;

public sealed class DataverseXrmRelationshipOperations(
    IMediator mediator)
{
    public async Task<ErrorOr<Success>> AssociateAsync(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities,
        CancellationToken cancellationToken)
    {
        var commandResult = BuildAssociateCommand(
            entityName,
            entityId,
            relationship,
            relatedEntities);
        if (commandResult.IsError)
        {
            return commandResult.Errors;
        }

        return await mediator.Send(commandResult.Value, cancellationToken);
    }

    public async Task<ErrorOr<Success>> DisassociateAsync(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities,
        CancellationToken cancellationToken)
    {
        var commandResult = BuildDisassociateCommand(
            entityName,
            entityId,
            relationship,
            relatedEntities);
        if (commandResult.IsError)
        {
            return commandResult.Errors;
        }

        return await mediator.Send(commandResult.Value, cancellationToken);
    }

    public async Task<ErrorOr<LookupRelationshipDefinition>> RetrieveRelationshipAsync(
        string schemaName,
        CancellationToken cancellationToken)
        => await mediator.Send(new GetRelationshipDefinitionQuery(schemaName), cancellationToken);

    public async Task<ErrorOr<LookupRelationshipDefinition[]>> ListRelationshipsAsync(
        CancellationToken cancellationToken)
        => await mediator.Send(new ListRelationshipDefinitionsQuery(), cancellationToken);

    private static ErrorOr<AssociateRowsCommand> BuildAssociateCommand(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities)
    {
        var relatedRowsResult = BuildRelatedRows(entityName, entityId, relationship, relatedEntities);
        return relatedRowsResult.IsError
            ? relatedRowsResult.Errors
            : new AssociateRowsCommand(entityName, entityId, relationship.SchemaName, relatedRowsResult.Value);
    }

    private static ErrorOr<DisassociateRowsCommand> BuildDisassociateCommand(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities)
    {
        var relatedRowsResult = BuildRelatedRows(entityName, entityId, relationship, relatedEntities);
        return relatedRowsResult.IsError
            ? relatedRowsResult.Errors
            : new DisassociateRowsCommand(entityName, entityId, relationship.SchemaName, relatedRowsResult.Value);
    }

    private static ErrorOr<RelatedRowReference[]> BuildRelatedRows(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return DataverseXrmErrors.ParameterRequired("entityName");
        }

        if (entityId == Guid.Empty)
        {
            return DomainErrors.Validation(
                "Protocol.Xrm.Relationship.EntityIdRequired",
                $"Relationship operation for '{entityName}' requires a non-empty id.");
        }

        if (relationship is null || string.IsNullOrWhiteSpace(relationship.SchemaName))
        {
            return DataverseXrmErrors.ParameterRequired("relationship");
        }

        if (relatedEntities is null)
        {
            return DataverseXrmErrors.ParameterRequired("relatedEntities");
        }

        var relatedRows = relatedEntities
            .Where(reference => reference is not null)
            .Select(reference => new RelatedRowReference(reference.LogicalName, reference.Id))
            .ToArray();

        return relatedRows;
    }
}
