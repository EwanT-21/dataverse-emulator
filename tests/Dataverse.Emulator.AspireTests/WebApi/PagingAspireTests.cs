using System.Net;
using System.Net.Http.Json;

namespace Dataverse.Emulator.AspireTests;

public sealed class PagingAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task Paging_NextLink_RoundTrips_Through_Http()
    {
        using var client = fixture.CreateClient();

        foreach (var name in new[] { "Alpha", "Bravo", "Charlie" })
        {
            var response = await client.PostAsJsonAsync("/api/data/v9.2/accounts", new
            {
                name,
                accountnumber = name[..1]
            });

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        using var firstPageRequest = new HttpRequestMessage(HttpMethod.Get, "/api/data/v9.2/accounts?$select=name&$orderby=name asc");
        firstPageRequest.Headers.Add("Prefer", "odata.maxpagesize=1");

        var firstPageResponse = await client.SendAsync(firstPageRequest);
        var firstPage = await firstPageResponse.ReadRequiredJsonAsync();
        var nextLink = firstPage.GetProperty("@odata.nextLink").GetString();

        var secondPageResponse = await client.GetAsync(nextLink);
        var secondPage = await secondPageResponse.ReadRequiredJsonAsync();

        Assert.Equal("Alpha", firstPage.GetProperty("value")[0].GetProperty("name").GetString());
        Assert.False(string.IsNullOrWhiteSpace(nextLink));
        Assert.Equal("Bravo", secondPage.GetProperty("value")[0].GetProperty("name").GetString());
    }
}
