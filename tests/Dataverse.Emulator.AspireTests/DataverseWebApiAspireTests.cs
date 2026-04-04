using System.Diagnostics;
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

    [Fact]
    public async Task Xrm_Wsdl_Is_Exposed_Through_AppHost()
    {
        using var client = fixture.CreateClient();

        var wsdlResponse = await client.GetAsync("/org/XRMServices/2011/Organization.svc?wsdl&sdkversion=9.2");
        var wsdl = await wsdlResponse.ReadRequiredStringAsync();

        Assert.Contains("IOrganizationService", wsdl, StringComparison.Ordinal);
        Assert.Contains("Organization.svc?wsdl=wsdl0", wsdl, StringComparison.Ordinal);
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

public sealed class CrmServiceClientAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task CrmServiceClient_Crud_And_QueryExpression_Flow_Works()
    {
        var result = await fixture.RunCrmHarnessAsync("crud");

        Assert.Equal("Contoso", result.GetProperty("retrievedName").GetString());
        Assert.Equal("A-100", result.GetProperty("retrievedAccountNumber").GetString());
        Assert.Equal("A-200", result.GetProperty("updatedAccountNumber").GetString());
        Assert.Equal(1, result.GetProperty("queryCount").GetInt32());
        Assert.False(result.GetProperty("moreRecords").GetBoolean());
    }

    [Fact]
    public async Task Unsupported_QueryExpression_Features_Surface_As_SdkFaults()
    {
        var result = await fixture.RunCrmHarnessAsync("unsupported-link-query");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("LinkEntity", result.GetProperty("message").GetString(), StringComparison.Ordinal);
    }
}

public sealed class CrossSurfaceAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task CrmServiceClient_Create_Can_Be_Read_Over_WebApi()
    {
        var created = await fixture.RunCrmHarnessAsync("create", "Tailspin", "TS-100");
        var accountId = Guid.Parse(created.GetProperty("id").GetString()!);

        using var client = fixture.CreateClient();
        var response = await client.GetAsync($"/api/data/v9.2/accounts({accountId})?$select=name,accountnumber");
        var payload = await response.ReadRequiredJsonAsync();

        Assert.Equal("Tailspin", payload.GetProperty("name").GetString());
        Assert.Equal("TS-100", payload.GetProperty("accountnumber").GetString());
    }

    [Fact]
    public async Task WebApi_Create_Can_Be_Read_Through_CrmServiceClient()
    {
        using var client = fixture.CreateClient();
        var createResponse = await client.PostAsJsonAsync("/api/data/v9.2/accounts", new
        {
            name = "Wingtip",
            accountnumber = "WT-200"
        });

        Assert.Equal(HttpStatusCode.NoContent, createResponse.StatusCode);

        var entityUri = createResponse.Headers.GetValues("OData-EntityId").Single();
        var accountId = ExtractId(entityUri);
        var retrieved = await fixture.RunCrmHarnessAsync("retrieve", accountId.ToString());
        var attributes = retrieved.GetProperty("attributes");

        Assert.Equal("Wingtip", attributes.GetProperty("name").GetString());
        Assert.Equal("WT-200", attributes.GetProperty("accountnumber").GetString());
    }

    private static Guid ExtractId(string entityUri)
    {
        var start = entityUri.IndexOf('(');
        var end = entityUri.IndexOf(')', start + 1);
        return Guid.Parse(entityUri[(start + 1)..end]);
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

    public string CreateConnectionString()
    {
        using var client = CreateClient();
        var orgUrl = new Uri(client.BaseAddress!, "/org").ToString().TrimEnd('/');
        return $"AuthType=AD;Url={orgUrl};Domain=EMULATOR;Username=local;Password=local";
    }

    public async Task<JsonElement> RunCrmHarnessAsync(string scenario, params string[] args)
    {
        var harnessPath = ResolveHarnessPath();
        Assert.True(File.Exists(harnessPath), $"Could not find CrmServiceClient harness at '{harnessPath}'.");

        var startInfo = new ProcessStartInfo(harnessPath)
        {
            WorkingDirectory = Path.GetDirectoryName(harnessPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(scenario);
        startInfo.ArgumentList.Add(CreateConnectionString());

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start(), $"Failed to start CrmServiceClient harness at '{harnessPath}'.");

        using var cts = new CancellationTokenSource(DefaultTimeout);
        var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

        await process.WaitForExitAsync(cts.Token).WaitAsync(DefaultTimeout, cts.Token);

        var output = await outputTask;
        var error = await errorTask;

        Assert.Equal(0, process.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(output), error);

        return JsonSerializer.Deserialize<JsonElement>(output);
    }

    public async Task DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
            app = null;
        }
    }

    private static string ResolveHarnessPath()
    {
        var configuration = new DirectoryInfo(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."))).Name;

        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Dataverse.Emulator.CrmServiceClientHarness",
                "bin",
                configuration,
                "net48",
                "Dataverse.Emulator.CrmServiceClientHarness.exe"));
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
