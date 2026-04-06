using System;

namespace Dataverse.Emulator.AspireTests;

public sealed class CrmServiceClientBootstrapAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task CrmServiceClient_Can_Read_Configured_Organization_Version()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("version");

        Assert.Equal("9.2.0.0", result.GetProperty("version").GetString());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Available_Languages()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("available-languages");

        Assert.Equal([1033], result.ReadIntArray("languages"));
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Deprovisioned_Languages()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("deprovisioned-languages");

        Assert.Empty(result.ReadIntArray("languages"));
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Provisioned_Languages_Through_Execute()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("provisioned-languages");

        Assert.Equal([1033], result.ReadIntArray("languages"));
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Installed_Language_Pack_Version()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("installed-language-pack-version");

        Assert.Equal(1033, result.GetProperty("language").GetInt32());
        Assert.Equal("9.2.0.0", result.GetProperty("version").GetString());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Provisioned_Language_Pack_Version()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("provisioned-language-pack-version");

        Assert.Equal(1033, result.GetProperty("language").GetInt32());
        Assert.Equal("9.2.0.0", result.GetProperty("version").GetString());
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_WhoAmI_Details()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("whoami");

        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("userId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("businessUnitId").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(result.GetProperty("organizationId").GetString()));
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Current_Organization_Details()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("current-organization");

        Assert.Equal("Dataverse Emulator", result.GetProperty("friendlyName").GetString());
        Assert.Equal("dataverse-emulator", result.GetProperty("uniqueName").GetString());
        Assert.Equal("9.2.0.0", result.GetProperty("organizationVersion").GetString());
        Assert.Contains("/org/XRMServices/2011/Organization.svc", result.GetProperty("organizationServiceEndpoint").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Installed_Language_Packs()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("installed-language-packs");

        Assert.False(result.GetProperty("faulted").GetBoolean());
        Assert.Equal([1033], result.ReadIntArray("languages"));
    }

    [Fact]
    public async Task CrmServiceClient_Can_Read_Organization_Info()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("organization-info");

        Assert.False(result.GetProperty("faulted").GetBoolean());
        Assert.True(result.GetProperty("solutionsCount").GetInt32() >= 0);
    }
}
