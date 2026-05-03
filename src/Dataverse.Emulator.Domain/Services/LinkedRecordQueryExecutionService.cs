using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Domain.Services;

public sealed class LinkedRecordQueryExecutionService
{
    private readonly QueryValueEvaluationService queryValueEvaluationService;
    private readonly ContinuationPagingService continuationPagingService;

    public LinkedRecordQueryExecutionService()
        : this(
            new QueryValueEvaluationService(),
            new ContinuationPagingService())
    {
    }

    public LinkedRecordQueryExecutionService(
        QueryValueEvaluationService queryValueEvaluationService,
        ContinuationPagingService continuationPagingService)
    {
        this.queryValueEvaluationService = queryValueEvaluationService;
        this.continuationPagingService = continuationPagingService;
    }

    public PageResult<LinkedEntityRecord> Execute(
        LinkedRecordQuery query,
        TableDefinition rootTable,
        IReadOnlyDictionary<string, IReadOnlyList<EntityRecord>> rowsByTable)
    {
        var contexts = rowsByTable[rootTable.LogicalName]
            .Select(rootRecord => new LinkedEntityContext(rootRecord))
            .ToList();

        foreach (var join in query.Joins)
        {
            contexts = JoinContexts(
                contexts,
                rootTable.LogicalName,
                join,
                rowsByTable[join.TableLogicalName]);
        }

        if (query.Filter is not null)
        {
            contexts = contexts
                .Where(context => Matches(context, rootTable.LogicalName, query.Filter))
                .ToList();
        }

        var sortedContexts = ApplySorting(contexts, rootTable.LogicalName, query.Sorts).ToArray();

        if (query.Top is int top)
        {
            sortedContexts = sortedContexts.Take(top).ToArray();
        }

        var pageResult = continuationPagingService.Apply(sortedContexts, query.Page);
        var items = pageResult.Items
            .Select(context => ToRecord(query, context))
            .ToArray();

        return new PageResult<LinkedEntityRecord>(
            items,
            pageResult.ContinuationToken,
            pageResult.TotalCount);
    }

    private List<LinkedEntityContext> JoinContexts(
        IReadOnlyList<LinkedEntityContext> contexts,
        string rootScopeName,
        LinkedRecordJoin join,
        IReadOnlyList<EntityRecord> linkedRows)
    {
        var joinedContexts = new List<LinkedEntityContext>();
        var parentScopeName = ResolveParentScopeName(join, rootScopeName);

        foreach (var context in contexts)
        {
            var matched = false;

            if (TryGetScopedValue(context, rootScopeName, parentScopeName, join.FromAttributeName, out var leftValue)
                && leftValue is not null)
            {
                foreach (var linkedRow in linkedRows)
                {
                    if (!linkedRow.Values.TryGetValue(join.ToAttributeName, out var rightValue)
                        || rightValue is null
                        || !queryValueEvaluationService.AreEqual(leftValue, rightValue))
                    {
                        continue;
                    }

                    var expandedContext = context.WithLinkedRecord(join.Alias, linkedRow);
                    if (join.Filter is not null && !Matches(expandedContext, rootScopeName, join.Filter))
                    {
                        continue;
                    }

                    joinedContexts.Add(expandedContext);
                    matched = true;
                }
            }

            if (!matched && join.JoinType == LinkedRecordJoinType.LeftOuter)
            {
                joinedContexts.Add(context);
            }
        }

        return joinedContexts;
    }

    private bool Matches(
        LinkedEntityContext context,
        string rootScopeName,
        LinkedRecordFilter filter)
    {
        var results = filter.Conditions
            .Select(condition => Matches(context, rootScopeName, condition))
            .Concat(filter.Filters.Select(childFilter => Matches(context, rootScopeName, childFilter)))
            .ToArray();

        if (results.Length == 0)
        {
            return true;
        }

        return filter.Operator == FilterOperator.Or
            ? results.Any(result => result)
            : results.All(result => result);
    }

    private bool Matches(
        LinkedEntityContext context,
        string rootScopeName,
        LinkedRecordCondition condition)
    {
        var currentValue = TryGetScopedValue(context, rootScopeName, condition.ScopeName, condition.ColumnLogicalName, out var value)
            ? value
            : null;

        return queryValueEvaluationService.Matches(
            condition.Operator,
            currentValue,
            condition.Values);
    }

    private IEnumerable<LinkedEntityContext> ApplySorting(
        IEnumerable<LinkedEntityContext> contexts,
        string rootScopeName,
        IReadOnlyList<LinkedRecordSort> sorts)
    {
        if (sorts.Count == 0)
        {
            return contexts.OrderBy(context => context.RootRecord.Id);
        }

        IOrderedEnumerable<LinkedEntityContext>? ordered = null;

        foreach (var sort in sorts)
        {
            if (ordered is null)
            {
                ordered = sort.Direction == SortDirection.Ascending
                    ? contexts.OrderBy(
                        context => ToSortableValue(context, rootScopeName, sort),
                        Comparer<object?>.Create(queryValueEvaluationService.CompareSortableValues))
                    : contexts.OrderByDescending(
                        context => ToSortableValue(context, rootScopeName, sort),
                        Comparer<object?>.Create(queryValueEvaluationService.CompareSortableValues));

                continue;
            }

            ordered = sort.Direction == SortDirection.Ascending
                ? ordered.ThenBy(
                    context => ToSortableValue(context, rootScopeName, sort),
                    Comparer<object?>.Create(queryValueEvaluationService.CompareSortableValues))
                : ordered.ThenByDescending(
                    context => ToSortableValue(context, rootScopeName, sort),
                    Comparer<object?>.Create(queryValueEvaluationService.CompareSortableValues));
        }

        return ordered ?? contexts;
    }

    private static object? ToSortableValue(
        LinkedEntityContext context,
        string rootScopeName,
        LinkedRecordSort sort)
    {
        return TryGetScopedValue(context, rootScopeName, sort.ScopeName, sort.ColumnLogicalName, out var value)
            ? value
            : null;
    }

    private static LinkedEntityRecord ToRecord(
        LinkedRecordQuery query,
        LinkedEntityContext context)
    {
        var projectedRoot = query.RootSelectedColumns.Count == 0
            ? context.RootRecord
            : context.RootRecord.Project(query.RootSelectedColumns);

        return new LinkedEntityRecord(projectedRoot, context.LinkedRecords);
    }

    private static bool TryGetScopedValue(
        LinkedEntityContext context,
        string rootScopeName,
        string scopeName,
        string columnLogicalName,
        out object? value)
    {
        if (scopeName.Equals(rootScopeName, StringComparison.OrdinalIgnoreCase))
        {
            return context.RootRecord.Values.TryGetValue(columnLogicalName, out value);
        }

        if (context.LinkedRecords.TryGetValue(scopeName, out var linkedRecord))
        {
            return linkedRecord.Values.TryGetValue(columnLogicalName, out value);
        }

        value = null;
        return false;
    }

    private static string ResolveParentScopeName(
        LinkedRecordJoin join,
        string rootScopeName)
        => string.IsNullOrWhiteSpace(join.ParentScopeName)
            ? rootScopeName
            : join.ParentScopeName;

    private sealed class LinkedEntityContext(EntityRecord rootRecord)
    {
        private readonly Dictionary<string, EntityRecord> linkedRecords = new(StringComparer.OrdinalIgnoreCase);

        public EntityRecord RootRecord { get; } = rootRecord;

        public IReadOnlyDictionary<string, EntityRecord> LinkedRecords => linkedRecords;

        public LinkedEntityContext WithLinkedRecord(string alias, EntityRecord record)
        {
            var copy = new LinkedEntityContext(RootRecord);

            foreach (var pair in linkedRecords)
            {
                copy.linkedRecords[pair.Key] = pair.Value;
            }

            copy.linkedRecords[alias] = record;
            return copy;
        }
    }
}
