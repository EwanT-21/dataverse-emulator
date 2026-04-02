using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed record ListRowsQuery(RecordQuery Query) : IQuery<ErrorOr<PageResult<EntityRecord>>>;
