using Dataverse.Emulator.Application.Runtime;
using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class RetrieveVersionXrmRequestHandler(
    DataverseEmulatorRuntimeSettings runtimeSettings)
    : IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveVersion";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveVersionRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveVersionRequest")]);
        }

        var response = new RetrieveVersionResponse();
        response.Results["Version"] = runtimeSettings.OrganizationVersion;
        return response;
    }
}
