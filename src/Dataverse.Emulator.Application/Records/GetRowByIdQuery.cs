using Dataverse.Emulator.Domain.Records;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed record GetRowByIdQuery(
    string TableLogicalName,
    Guid Id) : IQuery<ErrorOr<EntityRecord>>;
