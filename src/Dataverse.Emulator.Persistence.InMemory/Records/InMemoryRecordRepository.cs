using System.Collections.Concurrent;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;

namespace Dataverse.Emulator.Persistence.InMemory.Records;

public sealed class InMemoryRecordRepository : IRecordRepository
{
    private readonly ConcurrentDictionary<string, InMemoryTableStore> tableStores = new(StringComparer.OrdinalIgnoreCase);

    public ValueTask<EntityRecord?> GetAsync(
        string tableLogicalName,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!tableStores.TryGetValue(tableLogicalName, out var tableStore))
        {
            return ValueTask.FromResult<EntityRecord?>(null);
        }

        return ValueTask.FromResult(tableStore.Get(id));
    }

    public ValueTask<PageResult<EntityRecord>> ListAsync(
        RecordQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!tableStores.TryGetValue(query.TableLogicalName, out var tableStore))
        {
            return ValueTask.FromResult(new PageResult<EntityRecord>(Array.Empty<EntityRecord>()));
        }

        IEnumerable<EntityRecord> records = tableStore.Snapshot();

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

    public ValueTask CreateAsync(
        EntityRecord record,
        CancellationToken cancellationToken = default)
    {
        GetOrCreateStore(record.TableLogicalName).Create(record);
        return ValueTask.CompletedTask;
    }

    public ValueTask UpdateAsync(
        EntityRecord record,
        CancellationToken cancellationToken = default)
    {
        GetOrCreateStore(record.TableLogicalName).Update(record);
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> DeleteAsync(
        string tableLogicalName,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (!tableStores.TryGetValue(tableLogicalName, out var tableStore))
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(tableStore.Delete(id));
    }

    public ValueTask ResetAsync(
        IEnumerable<EntityRecord> records,
        CancellationToken cancellationToken = default)
    {
        tableStores.Clear();

        foreach (var group in records.GroupBy(record => record.TableLogicalName, StringComparer.OrdinalIgnoreCase))
        {
            var tableStore = GetOrCreateStore(group.Key);
            tableStore.Reset(group);
        }

        return ValueTask.CompletedTask;
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
    {
        if (selectedColumns.Count == 0)
        {
            return record;
        }

        var values = selectedColumns
            .Where(record.Values.Contains)
            .Select(column => new KeyValuePair<string, object?>(column, record.Values[column]));

        return new EntityRecord(record.TableLogicalName, record.Id, new RecordValues(values), record.Version);
    }

    private InMemoryTableStore GetOrCreateStore(string tableLogicalName)
    {
        return tableStores.GetOrAdd(tableLogicalName, _ => new InMemoryTableStore());
    }
}
