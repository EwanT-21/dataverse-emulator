using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveInstalledLanguagePackVersionXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveInstalledLanguagePackVersion";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveInstalledLanguagePackVersionRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveInstalledLanguagePackVersionRequest")]);
        }

        var response = new RetrieveInstalledLanguagePackVersionResponse();
        response.Results["Version"] = Invoke(ct => runtimeOperations.GetInstalledLanguagePackVersionAsync(typedRequest.Language, ct));
        return response;
    }
}
