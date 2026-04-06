using Dataverse.Emulator.Protocols.Xrm.Runtime;

namespace Dataverse.Emulator.IntegrationTests;

public class XrmCompatibilityProfileIntegrationTests
{
    [Fact]
    public void Xrm_Compatibility_Profile_Service_Returns_Protocol_Owned_Organization_Profile()
    {
        var service = new DataverseXrmCompatibilityProfileService(CreateDefaultSettings());

        var result = service.GetOrganizationProfile();

        Assert.Equal("Dataverse Emulator", result.OrganizationFriendlyName);
        Assert.Equal("dataverse-emulator", result.OrganizationUniqueName);
        Assert.Equal("9.2.0.0", result.OrganizationVersion);
        Assert.Equal([1033], result.ProvisionedLanguages);
        Assert.Equal([1033], result.InstalledLanguagePacks);
        Assert.Equal([1033], result.AvailableLanguages);
        Assert.Empty(result.DeprovisionedLanguages);
    }

    [Fact]
    public void Language_Pack_Version_Queries_Use_Xrm_Compatibility_Profile()
    {
        var service = new DataverseXrmCompatibilityProfileService(CreateDefaultSettings());

        var installedResult = service.GetInstalledLanguagePackVersion(1033);
        var provisionedResult = service.GetProvisionedLanguagePackVersion(1033);
        var unsupportedResult = service.GetInstalledLanguagePackVersion(1041);

        Assert.False(installedResult.IsError);
        Assert.False(provisionedResult.IsError);
        Assert.Equal("9.2.0.0", installedResult.Value);
        Assert.Equal("9.2.0.0", provisionedResult.Value);
        Assert.True(unsupportedResult.IsError);
        Assert.Contains(unsupportedResult.Errors, error => error.Code == "Runtime.LanguagePack.Unsupported");
    }

    private static DataverseXrmCompatibilitySettings CreateDefaultSettings()
        => new(
            OrganizationVersion: DataverseXrmCompatibilitySettings.DefaultOrganizationVersion,
            OrganizationId: DataverseXrmCompatibilitySettings.DefaultOrganizationId,
            OrganizationFriendlyName: DataverseXrmCompatibilitySettings.DefaultOrganizationFriendlyName,
            OrganizationUniqueName: DataverseXrmCompatibilitySettings.DefaultOrganizationUniqueName,
            DefaultUserId: DataverseXrmCompatibilitySettings.DefaultOrganizationUserId,
            DefaultBusinessUnitId: DataverseXrmCompatibilitySettings.DefaultOrganizationBusinessUnitId,
            ProvisionedLanguages: DataverseXrmCompatibilitySettings.DefaultProvisionedLanguages.ToArray(),
            InstalledLanguagePacks: DataverseXrmCompatibilitySettings.DefaultInstalledLanguagePacks.ToArray(),
            OrganizationTypeName: DataverseXrmCompatibilitySettings.DefaultOrganizationTypeName,
            SolutionUniqueNames: DataverseXrmCompatibilitySettings.DefaultSolutionUniqueNames.ToArray());
}
