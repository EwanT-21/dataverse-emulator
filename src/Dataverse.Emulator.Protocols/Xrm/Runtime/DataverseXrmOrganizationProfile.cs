namespace Dataverse.Emulator.Protocols.Xrm.Runtime;

public sealed record DataverseXrmOrganizationProfile(
    Guid OrganizationId,
    string OrganizationFriendlyName,
    string OrganizationUniqueName,
    string OrganizationVersion,
    Guid DefaultUserId,
    Guid DefaultBusinessUnitId,
    int[] ProvisionedLanguages,
    int[] InstalledLanguagePacks,
    int[] AvailableLanguages,
    int[] DeprovisionedLanguages,
    string OrganizationTypeName,
    string[] SolutionUniqueNames);
