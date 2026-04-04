using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Queries;

public sealed class QueryFilter
{
    internal QueryFilter(
        FilterOperator @operator,
        IReadOnlyList<QueryCondition> conditions,
        IReadOnlyList<QueryFilter> filters)
    {
        Operator = @operator;
        Conditions = conditions;
        Filters = filters;
    }

    public FilterOperator Operator { get; }

    public IReadOnlyList<QueryCondition> Conditions { get; }

    public IReadOnlyList<QueryFilter> Filters { get; }

    public static ErrorOr<QueryFilter> Create(
        FilterOperator @operator,
        IReadOnlyList<QueryCondition>? conditions = null,
        IReadOnlyList<QueryFilter>? filters = null)
    {
        return new QueryFilter(
            @operator,
            conditions ?? Array.Empty<QueryCondition>(),
            filters ?? Array.Empty<QueryFilter>());
    }
}
