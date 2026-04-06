using System.Net;
using System.Net.Http.Json;

namespace Dataverse.Emulator.AspireTests;

public sealed class CrudFlowAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task Crud_Flow_Works_Over_Http()
    {
        using var client = fixture.CreateClient();

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, "/api/data/v9.2/accounts");
        createRequest.Headers.Add("Prefer", "return=representation");
        createRequest.Content = JsonContent.Create(new
        {
            name = "Contoso",
            accountnumber = "A-100",
            isactive = true
        });

        var createResponse = await client.SendAsync(createRequest);
        var created = await createResponse.ReadJsonAsync();
        var accountId = created.GetProperty("accountid").GetGuid();

        var getResponse = await client.GetAsync($"/api/data/v9.2/accounts({accountId})?$select=name,accountnumber");
        var getPayload = await getResponse.ReadRequiredJsonAsync();

        using var updateRequest = new HttpRequestMessage(new HttpMethod("PATCH"), $"/api/data/v9.2/accounts({accountId})");
        updateRequest.Headers.Add("Prefer", "return=representation");
        updateRequest.Content = JsonContent.Create(new
        {
            accountnumber = "A-200"
        });

        var updateResponse = await client.SendAsync(updateRequest);
        var updated = await updateResponse.ReadJsonAsync();

        var listResponse = await client.GetAsync("/api/data/v9.2/accounts?$select=name,accountnumber&$filter=name eq 'Contoso'&$orderby=name asc&$top=10");
        var listPayload = await listResponse.ReadRequiredJsonAsync();

        var deleteResponse = await client.DeleteAsync($"/api/data/v9.2/accounts({accountId})");
        var afterDeleteResponse = await client.GetAsync($"/api/data/v9.2/accounts({accountId})");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.True(createResponse.Headers.Contains("OData-EntityId"));
        Assert.Equal("Contoso", created.GetProperty("name").GetString());

        Assert.Equal("A-100", getPayload.GetProperty("accountnumber").GetString());
        Assert.Equal("A-200", updated.GetProperty("accountnumber").GetString());
        Assert.Single(listPayload.GetProperty("value").EnumerateArray());

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
    }
}
