using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveDeprovisionedLanguagesXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveDeprovisionedLanguages";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveDeprovisionedLanguagesRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveDeprovisionedLanguagesRequest")]);
        }

        var response = new RetrieveDeprovisionedLanguagesResponse();
        response.Results["RetrieveDeprovisionedLanguages"] = Invoke(ct => runtimeOperations.GetOrganizationProfileAsync(ct)).DeprovisionedLanguages;
        return response;
    }
}
