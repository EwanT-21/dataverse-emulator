using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveProvisionedLanguagesXrmRequestHandler
    : IXrmOrganizationRequestHandler
{
    private static readonly int[] ProvisionedLanguages = [1033];

    public string RequestName => "RetrieveProvisionedLanguages";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveProvisionedLanguagesRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveProvisionedLanguagesRequest")]);
        }

        var response = new RetrieveProvisionedLanguagesResponse();
        response.Results["RetrieveProvisionedLanguages"] = ProvisionedLanguages;
        return response;
    }
}
