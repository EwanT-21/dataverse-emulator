using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Persistence.InMemory.Metadata;
using Dataverse.Emulator.Persistence.InMemory.Records;

namespace Dataverse.Emulator.Persistence.InMemory;

internal sealed class InMemoryEmulatorStateSnapshotService(
    InMemoryMetadataRepository metadataRepository,
    InMemoryRecordRepository recordRepository)
    : IEmulatorStateSnapshotService
{
    public ValueTask<EmulatorStateSnapshot> CaptureAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(new EmulatorStateSnapshot(
            metadataRepository.CaptureEntities().ToArray(),
            recordRepository.CaptureEntities().ToArray()));
    }

    public ValueTask RestoreAsync(EmulatorStateSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(snapshot);

        metadataRepository.ReplaceAll(snapshot.Tables);
        recordRepository.ReplaceAll(snapshot.Records);
        return ValueTask.CompletedTask;
    }
}
