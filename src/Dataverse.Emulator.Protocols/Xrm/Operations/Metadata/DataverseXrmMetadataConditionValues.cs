using System.Collections;

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
}
