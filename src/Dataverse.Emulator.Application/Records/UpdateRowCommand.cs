namespace Dataverse.Emulator.Application.Records;

public sealed record UpdateRowCommand(
    string TableLogicalName,
    Guid Id,
    IReadOnlyDictionary<string, object?> Values);
