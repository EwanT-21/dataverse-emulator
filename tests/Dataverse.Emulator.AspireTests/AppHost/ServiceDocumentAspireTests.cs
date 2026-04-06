using System;
using System.Linq;

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
