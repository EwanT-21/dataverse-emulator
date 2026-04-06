using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveAvailableLanguagesXrmRequestHandler(
    DataverseXrmRuntimeOperations runtimeOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveAvailableLanguages";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveAvailableLanguagesRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveAvailableLanguagesRequest")]);
        }

        var response = new RetrieveAvailableLanguagesResponse();
        response.Results["LocaleIds"] = Invoke(ct => runtimeOperations.GetOrganizationProfileAsync(ct)).AvailableLanguages;
        return response;
    }
}
