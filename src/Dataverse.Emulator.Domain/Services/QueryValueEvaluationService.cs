using Dataverse.Emulator.Domain.Queries;
using System.Text.RegularExpressions;

namespace Dataverse.Emulator.Domain.Services;

public sealed class QueryValueEvaluationService
{
    public bool Matches(
        ConditionOperator @operator,
        object? currentValue,
        IReadOnlyList<object?> values)
    {
        if (@operator == ConditionOperator.Equal)
        {
            return AreEqual(currentValue, values.FirstOrDefault());
        }

        if (@operator == ConditionOperator.NotEqual)
        {
            return !AreEqual(currentValue, values.FirstOrDefault());
        }

        if (@operator == ConditionOperator.Null)
        {
            return currentValue is null;
        }

        if (@operator == ConditionOperator.NotNull)
        {
            return currentValue is not null;
        }

        if (@operator == ConditionOperator.Like)
        {
            return currentValue is string text
                && values.FirstOrDefault() is string pattern
                && MatchesLike(text, pattern);
        }

        if (@operator == ConditionOperator.BeginsWith)
        {
            return currentValue is string text
                && values.FirstOrDefault() is string prefix
                && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        if (@operator == ConditionOperator.EndsWith)
        {
            return currentValue is string text
                && values.FirstOrDefault() is string suffix
                && text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (@operator == ConditionOperator.GreaterThan)
        {
            return Compare(currentValue, values.FirstOrDefault()) is > 0;
        }

        if (@operator == ConditionOperator.GreaterThanOrEqual)
        {
            return Compare(currentValue, values.FirstOrDefault()) is >= 0;
        }

        if (@operator == ConditionOperator.LessThan)
        {
            return Compare(currentValue, values.FirstOrDefault()) is < 0;
        }

        if (@operator == ConditionOperator.LessThanOrEqual)
        {
            return Compare(currentValue, values.FirstOrDefault()) is <= 0;
        }

        if (@operator == ConditionOperator.In)
        {
            return values.Any(value => AreEqual(currentValue, value));
        }

        return false;
    }

    public bool AreEqual(object? left, object? right)
    {
        if (left is string leftText && right is string rightText)
        {
            return string.Equals(leftText, rightText, StringComparison.OrdinalIgnoreCase);
        }

        return Equals(left, right);
    }

    public int? Compare(object? left, object? right)
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

    public int CompareSortableValues(object? left, object? right)
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
}
