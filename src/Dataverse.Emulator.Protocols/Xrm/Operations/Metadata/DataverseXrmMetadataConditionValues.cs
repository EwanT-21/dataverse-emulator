using System.Collections;
using System.Globalization;

namespace Dataverse.Emulator.Protocols.Xrm.Operations.Metadata;

internal static class DataverseXrmMetadataConditionValues
{
    public static IEnumerable<object?> Enumerate(object? value)
    {
        if (value is null)
        {
            yield return null;
            yield break;
        }

        if (value is string)
        {
            yield return value;
            yield break;
        }

        if (value is IEnumerable values)
        {
            foreach (var item in values)
            {
                yield return item;
            }

            yield break;
        }

        yield return value;
    }

    public static bool ValuesEqual(object? actual, object? expected)
    {
        if (actual is null || expected is null)
        {
            return actual is null && expected is null;
        }

        if (actual is string actualString)
        {
            return string.Equals(actualString, Convert.ToString(expected), StringComparison.OrdinalIgnoreCase);
        }

        if (actual is Guid actualGuid)
        {
            return expected switch
            {
                Guid expectedGuid => actualGuid == expectedGuid,
                string expectedGuidString when Guid.TryParse(expectedGuidString, out var parsedGuid) => actualGuid == parsedGuid,
                _ => false
            };
        }

        if (actual is bool actualBoolean)
        {
            return expected switch
            {
                bool expectedBoolean => actualBoolean == expectedBoolean,
                string expectedBooleanString when bool.TryParse(expectedBooleanString, out var parsedBoolean) => actualBoolean == parsedBoolean,
                _ => false
            };
        }

        if (actual is int actualInt32)
        {
            return expected switch
            {
                int expectedInt32 => actualInt32 == expectedInt32,
                string expectedInt32String when int.TryParse(expectedInt32String, out var parsedInt32) => actualInt32 == parsedInt32,
                _ => false
            };
        }

        return actual.Equals(expected);
    }

    public static bool TryCompare(object? actual, object? expected, out int comparison)
    {
        comparison = 0;

        if (actual is null || expected is null)
        {
            return false;
        }

        if (actual is string actualString)
        {
            comparison = string.Compare(
                actualString,
                Convert.ToString(expected, CultureInfo.InvariantCulture),
                StringComparison.OrdinalIgnoreCase);
            return true;
        }

        if (actual is int actualInt32)
        {
            if (!TryConvertInt32(expected, out var expectedInt32))
            {
                return false;
            }

            comparison = actualInt32.CompareTo(expectedInt32);
            return true;
        }

        if (actual is Guid actualGuid)
        {
            if (!TryConvertGuid(expected, out var expectedGuid))
            {
                return false;
            }

            comparison = actualGuid.CompareTo(expectedGuid);
            return true;
        }

        if (actual is bool actualBoolean)
        {
            if (!TryConvertBoolean(expected, out var expectedBoolean))
            {
                return false;
            }

            comparison = actualBoolean.CompareTo(expectedBoolean);
            return true;
        }

        if (actual is IComparable comparable && expected.GetType() == actual.GetType())
        {
            comparison = comparable.CompareTo(expected);
            return true;
        }

        return false;
    }

    private static bool TryConvertBoolean(object expected, out bool value)
    {
        switch (expected)
        {
            case bool expectedBoolean:
                value = expectedBoolean;
                return true;
            case string expectedBooleanString when bool.TryParse(expectedBooleanString, out var parsedBoolean):
                value = parsedBoolean;
                return true;
            default:
                value = default;
                return false;
        }
    }

    private static bool TryConvertGuid(object expected, out Guid value)
    {
        switch (expected)
        {
            case Guid expectedGuid:
                value = expectedGuid;
                return true;
            case string expectedGuidString when Guid.TryParse(expectedGuidString, out var parsedGuid):
                value = parsedGuid;
                return true;
            default:
                value = default;
                return false;
        }
    }

    private static bool TryConvertInt32(object expected, out int value)
    {
        switch (expected)
        {
            case int expectedInt32:
                value = expectedInt32;
                return true;
            case long expectedInt64 when expectedInt64 is >= int.MinValue and <= int.MaxValue:
                value = (int)expectedInt64;
                return true;
            case short expectedInt16:
                value = expectedInt16;
                return true;
            case byte expectedByte:
                value = expectedByte;
                return true;
            case string expectedInt32String when int.TryParse(
                expectedInt32String,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var parsedInt32):
                value = parsedInt32;
                return true;
            default:
                value = default;
                return false;
        }
    }
}
