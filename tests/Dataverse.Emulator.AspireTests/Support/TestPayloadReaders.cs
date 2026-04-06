using System.Linq;
using System.Text.Json;

namespace Dataverse.Emulator.AspireTests;

internal static class TestPayloadReaders
{
    public static string[] ReadStringArray(this JsonElement payload, string propertyName)
        => payload.GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();

    public static int[] ReadIntArray(this JsonElement payload, string propertyName)
        => payload.GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetInt32())
            .ToArray();
}
