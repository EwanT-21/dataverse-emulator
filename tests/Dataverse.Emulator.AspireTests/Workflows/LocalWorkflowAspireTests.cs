using System;
using System.Net;
using System.Net.Http.Json;
using System.Linq;

namespace Dataverse.Emulator.AspireTests;

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
        var accountId = ODataEntityReferenceParser.ExtractId(accountEntityUri);

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
}
