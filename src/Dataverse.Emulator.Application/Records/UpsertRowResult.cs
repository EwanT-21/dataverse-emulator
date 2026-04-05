namespace Dataverse.Emulator.Application.Records;

public sealed record UpsertRowResult(
    Guid Id,
    bool RecordCreated);
