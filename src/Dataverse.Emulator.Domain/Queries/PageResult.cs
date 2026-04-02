namespace Dataverse.Emulator.Domain.Queries;

public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    string? ContinuationToken = null,
    int? TotalCount = null);
