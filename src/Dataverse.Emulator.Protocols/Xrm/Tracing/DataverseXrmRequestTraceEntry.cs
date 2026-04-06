namespace Dataverse.Emulator.Protocols.Xrm.Tracing;

public sealed record DataverseXrmRequestTraceEntry(
    long Sequence,
    string Source,
    string Name,
    bool Succeeded,
    int? ErrorCode,
    string? Message,
    DateTimeOffset StartedAtUtc,
    long DurationMilliseconds);
