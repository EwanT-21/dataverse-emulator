using ErrorOr;
using Dataverse.Emulator.Protocols.Xrm.Runtime;

namespace Dataverse.Emulator.Protocols.Xrm.Operations;

public sealed class DataverseXrmRuntimeOperations(
    DataverseXrmCompatibilityProfileService compatibilityProfileService)
{
    public Task<ErrorOr<DataverseXrmOrganizationProfile>> GetOrganizationProfileAsync(
        CancellationToken cancellationToken)
        => Task.FromResult<ErrorOr<DataverseXrmOrganizationProfile>>(
            compatibilityProfileService.GetOrganizationProfile());

    public Task<ErrorOr<string>> GetInstalledLanguagePackVersionAsync(
        int language,
        CancellationToken cancellationToken)
        => Task.FromResult(compatibilityProfileService.GetInstalledLanguagePackVersion(language));

    public Task<ErrorOr<string>> GetProvisionedLanguagePackVersionAsync(
        int language,
        CancellationToken cancellationToken)
        => Task.FromResult(compatibilityProfileService.GetProvisionedLanguagePackVersion(language));
}
