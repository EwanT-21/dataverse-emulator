using Dataverse.Emulator.Domain.Queries;
using ErrorOr;
using Mediator;

namespace Dataverse.Emulator.Application.Records;

public sealed record ListLinkedRowsQuery(LinkedRecordQuery Query) : IQuery<ErrorOr<PageResult<LinkedEntityRecord>>>;
