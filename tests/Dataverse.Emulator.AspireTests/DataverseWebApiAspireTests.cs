using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

namespace Dataverse.Emulator.AspireTests;

public sealed class ServiceDocumentAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task ServiceDocument_And_Metadata_AreExposed_Through_AppHost()
    {
        using var client = fixture.CreateClient();

        var serviceDocumentResponse = await client.GetAsync("/api/data/v9.2");
        var serviceDocument = await serviceDocumentResponse.ReadRequiredJsonAsync();

        var metadataResponse = await client.GetAsync("/api/data/v9.2/$metadata");
        var metadataDocument = await metadataResponse.ReadRequiredStringAsync();

        Assert.Contains("accounts", serviceDocument.GetProperty("value").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Contains("EntitySet Name=\"accounts\"", metadataDocument, StringComparison.Ordinal);
        Assert.Contains("EntityType Name=\"account\"", metadataDocument, StringComparison.Ordinal);
    }
}

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

public sealed class DataverseEmulatorFixture : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private DistributedApplication? app;

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(DefaultTimeout);

        try
        {
            var builder = await DistributedApplicationTestingBuilder.CreateAsync<Dataverse_Emulator_AppHost>(cts.Token);
            builder.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
                logging.AddSimpleConsole();
            });

            app = await builder.BuildAsync(cts.Token).WaitAsync(DefaultTimeout, cts.Token);
            await app.StartAsync(cts.Token).WaitAsync(DefaultTimeout, cts.Token);
            await app.ResourceNotifications
                .WaitForResourceAsync("dataverse-emulator", KnownResourceStates.Running, cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("dataverse-emulator", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);
        }
        catch
        {
            if (app is not null)
            {
                await app.DisposeAsync();
                app = null;
            }

            throw;
        }
    }

    public HttpClient CreateClient()
    {
        Assert.NotNull(app);
        return app.CreateHttpClient("dataverse-emulator", "http");
    }

    public async Task DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
            app = null;
        }
    }
}

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
