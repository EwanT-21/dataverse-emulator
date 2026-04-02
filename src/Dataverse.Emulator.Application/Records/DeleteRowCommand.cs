using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed record DeleteRowCommand(
    string TableLogicalName,
    Guid Id) : ICommand<ErrorOr<bool>>;
