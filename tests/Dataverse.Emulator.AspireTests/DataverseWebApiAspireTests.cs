using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Dataverse.Emulator.AppHost;
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
        Assert.Contains("contacts", serviceDocument.GetProperty("value").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Contains("EntitySet Name=\"accounts\"", metadataDocument, StringComparison.Ordinal);
        Assert.Contains("EntityType Name=\"account\"", metadataDocument, StringComparison.Ordinal);
        Assert.Contains("EntitySet Name=\"contacts\"", metadataDocument, StringComparison.Ordinal);
        Assert.Contains("EntityType Name=\"contact\"", metadataDocument, StringComparison.Ordinal);
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

public sealed class AppHostPackagingAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task AppHost_Exposes_Emulator_ConnectionString()
    {
        var connectionString = await fixture.GetConnectionStringAsync();

        Assert.Contains("AuthType=AD;", connectionString, StringComparison.Ordinal);
        Assert.Contains("/org;", connectionString, StringComparison.Ordinal);
        Assert.Contains("Domain=EMULATOR;", connectionString, StringComparison.Ordinal);
        Assert.Contains("Username=local;", connectionString, StringComparison.Ordinal);
        Assert.Contains("Password=local", connectionString, StringComparison.Ordinal);
    }
}

public sealed class AppHostModelAspireTests
{
    [Fact]
    public async Task AppHost_Helper_Can_Map_Emulator_ConnectionString_Into_A_Custom_Environment_Variable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var service = builder.AddProject<Dataverse_Emulator_Host>("dataverse-emulator-model");
        var connectionString = builder.AddConnectionString("dataverse-model", expression =>
            expression.AppendLiteral("AuthType=AD;Url=http://localhost:5100/org;Domain=EMULATOR;Username=local;Password=local"));
        var emulator = new DataverseEmulatorAppHostResource(service, connectionString);
        var consumer = builder.AddExecutable("legacy-consumer", "cmd.exe", Environment.SystemDirectory)
            .WithDataverseConnectionString(emulator, "CRM_CONNECTION_STRING");

        #pragma warning disable CS0618
        var environment = await consumer.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
        #pragma warning restore CS0618
        var environmentKeys = environment.Select(entry => entry.Key).ToArray();

        Assert.Contains("CRM_CONNECTION_STRING", environmentKeys);
        Assert.DoesNotContain("ConnectionStrings__dataverse", environmentKeys);

        var resolvedConnectionString = environment.Single(entry => entry.Key == "CRM_CONNECTION_STRING").Value;
        Assert.Equal("{dataverse-model.connectionString}", resolvedConnectionString);
    }

    [Fact]
    public async Task AppHost_Helper_Can_Set_The_Xrm_Trace_Limit()
    {
        var builder = DistributedApplication.CreateBuilder();
        var emulator = builder.AddDataverseEmulator("dataverse-trace-model")
            .WithXrmTraceLimit(25);

        #pragma warning disable CS0618
        var environment = await emulator.Service.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
        #pragma warning restore CS0618

        var traceLimit = environment.Single(entry => entry.Key == "DATAVERSE_EMULATOR_XRM_TRACE_LIMIT").Value;
        Assert.Equal("25", traceLimit);
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
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("crud");

        Assert.Equal("Contoso", result.GetProperty("retrievedName").GetString());
        Assert.Equal("A-100", result.GetProperty("retrievedAccountNumber").GetString());
        Assert.Equal("A-200", result.GetProperty("updatedAccountNumber").GetString());
        Assert.Equal(1, result.GetProperty("queryCount").GetInt32());
        Assert.False(result.GetProperty("moreRecords").GetBoolean());
    }

    [Fact]
    public async Task CrmServiceClient_QueryExpression_Paging_RoundTrips()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("paged-query");

        Assert.Equal("Alpha", result.GetProperty("firstPageName").GetString());
        Assert.Equal(1, result.GetProperty("firstPageCount").GetInt32());
        Assert.True(result.GetProperty("firstMoreRecords").GetBoolean());
        Assert.True(result.GetProperty("firstPagingCookiePresent").GetBoolean());
        Assert.Equal("Bravo", result.GetProperty("secondPageName").GetString());
        Assert.Equal(1, result.GetProperty("secondPageCount").GetInt32());
    }

    [Fact]
    public async Task CrmServiceClient_Advanced_QueryExpression_Filters_RoundTrip()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("advanced-query");

        Assert.Equal(2, result.GetProperty("groupedCount").GetInt32());
        Assert.Equal(["Alpha", "Charlie"], ReadStringArray(result, "groupedNames"));
        Assert.Equal(2, result.GetProperty("inCount").GetInt32());
        Assert.Equal(["Alpha", "Charlie"], ReadStringArray(result, "inNames"));
        Assert.Equal(2, result.GetProperty("likeCount").GetInt32());
        Assert.Equal(["Alpha", "Alpine"], ReadStringArray(result, "likeNames"));
        Assert.Equal(2, result.GetProperty("rangeCount").GetInt32());
        Assert.Equal(["Alpine", "Bravo"], ReadStringArray(result, "rangeNames"));
    }

    [Fact]
    public async Task CrmServiceClient_Linked_QueryExpression_RoundTrips_Across_Tables()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("linked-query");

        Assert.Equal(2, result.GetProperty("count").GetInt32());
        Assert.Equal(["Alice Alpha", "Aria Alpha"], ReadStringArray(result, "names"));
        Assert.Equal(["Alpha Account", "Alpha Account"], ReadStringArray(result, "accountNames"));
        Assert.Equal(["A-100", "A-100"], ReadStringArray(result, "accountNumbers"));
    }

    [Fact]
    public async Task CrmServiceClient_FetchXml_RetrieveMultiple_RoundTrips()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("fetchxml");

        Assert.Equal(2, result.GetProperty("firstPageCount").GetInt32());
        Assert.Equal(["Alpha", "Alpine"], ReadStringArray(result, "firstPageNames"));
        Assert.True(result.GetProperty("firstMoreRecords").GetBoolean());
        Assert.True(result.GetProperty("firstPagingCookiePresent").GetBoolean());
        Assert.Equal(1, result.GetProperty("secondPageCount").GetInt32());
        Assert.Equal(["Charlie"], ReadStringArray(result, "secondPageNames"));
        Assert.False(result.GetProperty("secondMoreRecords").GetBoolean());
    }

    [Fact]
    public async Task CrmServiceClient_ExecuteMultiple_Composes_Existing_Request_Slices()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("execute-multiple");

        Assert.False(result.GetProperty("isFaulted").GetBoolean());
        Assert.Equal(3, result.GetProperty("responseCount").GetInt32());
        Assert.Equal([0, 1, 2], ReadIntArray(result, "successIndices"));
        Assert.Equal(2, result.GetProperty("createdCount").GetInt32());
        Assert.Equal(["Alpha", "Bravo"], ReadStringArray(result, "createdNames"));
    }

    [Fact]
    public async Task CrmServiceClient_UpsertRequest_Composes_Create_And_Update_Flow()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("upsert");

        Assert.True(result.GetProperty("createRecordCreated").GetBoolean());
        Assert.False(result.GetProperty("updateRecordCreated").GetBoolean());
        Assert.Equal(result.GetProperty("createdId").GetString(), result.GetProperty("updateTargetId").GetString());
        Assert.Equal("Upserted", result.GetProperty("retrievedName").GetString());
        Assert.Equal("UP-200", result.GetProperty("retrievedAccountNumber").GetString());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Configured_Organization_Version()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("version");

        Assert.Equal("9.2.0.0", result.GetProperty("version").GetString());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Seeded_Metadata()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("metadata");

        Assert.Equal("account", result.GetProperty("entityLogicalName").GetString());
        Assert.Equal("accounts", result.GetProperty("entitySetName").GetString());
        Assert.Equal("accountid", result.GetProperty("primaryIdAttribute").GetString());
        Assert.Equal("name", result.GetProperty("primaryNameAttribute").GetString());
        Assert.Equal(5, result.GetProperty("attributeCount").GetInt32());
        Assert.Equal(1, result.GetProperty("objectTypeCode").GetInt32());
        Assert.Equal("name", result.GetProperty("attributeLogicalName").GetString());
        Assert.Equal("String", result.GetProperty("attributeType").GetString());
        Assert.Equal("ApplicationRequired", result.GetProperty("attributeRequiredLevel").GetString());
        Assert.Equal(2, result.GetProperty("allEntitiesCount").GetInt32());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Associate_And_Disassociate_Seeded_Lookup_Relationship()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("associate");

        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("accountId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("contactId").GetString()));
        Assert.Equal(result.GetProperty("accountId").GetString(), result.GetProperty("associatedParentId").GetString());
        Assert.Equal("account", result.GetProperty("associatedParentLogicalName").GetString());
        Assert.False(result.GetProperty("disassociatedParentPresent").GetBoolean());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Seeded_Relationship_Metadata()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("relationship-metadata");

        Assert.Equal("contact_customer_accounts", result.GetProperty("schemaName").GetString());
        Assert.Equal("account", result.GetProperty("referencedEntity").GetString());
        Assert.Equal("contact", result.GetProperty("referencingEntity").GetString());
        Assert.Equal("parentcustomerid", result.GetProperty("referencingAttribute").GetString());
        Assert.Equal(1, result.GetProperty("accountOneToManyCount").GetInt32());
        Assert.Equal(["contact_customer_accounts"], ReadStringArray(result, "accountOneToManyNames"));
        Assert.Equal(1, result.GetProperty("contactManyToOneCount").GetInt32());
        Assert.Equal(["contact_customer_accounts"], ReadStringArray(result, "contactManyToOneNames"));
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Provisioned_Languages_Through_Execute()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("provisioned-languages");

        Assert.Equal([1033], ReadIntArray(result, "languages"));
    }

    [Fact]
    public async Task Unsupported_QueryExpression_Features_Surface_As_SdkFaults()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("unsupported-link-query");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("Join operator", result.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unsupported_FetchXml_Features_Surface_As_SdkFaults()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("unsupported-fetchxml-link-entity");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("link-entity", result.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unsupported_Upsert_AlternateKey_Features_Surface_As_SdkFaults()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("unsupported-upsert-alternate-key");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("alternate keys", result.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    private static string[] ReadStringArray(JsonElement payload, string propertyName)
        => payload.GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();

    private static int[] ReadIntArray(JsonElement payload, string propertyName)
        => payload.GetProperty(propertyName)
            .EnumerateArray()
            .Select(item => item.GetInt32())
            .ToArray();
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

public sealed class LocalWorkflowAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task Reset_Restores_Default_Seed_State()
    {
        await fixture.ResetAsync();

        var created = await fixture.RunCrmHarnessAsync("create", "Resettable", "RS-100");
        var accountId = Guid.Parse(created.GetProperty("id").GetString()!);

        using var client = fixture.CreateClient();
        var beforeResetResponse = await client.GetAsync($"/api/data/v9.2/accounts({accountId})?$select=name");
        Assert.Equal(HttpStatusCode.OK, beforeResetResponse.StatusCode);

        await fixture.ResetAsync();

        var afterResetResponse = await client.GetAsync($"/api/data/v9.2/accounts({accountId})?$select=name");
        var metadata = await fixture.RunCrmHarnessAsync("metadata");

        Assert.Equal(HttpStatusCode.NotFound, afterResetResponse.StatusCode);
        Assert.Equal(2, metadata.GetProperty("allEntitiesCount").GetInt32());
        Assert.Equal("account", metadata.GetProperty("entityLogicalName").GetString());
    }

    [Fact]
    public async Task Reset_Can_Target_Empty_Seed_Scenario()
    {
        await fixture.ResetAsync();

        using var client = fixture.CreateClient();
        var response = await client.PostAsync("/_emulator/v1/reset?scenario=empty", content: null);
        var payload = await response.ReadRequiredJsonAsync();
        var serviceDocumentResponse = await client.GetAsync("/api/data/v9.2");
        var serviceDocument = await serviceDocumentResponse.ReadRequiredJsonAsync();

        Assert.Equal("reset", payload.GetProperty("status").GetString());
        Assert.Equal("scenario", payload.GetProperty("baselineKind").GetString());
        Assert.Equal("empty", payload.GetProperty("baselineName").GetString());
        Assert.Empty(serviceDocument.GetProperty("value").EnumerateArray());

        await fixture.ResetAsync();
    }

    [Fact]
    public async Task Snapshot_Export_And_Import_RoundTrips_Runtime_State()
    {
        await fixture.ResetAsync();

        using var client = fixture.CreateClient();
        var accountCreateResponse = await client.PostAsJsonAsync("/api/data/v9.2/accounts", new
        {
            name = "Snapshot Account",
            accountnumber = "SN-100"
        });

        Assert.Equal(HttpStatusCode.NoContent, accountCreateResponse.StatusCode);

        var accountEntityUri = accountCreateResponse.Headers.GetValues("OData-EntityId").Single();
        var accountId = ExtractId(accountEntityUri);

        var contactCreateResponse = await client.PostAsJsonAsync("/api/data/v9.2/contacts", new
        {
            fullname = "Snapshot Contact",
            emailaddress1 = "snapshot@example.com",
            parentcustomerid = accountId
        });

        Assert.Equal(HttpStatusCode.NoContent, contactCreateResponse.StatusCode);

        var snapshot = await fixture.ExportSnapshotAsync();

        Assert.Equal("1.0", snapshot.GetProperty("schemaVersion").GetString());
        Assert.Equal(2, snapshot.GetProperty("tables").GetArrayLength());
        Assert.Equal(2, snapshot.GetProperty("records").GetArrayLength());

        await fixture.ResetAsync();

        var afterResetResponse = await client.GetAsync($"/api/data/v9.2/accounts({accountId})?$select=name");
        Assert.Equal(HttpStatusCode.NotFound, afterResetResponse.StatusCode);

        var importResult = await fixture.ImportSnapshotAsync(snapshot);

        Assert.Equal("imported", importResult.GetProperty("status").GetString());
        Assert.Equal("1.0", importResult.GetProperty("schemaVersion").GetString());
        Assert.Equal(2, importResult.GetProperty("tableCount").GetInt32());
        Assert.Equal(2, importResult.GetProperty("recordCount").GetInt32());

        var restoredAccountResponse = await client.GetAsync($"/api/data/v9.2/accounts({accountId})?$select=name,accountnumber");
        var restoredAccount = await restoredAccountResponse.ReadRequiredJsonAsync();
        var restoredContactsResponse = await client.GetAsync("/api/data/v9.2/contacts?$select=fullname,emailaddress1,parentcustomerid&$filter=fullname eq 'Snapshot Contact'");
        var restoredContacts = await restoredContactsResponse.ReadRequiredJsonAsync();

        Assert.Equal("Snapshot Account", restoredAccount.GetProperty("name").GetString());
        Assert.Equal("SN-100", restoredAccount.GetProperty("accountnumber").GetString());
        Assert.Single(restoredContacts.GetProperty("value").EnumerateArray());
        Assert.Equal(accountId, restoredContacts.GetProperty("value")[0].GetProperty("parentcustomerid").GetGuid());
    }

    [Fact]
    public async Task Xrm_Traces_Capture_Supported_And_Unsupported_Request_Flows()
    {
        await fixture.ResetAsync();
        await fixture.ClearXrmTracesAsync();

        await fixture.RunCrmHarnessAsync("provisioned-languages");
        var supportedTraces = await fixture.GetXrmTracesAsync();

        Assert.Contains(
            supportedTraces.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "RetrieveProvisionedLanguages"
                && item.GetProperty("source").GetString() == "ExecuteRequest"
                && item.GetProperty("succeeded").GetBoolean());

        await fixture.ClearXrmTracesAsync();
        var unsupported = await fixture.RunCrmHarnessAsync("unsupported-request");
        var unsupportedTraces = await fixture.GetXrmTracesAsync();

        Assert.True(unsupported.GetProperty("faulted").GetBoolean());
        Assert.Contains(
            unsupportedTraces.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "RetrieveUserLicenseInfo"
                && !item.GetProperty("succeeded").GetBoolean());
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
    private string? connectionString;

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
            connectionString = await app.GetConnectionStringAsync("dataverse", cts.Token).AsTask()
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

    public Task<string> GetConnectionStringAsync()
        => Task.FromResult(connectionString ?? throw new InvalidOperationException("The emulator connection string has not been initialized."));

    public async Task ResetAsync()
        => await ResetAsync(scenario: null);

    public async Task ResetAsync(string? scenario)
    {
        using var client = CreateClient();
        var path = string.IsNullOrWhiteSpace(scenario)
            ? "/_emulator/v1/reset"
            : $"/_emulator/v1/reset?scenario={Uri.EscapeDataString(scenario)}";

        var response = await client.PostAsync(path, content: null);
        var payload = await response.ReadRequiredJsonAsync();

        Assert.Equal("reset", payload.GetProperty("status").GetString());
        Assert.Equal("scenario", payload.GetProperty("baselineKind").GetString());
        Assert.Equal(
            string.IsNullOrWhiteSpace(scenario) ? "default-seed" : scenario,
            payload.GetProperty("baselineName").GetString());
    }

    public async Task<JsonElement> ExportSnapshotAsync()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/_emulator/v1/snapshot");
        return await response.ReadRequiredJsonAsync();
    }

    public async Task<JsonElement> ImportSnapshotAsync(JsonElement snapshot)
    {
        using var client = CreateClient();
        using var content = new StringContent(snapshot.GetRawText(), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/_emulator/v1/snapshot", content);
        return await response.ReadRequiredJsonAsync();
    }

    public async Task<JsonElement> GetXrmTracesAsync(int? limit = null)
    {
        using var client = CreateClient();
        var path = limit.HasValue
            ? $"/_emulator/v1/traces/xrm?limit={limit.Value}"
            : "/_emulator/v1/traces/xrm";
        var response = await client.GetAsync(path);
        return await response.ReadRequiredJsonAsync();
    }

    public async Task ClearXrmTracesAsync()
    {
        using var client = CreateClient();
        var response = await client.DeleteAsync("/_emulator/v1/traces/xrm");
        var payload = await response.ReadRequiredJsonAsync();

        Assert.Equal("cleared", payload.GetProperty("status").GetString());
        Assert.Equal("xrm", payload.GetProperty("traceKind").GetString());
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
        startInfo.ArgumentList.Add(await GetConnectionStringAsync());

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
