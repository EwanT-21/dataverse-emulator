using Dataverse.Emulator.Domain.Records;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed record UpdateRowCommand(
    string TableLogicalName,
    Guid Id,
    IReadOnlyDictionary<string, object?> Values) : ICommand<ErrorOr<EntityRecord>>;
