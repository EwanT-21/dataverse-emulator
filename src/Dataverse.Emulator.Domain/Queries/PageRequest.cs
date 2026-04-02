namespace Dataverse.Emulator.Domain.Queries;

public sealed record PageRequest(
    int Size = 50,
    string? ContinuationToken = null);
