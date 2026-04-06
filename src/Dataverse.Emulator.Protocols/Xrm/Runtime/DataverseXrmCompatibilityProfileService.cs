using ErrorOr;

namespace Dataverse.Emulator.Protocols.Xrm.Runtime;

public sealed class DataverseXrmCompatibilityProfileService(
    DataverseXrmCompatibilitySettings compatibilitySettings)
{
    public DataverseXrmOrganizationProfile GetOrganizationProfile()
    {
        var provisionedLanguages = compatibilitySettings.ProvisionedLanguages.ToArray();
        var installedLanguagePacks = compatibilitySettings.InstalledLanguagePacks.ToArray();
        var availableLanguages = provisionedLanguages
            .Concat(installedLanguagePacks)
            .Distinct()
            .OrderBy(language => language)
            .ToArray();

        return new DataverseXrmOrganizationProfile(
            compatibilitySettings.OrganizationId,
            compatibilitySettings.OrganizationFriendlyName,
            compatibilitySettings.OrganizationUniqueName,
            compatibilitySettings.OrganizationVersion,
            compatibilitySettings.DefaultUserId,
            compatibilitySettings.DefaultBusinessUnitId,
            provisionedLanguages,
            installedLanguagePacks,
            availableLanguages,
            [],
            compatibilitySettings.OrganizationTypeName,
            compatibilitySettings.SolutionUniqueNames.ToArray());
    }

    public ErrorOr<string> GetInstalledLanguagePackVersion(int language)
        => ResolveLanguagePackVersion(
            language,
            GetOrganizationProfile().InstalledLanguagePacks,
            "installed");

    public ErrorOr<string> GetProvisionedLanguagePackVersion(int language)
        => ResolveLanguagePackVersion(
            language,
            GetOrganizationProfile().ProvisionedLanguages,
            "provisioned");

    private ErrorOr<string> ResolveLanguagePackVersion(
        int language,
        IReadOnlyList<int> supportedLanguages,
        string kind)
    {
        if (!supportedLanguages.Contains(language))
        {
            return Error.Validation(
                "Runtime.LanguagePack.Unsupported",
                $"Language '{language}' is not {kind} in the local Dataverse emulator.");
        }

        return compatibilitySettings.OrganizationVersion;
    }
}
