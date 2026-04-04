namespace Dataverse.Emulator.Protocols.Xrm;

internal static class DataverseXrmConstants
{
    public const string ServiceNamespace = "http://schemas.microsoft.com/xrm/2011/Contracts/Services";
    public const string OrganizationRootPath = "/org";
    public const string OrganizationServicePath = "/org/XRMServices/2011/Organization.svc";
    public const string MetadataRootFolder = "XrmMetadata";
    public const string MetadataProfileFolder = "Basic";
    public const string OrganizationFriendlyName = "Dataverse Emulator";
    public const string OrganizationUniqueName = "dataverse-emulator";
    public const string OrganizationVersion = "9.2.0.0";

    public static readonly Guid OrganizationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DefaultBusinessUnitId = Guid.Parse("33333333-3333-3333-3333-333333333333");
}
