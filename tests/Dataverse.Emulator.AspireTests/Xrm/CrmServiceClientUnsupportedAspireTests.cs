using System;

namespace Dataverse.Emulator.AspireTests;

public sealed class CrmServiceClientUnsupportedAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [WindowsOnlyFact]
    public async Task Unsupported_QueryExpression_Features_Surface_As_SdkFaults()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("unsupported-query-expression");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("Distinct", result.GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [WindowsOnlyFact]
    public async Task Unsupported_FetchXml_Features_Surface_As_SdkFaults()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("unsupported-fetchxml-aggregate");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("aggregate", result.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [WindowsOnlyFact]
    public async Task Unsupported_Upsert_AlternateKey_Features_Surface_As_SdkFaults()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("unsupported-upsert-alternate-key");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("alternate keys", result.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [WindowsOnlyFact]
    public async Task Unsupported_Installed_Language_Pack_Version_Requests_Surface_As_SdkFaults()
    {
        await fixture.ResetAsync();
        var result = await fixture.RunCrmHarnessAsync("unsupported-installed-language-pack-version");

        Assert.True(result.GetProperty("faulted").GetBoolean());
        Assert.Contains("not installed", result.GetProperty("message").GetString(), StringComparison.OrdinalIgnoreCase);
    }
}
