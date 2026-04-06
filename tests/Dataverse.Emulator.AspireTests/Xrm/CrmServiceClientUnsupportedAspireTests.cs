using System;

namespace Dataverse.Emulator.AspireTests;

public sealed class CrmServiceClientUnsupportedAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
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

    [Fact]
    public async Task Unsupported_Installed_Language_Pack_Version_Requests_Surface_As_SdkFaults()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("unsupported-installed-language-pack-version");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("not installed", result.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }
}
