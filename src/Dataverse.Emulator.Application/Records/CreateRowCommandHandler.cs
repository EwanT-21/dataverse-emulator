using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Common;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Metadata.Specifications;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Records.Specifications;
using Dataverse.Emulator.Domain.Services;
using ErrorOr;
using FluentValidation;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed class CreateRowCommandHandler(
    IReadRepository<TableDefinition> tableRepository,
    IRepository<EntityRecord> recordRepository,
    IValidator<CreateRowCommand> validator,
    RecordValidationService recordValidationService)
    : ICommandHandler<CreateRowCommand, ErrorOr<Guid>>
{
    public async ValueTask<ErrorOr<Guid>> Handle(
        CreateRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }

        var table = await tableRepository.SingleOrDefaultAsync(
            new TableByLogicalNameSpecification(command.TableLogicalName),
            cancellationToken);

        if (table is null)
        {
            return DomainErrors.UnknownTable(command.TableLogicalName);
        }

        var errors = recordValidationService.ValidateCreate(table, command.Values);
        if (errors.Count > 0)
        {
            return errors.ToList();
        }

        var id = ResolveId(table.PrimaryIdAttribute, command.Values);
        var values = new Dictionary<string, object?>(command.Values, StringComparer.OrdinalIgnoreCase)
        {
            [table.PrimaryIdAttribute] = id
        };

        var existing = await recordRepository.AnyAsync(
            new RecordByIdSpecification(table.LogicalName, id),
            cancellationToken);

        if (existing)
        {
            return DomainErrors.DuplicateRow(table.LogicalName, id);
        }

        var recordValues = RecordValues.Create(values);
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
        return id;
    }

    private static Guid ResolveId(
        string primaryIdAttribute,
        IReadOnlyDictionary<string, object?> values)
    {
        if (values.TryGetValue(primaryIdAttribute, out var suppliedId) && suppliedId is Guid id)
        {
            return id;
        }

        return Guid.NewGuid();
    }
}
