namespace Dataverse.Emulator.Application.Records;

public sealed record CreateRowCommand(
    string TableLogicalName,
    IReadOnlyDictionary<string, object?> Values);
