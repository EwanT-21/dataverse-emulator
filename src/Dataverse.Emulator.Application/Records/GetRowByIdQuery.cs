namespace Dataverse.Emulator.Application.Records;

public sealed record GetRowByIdQuery(
    string TableLogicalName,
    Guid Id);
