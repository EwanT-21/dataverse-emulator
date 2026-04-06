using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;

namespace Dataverse.Emulator.Application.Records;

public sealed class LookupRelationshipAssociationService(
    IReadRepository<TableDefinition> tableRepository,
    IRepository<EntityRecord> recordRepository,
    LookupRelationshipDefinitionService lookupRelationshipDefinitionService,
    RecordValidationService recordValidationService)
{
    public async Task<ErrorOr<Success>> AssociateAsync(
        AssociateRowsCommand command,
        CancellationToken cancellationToken)
    {
        var contextResult = await ResolveContextAsync(
            command.PrimaryTableLogicalName,
            command.PrimaryId,
            command.RelationshipSchemaName,
            command.RelatedEntities,
            cancellationToken);
        if (contextResult.IsError)
        {
            return contextResult.Errors;
        }

        var validationErrors = recordValidationService.ValidateUpdate(
            contextResult.Value.ReferencingTable,
            new Dictionary<string, object?>
            {
                [contextResult.Value.Relationship.ReferencingAttributeLogicalName] = command.PrimaryId
            });
        if (validationErrors.Count > 0)
        {
            return validationErrors.ToList();
        }

        foreach (var relatedRecord in contextResult.Value.RelatedRecords)
        {
            var updated = relatedRecord.ApplyChanges(
                new Dictionary<string, object?>
                {
                    [contextResult.Value.Relationship.ReferencingAttributeLogicalName] = command.PrimaryId
                });
            await recordRepository.UpdateAsync(updated, cancellationToken);
        }

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> DisassociateAsync(
        DisassociateRowsCommand command,
        CancellationToken cancellationToken)
    {
        var contextResult = await ResolveContextAsync(
            command.PrimaryTableLogicalName,
            command.PrimaryId,
            command.RelationshipSchemaName,
            command.RelatedEntities,
            cancellationToken);
        if (contextResult.IsError)
        {
            return contextResult.Errors;
        }

        var validationErrors = recordValidationService.ValidateUpdate(
            contextResult.Value.ReferencingTable,
            new Dictionary<string, object?>
            {
                [contextResult.Value.Relationship.ReferencingAttributeLogicalName] = null
            });
        if (validationErrors.Count > 0)
        {
            return validationErrors.ToList();
        }

        foreach (var relatedRecord in contextResult.Value.RelatedRecords)
        {
            if (!relatedRecord.Values.TryGetValue(
                    contextResult.Value.Relationship.ReferencingAttributeLogicalName,
                    out var currentValue)
                || currentValue is not Guid currentId
                || currentId != command.PrimaryId)
            {
                continue;
            }

            var updated = relatedRecord.ApplyChanges(
                new Dictionary<string, object?>
                {
                    [contextResult.Value.Relationship.ReferencingAttributeLogicalName] = null
                });
            await recordRepository.UpdateAsync(updated, cancellationToken);
        }

        return Result.Success;
    }

    private async Task<ErrorOr<RelationshipContext>> ResolveContextAsync(
        string primaryTableLogicalName,
        Guid primaryId,
        string relationshipSchemaName,
        IReadOnlyList<RelatedRowReference> relatedEntities,
        CancellationToken cancellationToken)
    {
        var tables = await tableRepository.ListAsync(new AllTablesSpecification(), cancellationToken);
        var relationshipResult = lookupRelationshipDefinitionService.Resolve(tables, relationshipSchemaName);
        if (relationshipResult.IsError)
        {
            return relationshipResult.Errors;
        }

        var relationship = relationshipResult.Value;
        if (!relationship.ReferencedTableLogicalName.Equals(primaryTableLogicalName, StringComparison.OrdinalIgnoreCase))
        {
            return DomainErrors.Validation(
                "Records.Relationship.PrimaryTableMismatch",
                $"Relationship '{relationshipSchemaName}' expects primary table '{relationship.ReferencedTableLogicalName}', but received '{primaryTableLogicalName}'.");
        }

        var primaryRecord = await recordRepository.SingleOrDefaultAsync(
            new RecordByIdSpecification(primaryTableLogicalName, primaryId),
            cancellationToken);
        if (primaryRecord is null)
        {
            return DomainErrors.RowNotFound(primaryTableLogicalName, primaryId);
        }

        var referencingTable = tables.Single(table =>
            table.LogicalName.Equals(relationship.ReferencingTableLogicalName, StringComparison.OrdinalIgnoreCase));
        var relatedRecords = new List<EntityRecord>();
        var errors = new List<Error>();

        foreach (var relatedEntity in relatedEntities
                     .Where(reference => reference is not null)
                     .DistinctBy(reference => (reference.TableLogicalName, reference.Id)))
        {
            if (!relatedEntity.TableLogicalName.Equals(relationship.ReferencingTableLogicalName, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(DomainErrors.Validation(
                    "Records.Relationship.RelatedTableMismatch",
                    $"Relationship '{relationshipSchemaName}' expects related rows on table '{relationship.ReferencingTableLogicalName}', but received '{relatedEntity.TableLogicalName}'."));
                continue;
            }

            var relatedRecord = await recordRepository.SingleOrDefaultAsync(
                new RecordByIdSpecification(relatedEntity.TableLogicalName, relatedEntity.Id),
                cancellationToken);
            if (relatedRecord is null)
            {
                errors.Add(DomainErrors.RowNotFound(relatedEntity.TableLogicalName, relatedEntity.Id));
                continue;
            }

            relatedRecords.Add(relatedRecord);
        }

        return errors.Count > 0
            ? errors
            : new RelationshipContext(relationship, referencingTable, relatedRecords);
    }

    private sealed record RelationshipContext(
        LookupRelationshipDefinition Relationship,
        TableDefinition ReferencingTable,
        IReadOnlyList<EntityRecord> RelatedRecords);
}
