using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class UpsertRowCommandHandler(
    IReadRepository<TableDefinition> tableRepository,
    IRepository<EntityRecord> recordRepository,
    RecordValidationService recordValidationService)
    : ICommandHandler<UpsertRowCommand, ErrorOr<UpsertRowResult>>
{
    public async ValueTask<ErrorOr<UpsertRowResult>> Handle(
        UpsertRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var table = await tableRepository.SingleOrDefaultAsync(
            new TableByLogicalNameSpecification(command.TableLogicalName),
            cancellationToken);

        if (table is null)
        {
            return DomainErrors.UnknownTable(command.TableLogicalName);
        }

        var resolvedIdResult = ResolveId(table, command);
        if (resolvedIdResult.IsError)
        {
            return resolvedIdResult.Errors;
        }

        var resolvedId = resolvedIdResult.Value;

        var existing = await recordRepository.SingleOrDefaultAsync(
            new RecordByIdSpecification(command.TableLogicalName, resolvedId),
            cancellationToken);

        return existing is not null
            ? await UpdateExistingAsync(table, existing, command.Values, resolvedId, cancellationToken)
            : await CreateNewAsync(table, command.Values, resolvedId, cancellationToken);
    }

    private async ValueTask<ErrorOr<UpsertRowResult>> UpdateExistingAsync(
        TableDefinition table,
        EntityRecord existing,
        IReadOnlyDictionary<string, object?> values,
        Guid id,
        CancellationToken cancellationToken)
    {
        var updateValues = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        updateValues.Remove(table.PrimaryIdAttribute);

        var errors = recordValidationService.ValidateUpdate(table, updateValues);
        if (errors.Count > 0)
        {
            return errors.ToList();
        }

        var updated = existing.ApplyChanges(updateValues);
        await recordRepository.UpdateAsync(updated, cancellationToken);
        return new UpsertRowResult(id, RecordCreated: false);
    }

    private async ValueTask<ErrorOr<UpsertRowResult>> CreateNewAsync(
        TableDefinition table,
        IReadOnlyDictionary<string, object?> values,
        Guid id,
        CancellationToken cancellationToken)
    {
        var createValues = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase)
        {
            [table.PrimaryIdAttribute] = id
        };

        var errors = recordValidationService.ValidateCreate(table, createValues);
        if (errors.Count > 0)
        {
            return errors.ToList();
        }

        var recordValues = RecordValues.Create(createValues);
        if (recordValues.IsError)
        {
            return recordValues.Errors;
        }

        var record = EntityRecord.Create(table.LogicalName, id, recordValues.Value);
        if (record.IsError)
        {
            return record.Errors;
        }

        await recordRepository.AddAsync(record.Value, cancellationToken);
        return new UpsertRowResult(id, RecordCreated: true);
    }

    private static ErrorOr<Guid> ResolveId(
        TableDefinition table,
        UpsertRowCommand command)
    {
        if (command.Id.HasValue)
        {
            if (command.Values.TryGetValue(table.PrimaryIdAttribute, out var rawPrimaryId)
                && rawPrimaryId is Guid suppliedPrimaryId
                && suppliedPrimaryId != command.Id.Value)
            {
                return DomainErrors.Validation(
                    "Records.Row.IdConflict",
                    $"Primary id '{table.PrimaryIdAttribute}' must match the explicit row id when both are supplied.");
            }

            return command.Id.Value;
        }

        if (command.Values.TryGetValue(table.PrimaryIdAttribute, out var rawValue) && rawValue is Guid suppliedId)
        {
            return suppliedId;
        }

        return Guid.NewGuid();
    }
}
