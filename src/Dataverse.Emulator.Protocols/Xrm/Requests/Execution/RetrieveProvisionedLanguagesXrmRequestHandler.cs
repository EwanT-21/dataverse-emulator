using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveProvisionedLanguagesXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveProvisionedLanguages";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveProvisionedLanguagesRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveProvisionedLanguagesRequest")]);
        }

        var response = new RetrieveProvisionedLanguagesResponse();
        response.Results["RetrieveProvisionedLanguages"] = Invoke(ct => runtimeOperations.GetOrganizationProfileAsync(ct)).ProvisionedLanguages;
        return response;
    }
}
