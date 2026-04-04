using Ardalis.Specification;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Dataverse.Emulator.Persistence.InMemory.Records;

public sealed class InMemoryRecordRepository : InMemoryRepository<EntityRecord>, IRecordQueryService
{
    public ValueTask<PageResult<EntityRecord>> ListAsync(
        RecordQuery query,
        CancellationToken cancellationToken = default)
    {
        var records = Snapshot()
            .Where(record => record.TableLogicalName.Equals(query.TableLogicalName, StringComparison.OrdinalIgnoreCase));

        if (query.Filter is not null)
        {
            records = records.Where(record => Matches(record, query.Filter));
        }

        var sortedRecords = ApplySorting(records, query.Sorts).ToArray();

        if (query.Top is int top)
        {
            sortedRecords = sortedRecords.Take(top).ToArray();
        }

        if (query.Page is { } page)
        {
            var offset = DecodeContinuationToken(page.ContinuationToken);
            var pagedRecords = sortedRecords
                .Skip(offset)
                .Take(page.Size)
                .ToArray();
            var nextOffset = offset + pagedRecords.Length;
            var continuationToken = nextOffset < sortedRecords.Length
                ? EncodeContinuationToken(nextOffset)
                : null;

            var pageItems = pagedRecords
                .Select(record => Project(record, query.SelectedColumns))
                .ToArray();

            return ValueTask.FromResult(new PageResult<EntityRecord>(pageItems, continuationToken));
        }

        var items = sortedRecords
            .Select(record => Project(record, query.SelectedColumns))
            .ToArray();

        return ValueTask.FromResult(new PageResult<EntityRecord>(items));
    }

    private static bool Matches(EntityRecord record, QueryFilter filter)
    {
        var results = filter.Conditions.Select(condition => Matches(record, condition))
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

    private static bool Matches(EntityRecord record, QueryCondition condition)
    {
        record.Values.TryGetValue(condition.ColumnLogicalName, out var currentValue);

        if (condition.Operator == ConditionOperator.Equal)
        {
            return AreEqual(currentValue, condition.Value);
        }

        if (condition.Operator == ConditionOperator.NotEqual)
        {
            return !AreEqual(currentValue, condition.Value);
        }

        if (condition.Operator == ConditionOperator.Null)
        {
            return currentValue is null;
        }

        if (condition.Operator == ConditionOperator.NotNull)
        {
            return currentValue is not null;
        }

        if (condition.Operator == ConditionOperator.Like)
        {
            return currentValue is string text
                && condition.Value is string pattern
                && MatchesLike(text, pattern);
        }

        if (condition.Operator == ConditionOperator.BeginsWith)
        {
            return currentValue is string text
                && condition.Value is string prefix
                && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        if (condition.Operator == ConditionOperator.EndsWith)
        {
            return currentValue is string text
                && condition.Value is string suffix
                && text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (condition.Operator == ConditionOperator.GreaterThan)
        {
            return Compare(currentValue, condition.Value) is > 0;
        }

        if (condition.Operator == ConditionOperator.GreaterThanOrEqual)
        {
            return Compare(currentValue, condition.Value) is >= 0;
        }

        if (condition.Operator == ConditionOperator.LessThan)
        {
            return Compare(currentValue, condition.Value) is < 0;
        }

        if (condition.Operator == ConditionOperator.LessThanOrEqual)
        {
            return Compare(currentValue, condition.Value) is <= 0;
        }

        if (condition.Operator == ConditionOperator.In)
        {
            return condition.Values.Any(value => AreEqual(currentValue, value));
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

    private static bool MatchesLike(string input, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("%", ".*", StringComparison.Ordinal)
            .Replace("_", ".", StringComparison.Ordinal) + "$";

        return Regex.IsMatch(
            input,
            regexPattern,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static int? Compare(object? left, object? right)
    {
        if (left is null || right is null)
        {
            return null;
        }

        if (left is string leftText && right is string rightText)
        {
            return StringComparer.OrdinalIgnoreCase.Compare(leftText, rightText);
        }

        if (TryToDecimal(left, out var leftDecimal) && TryToDecimal(right, out var rightDecimal))
        {
            return leftDecimal.CompareTo(rightDecimal);
        }

        if (TryToDateTimeOffset(left, out var leftDateTimeOffset) && TryToDateTimeOffset(right, out var rightDateTimeOffset))
        {
            return leftDateTimeOffset.CompareTo(rightDateTimeOffset);
        }

        if (left.GetType() == right.GetType() && left is IComparable comparable)
        {
            return comparable.CompareTo(right);
        }

        return StringComparer.OrdinalIgnoreCase.Compare(left.ToString(), right.ToString());
    }

    private static IEnumerable<EntityRecord> ApplySorting(
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

    private static bool TryToDecimal(object value, out decimal decimalValue)
    {
        switch (value)
        {
            case decimal exact:
                decimalValue = exact;
                return true;
            case double floating:
                decimalValue = (decimal)floating;
                return true;
            case float single:
                decimalValue = (decimal)single;
                return true;
            case int integer:
                decimalValue = integer;
                return true;
            case long longInteger:
                decimalValue = longInteger;
                return true;
            default:
                decimalValue = default;
                return false;
        }
    }

    private static bool TryToDateTimeOffset(object value, out DateTimeOffset dateTimeOffset)
    {
        switch (value)
        {
            case DateTimeOffset exact:
                dateTimeOffset = exact;
                return true;
            case DateTime dateTime:
                dateTimeOffset = dateTime.Kind switch
                {
                    DateTimeKind.Utc => new DateTimeOffset(dateTime),
                    DateTimeKind.Local => new DateTimeOffset(dateTime.ToUniversalTime()),
                    _ => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc))
                };
                return true;
            default:
                dateTimeOffset = default;
                return false;
        }
    }

    private static EntityRecord Project(EntityRecord record, IReadOnlyList<string> selectedColumns)
        => record.Project(selectedColumns);

    private static string EncodeContinuationToken(int offset)
        => offset.ToString(CultureInfo.InvariantCulture);

    private static int DecodeContinuationToken(string? continuationToken)
    {
        var normalizedToken = continuationToken?
            .Split('|', 2, StringSplitOptions.TrimEntries)[0];

        return int.TryParse(normalizedToken, NumberStyles.None, CultureInfo.InvariantCulture, out var offset) && offset > 0
            ? offset
            : 0;
    }

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
