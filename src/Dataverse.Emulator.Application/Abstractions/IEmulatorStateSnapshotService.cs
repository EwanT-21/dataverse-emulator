namespace Dataverse.Emulator.Application.Abstractions;

public interface IEmulatorStateSnapshotService
{
    ValueTask<EmulatorStateSnapshot> CaptureAsync(CancellationToken cancellationToken = default);

    ValueTask RestoreAsync(EmulatorStateSnapshot snapshot, CancellationToken cancellationToken = default);
}
