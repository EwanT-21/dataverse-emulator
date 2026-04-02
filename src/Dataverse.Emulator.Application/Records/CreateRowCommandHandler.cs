using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Domain.Services;

namespace Dataverse.Emulator.Application.Records;

public sealed class CreateRowCommandHandler(
        IMetadataRepository metadataRepository,
        IRecordRepository recordRepository,
        RecordValidationService recordValidationService)
{
    public async ValueTask<Guid> HandleAsync(
        CreateRowCommand command,
        CancellationToken cancellationToken = default)
    {
        var table = await metadataRepository.GetTableAsync(command.TableLogicalName, cancellationToken);
        if (table is null)
        {
            throw new InvalidOperationException($"Unknown table '{command.TableLogicalName}'.");
        }

        var errors = recordValidationService.ValidateCreate(table, command.Values);
        if (errors.Count > 0)
        {
            throw new DomainValidationException(errors);
        }

        var id = ResolveId(table.PrimaryIdAttribute, command.Values);
        var values = new Dictionary<string, object?>(command.Values, StringComparer.OrdinalIgnoreCase)
        {
            [table.PrimaryIdAttribute] = id
        };

        var record = new EntityRecord(
            table.LogicalName,
            id,
            new RecordValues(values));

        await recordRepository.CreateAsync(record, cancellationToken);
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
