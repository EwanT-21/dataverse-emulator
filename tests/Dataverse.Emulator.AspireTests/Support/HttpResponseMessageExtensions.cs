using System.Net.Http;
using System.Text.Json;

namespace Dataverse.Emulator.AspireTests;

internal static class HttpResponseMessageExtensions
{
    public static async Task<JsonElement> ReadRequiredJsonAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<JsonElement>(content);
    }

    public static async Task<string> ReadRequiredStringAsync(this HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);
        return content;
    }
}
