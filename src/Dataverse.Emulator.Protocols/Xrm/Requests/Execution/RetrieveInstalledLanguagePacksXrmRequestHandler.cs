using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveInstalledLanguagePacksXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveInstalledLanguagePacks";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveInstalledLanguagePacksRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveInstalledLanguagePacksRequest")]);
        }

        var response = new RetrieveInstalledLanguagePacksResponse();
        response.Results["RetrieveInstalledLanguagePacks"] = Invoke(ct => runtimeOperations.GetOrganizationProfileAsync(ct)).InstalledLanguagePacks;
        return response;
    }
}
