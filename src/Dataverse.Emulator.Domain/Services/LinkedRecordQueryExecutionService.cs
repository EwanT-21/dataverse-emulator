using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Dataverse.Emulator.Domain.Services;

public sealed class LinkedRecordQueryExecutionService
{
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

        string? continuationToken = null;
        var pagedContexts = sortedContexts;

        if (query.Page is { } page)
        {
            var offset = DecodeContinuationToken(page.ContinuationToken);
            pagedContexts = sortedContexts
                .Skip(offset)
                .Take(page.Size)
                .ToArray();

            var nextOffset = offset + pagedContexts.Length;
            continuationToken = nextOffset < sortedContexts.Length
                ? EncodeContinuationToken(nextOffset)
                : null;
        }

        var items = pagedContexts
            .Select(context => ToRecord(query, context))
            .ToArray();

        return new PageResult<LinkedEntityRecord>(items, continuationToken);
    }

    private static List<LinkedEntityContext> JoinContexts(
        IReadOnlyList<LinkedEntityContext> contexts,
        string rootScopeName,
        LinkedRecordJoin join,
        IReadOnlyList<EntityRecord> linkedRows)
    {
        var joinedContexts = new List<LinkedEntityContext>();

        foreach (var context in contexts)
        {
            if (!context.RootRecord.Values.TryGetValue(join.FromAttributeName, out var leftValue)
                || leftValue is null)
            {
                continue;
            }

            foreach (var linkedRow in linkedRows)
            {
                if (!linkedRow.Values.TryGetValue(join.ToAttributeName, out var rightValue)
                    || rightValue is null
                    || !AreEqual(leftValue, rightValue))
                {
                    continue;
                }

                var expandedContext = context.WithLinkedRecord(join.Alias, linkedRow);
                if (join.Filter is not null && !Matches(expandedContext, rootScopeName, join.Filter))
                {
                    continue;
                }

                joinedContexts.Add(expandedContext);
            }
        }

        return joinedContexts;
    }

    private static bool Matches(
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

    private static bool Matches(
        LinkedEntityContext context,
        string rootScopeName,
        LinkedRecordCondition condition)
    {
        var currentValue = TryGetScopedValue(context, rootScopeName, condition.ScopeName, condition.ColumnLogicalName, out var value)
            ? value
            : null;

        if (condition.Operator == ConditionOperator.Equal)
        {
            return AreEqual(currentValue, condition.Values.FirstOrDefault());
        }

        if (condition.Operator == ConditionOperator.NotEqual)
        {
            return !AreEqual(currentValue, condition.Values.FirstOrDefault());
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
                && condition.Values.FirstOrDefault() is string pattern
                && MatchesLike(text, pattern);
        }

        if (condition.Operator == ConditionOperator.BeginsWith)
        {
            return currentValue is string text
                && condition.Values.FirstOrDefault() is string prefix
                && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        if (condition.Operator == ConditionOperator.EndsWith)
        {
            return currentValue is string text
                && condition.Values.FirstOrDefault() is string suffix
                && text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (condition.Operator == ConditionOperator.GreaterThan)
        {
            return Compare(currentValue, condition.Values.FirstOrDefault()) is > 0;
        }

        if (condition.Operator == ConditionOperator.GreaterThanOrEqual)
        {
            return Compare(currentValue, condition.Values.FirstOrDefault()) is >= 0;
        }

        if (condition.Operator == ConditionOperator.LessThan)
        {
            return Compare(currentValue, condition.Values.FirstOrDefault()) is < 0;
        }

        if (condition.Operator == ConditionOperator.LessThanOrEqual)
        {
            return Compare(currentValue, condition.Values.FirstOrDefault()) is <= 0;
        }

        if (condition.Operator == ConditionOperator.In)
        {
            return condition.Values.Any(value => AreEqual(currentValue, value));
        }

        return false;
    }

    private static IEnumerable<LinkedEntityContext> ApplySorting(
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
                        Comparer<object?>.Create(CompareValues))
                    : contexts.OrderByDescending(
                        context => ToSortableValue(context, rootScopeName, sort),
                        Comparer<object?>.Create(CompareValues));

                continue;
            }

            ordered = sort.Direction == SortDirection.Ascending
                ? ordered.ThenBy(
                    context => ToSortableValue(context, rootScopeName, sort),
                    Comparer<object?>.Create(CompareValues))
                : ordered.ThenByDescending(
                    context => ToSortableValue(context, rootScopeName, sort),
                    Comparer<object?>.Create(CompareValues));
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
