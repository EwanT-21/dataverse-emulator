using System;
using System.Net;
using System.Net.Http.Json;
using System.Linq;

namespace Dataverse.Emulator.AspireTests;

public sealed class CrossSurfaceAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [WindowsOnlyFact]
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

    [WindowsOnlyFact]
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
        var accountId = ODataEntityReferenceParser.ExtractId(entityUri);
        var retrieved = await fixture.RunCrmHarnessAsync("retrieve", accountId.ToString());
        var attributes = retrieved.GetProperty("attributes");

        Assert.Equal("Wingtip", attributes.GetProperty("name").GetString());
        Assert.Equal("WT-200", attributes.GetProperty("accountnumber").GetString());
    }
}
