using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveProvisionedLanguagePackVersionXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveProvisionedLanguagePackVersion";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveProvisionedLanguagePackVersionRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveProvisionedLanguagePackVersionRequest")]);
        }

        var response = new RetrieveProvisionedLanguagePackVersionResponse();
        response.Results["Version"] = Invoke(ct => runtimeOperations.GetProvisionedLanguagePackVersionAsync(typedRequest.Language, ct));
        return response;
    }
}
