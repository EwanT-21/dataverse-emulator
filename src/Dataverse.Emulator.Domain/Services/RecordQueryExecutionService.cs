using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Domain.Services;

public sealed class RecordQueryExecutionService
{
    private readonly QueryValueEvaluationService queryValueEvaluationService;
    private readonly ContinuationPagingService continuationPagingService;

    public RecordQueryExecutionService()
        : this(
            new QueryValueEvaluationService(),
            new ContinuationPagingService())
    {
    }

    public RecordQueryExecutionService(
        QueryValueEvaluationService queryValueEvaluationService,
        ContinuationPagingService continuationPagingService)
    {
        this.queryValueEvaluationService = queryValueEvaluationService;
        this.continuationPagingService = continuationPagingService;
    }

    public PageResult<EntityRecord> Execute(
        RecordQuery query,
        IReadOnlyCollection<EntityRecord> records)
    {
        var filteredRecords = records
            .Where(record => record.TableLogicalName.Equals(query.TableLogicalName, StringComparison.OrdinalIgnoreCase));

        if (query.Filter is not null)
        {
            filteredRecords = filteredRecords.Where(record => Matches(record, query.Filter));
        }

        var sortedRecords = ApplySorting(filteredRecords, query.Sorts).ToArray();

        if (query.Top is int top)
        {
            sortedRecords = sortedRecords.Take(top).ToArray();
        }

        var pageResult = continuationPagingService.Apply(sortedRecords, query.Page);
        var projectedItems = pageResult.Items
            .Select(record => record.Project(query.SelectedColumns))
            .ToArray();

        return new PageResult<EntityRecord>(
            projectedItems,
            pageResult.ContinuationToken,
            pageResult.TotalCount);
    }

    private bool Matches(EntityRecord record, QueryFilter filter)
    {
        var results = filter.Conditions
            .Select(condition => Matches(record, condition))
            .Concat(filter.Filters.Select(childFilter => Matches(record, childFilter)))
            .ToArray();

        if (results.Length == 0)
        {
            return true;
        }

        return filter.Operator == FilterOperator.Or
            ? results.Any(result => result)
            : results.All(result => result);
    }

    private bool Matches(EntityRecord record, QueryCondition condition)
    {
        record.Values.TryGetValue(condition.ColumnLogicalName, out var currentValue);

        return queryValueEvaluationService.Matches(
            condition.Operator,
            currentValue,
            condition.Values);
    }

    private IEnumerable<EntityRecord> ApplySorting(
        IEnumerable<EntityRecord> records,
        IReadOnlyList<QuerySort> sorts)
    {
        if (sorts.Count == 0)
        {
            return records.OrderBy(record => record.Id);
        }

        IOrderedEnumerable<EntityRecord>? ordered = null;

        foreach (var sort in sorts)
        {
            if (ordered is null)
            {
                ordered = sort.Direction == SortDirection.Ascending
                    ? records.OrderBy(
                        record => ToSortableValue(record, sort.ColumnLogicalName),
                        Comparer<object?>.Create(queryValueEvaluationService.CompareSortableValues))
                    : records.OrderByDescending(
                        record => ToSortableValue(record, sort.ColumnLogicalName),
                        Comparer<object?>.Create(queryValueEvaluationService.CompareSortableValues));

                continue;
            }

            ordered = sort.Direction == SortDirection.Ascending
                ? ordered.ThenBy(
                    record => ToSortableValue(record, sort.ColumnLogicalName),
                    Comparer<object?>.Create(queryValueEvaluationService.CompareSortableValues))
                : ordered.ThenByDescending(
                    record => ToSortableValue(record, sort.ColumnLogicalName),
                    Comparer<object?>.Create(queryValueEvaluationService.CompareSortableValues));
        }

        return ordered ?? records;
    }

    private static object? ToSortableValue(EntityRecord record, string columnLogicalName)
    {
        return record.Values.TryGetValue(columnLogicalName, out var value)
            ? value
            : null;
    }
}
