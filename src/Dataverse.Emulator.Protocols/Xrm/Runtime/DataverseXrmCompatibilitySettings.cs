namespace Dataverse.Emulator.Protocols.Xrm.Runtime;

public sealed record DataverseXrmCompatibilitySettings(
    string OrganizationVersion,
    Guid OrganizationId,
    string OrganizationFriendlyName,
    string OrganizationUniqueName,
    Guid DefaultUserId,
    Guid DefaultBusinessUnitId,
    int[] ProvisionedLanguages,
    int[] InstalledLanguagePacks,
    string OrganizationTypeName,
    string[] SolutionUniqueNames)
{
    public const string DefaultOrganizationVersion = "9.2.0.0";
    public static readonly Guid DefaultOrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultOrganizationUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DefaultOrganizationBusinessUnitId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public const string DefaultOrganizationFriendlyName = "Dataverse Emulator";
    public const string DefaultOrganizationUniqueName = "dataverse-emulator";
    public const string DefaultOrganizationTypeName = "Developer";
    public static readonly int[] DefaultProvisionedLanguages = [1033];
    public static readonly int[] DefaultInstalledLanguagePacks = [1033];
    public static readonly string[] DefaultSolutionUniqueNames = [];
}
