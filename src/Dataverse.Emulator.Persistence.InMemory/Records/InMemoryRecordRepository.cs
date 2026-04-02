using Ardalis.Specification;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Persistence.InMemory.Records;

public sealed class InMemoryRecordRepository : InMemoryRepository<EntityRecord>, IRecordQueryService
{
    public ValueTask<PageResult<EntityRecord>> ListAsync(
        RecordQuery query,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<EntityRecord> records = Snapshot()
            .Where(record => record.TableLogicalName.Equals(query.TableLogicalName, StringComparison.OrdinalIgnoreCase));

        foreach (var condition in query.Conditions)
        {
            records = records.Where(record => Matches(record, condition));
        }

        records = ApplySorting(records, query.Sorts);

        if (query.Top is int top)
        {
            records = records.Take(top);
        }

        if (query.Page is { } page)
        {
            records = records.Take(page.Size);
        }

        var items = records
            .Select(record => Project(record, query.SelectedColumns))
            .ToArray();

        return ValueTask.FromResult(new PageResult<EntityRecord>(items));
    }

    private static bool Matches(EntityRecord record, QueryCondition condition)
    {
        if (!record.Values.TryGetValue(condition.ColumnLogicalName, out var currentValue))
        {
            return false;
        }

        if (condition.Operator == ConditionOperator.Equal)
        {
            return AreEqual(currentValue, condition.Value);
        }

        return false;
    }

    private static bool AreEqual(object? left, object? right)
    {
        if (left is string leftText && right is string rightText)
        {
            return string.Equals(leftText, rightText, StringComparison.OrdinalIgnoreCase);
        }

        return Equals(left, right);
    }

    private static IEnumerable<EntityRecord> ApplySorting(
        IEnumerable<EntityRecord> records,
        IReadOnlyList<QuerySort> sorts)
    {
        IOrderedEnumerable<EntityRecord>? ordered = null;

        foreach (var sort in sorts)
        {
            if (ordered is null)
            {
                ordered = sort.Direction == SortDirection.Ascending
                    ? records.OrderBy(record => ToSortableValue(record, sort.ColumnLogicalName), Comparer<object?>.Create(CompareValues))
                    : records.OrderByDescending(record => ToSortableValue(record, sort.ColumnLogicalName), Comparer<object?>.Create(CompareValues));

                continue;
            }

            ordered = sort.Direction == SortDirection.Ascending
                ? ordered.ThenBy(record => ToSortableValue(record, sort.ColumnLogicalName), Comparer<object?>.Create(CompareValues))
                : ordered.ThenByDescending(record => ToSortableValue(record, sort.ColumnLogicalName), Comparer<object?>.Create(CompareValues));
        }

        return ordered ?? records;
    }

    private static object? ToSortableValue(EntityRecord record, string columnLogicalName)
    {
        return record.Values.TryGetValue(columnLogicalName, out var value)
            ? value
            : null;
    }

    private static int CompareValues(object? left, object? right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left is null)
        {
            return -1;
        }

        if (right is null)
        {
            return 1;
        }

        if (left.GetType() == right.GetType() && left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        return StringComparer.OrdinalIgnoreCase.Compare(left.ToString(), right.ToString());
    }

    private static EntityRecord Project(EntityRecord record, IReadOnlyList<string> selectedColumns)
        => record.Project(selectedColumns);

    protected override string GetStorageKey(EntityRecord entity)
        => $"{entity.TableLogicalName}|{entity.Id:N}";

    protected override bool MatchesId<TId>(EntityRecord entity, TId id)
    {
        return id switch
        {
            Guid guid => entity.Id == guid,
            string compositeKey => string.Equals(GetStorageKey(entity), compositeKey, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
