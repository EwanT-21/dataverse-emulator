using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed record UpsertRowCommand(
    string TableLogicalName,
    Guid? Id,
    IReadOnlyDictionary<string, object?> Values) : ICommand<ErrorOr<UpsertRowResult>>;
