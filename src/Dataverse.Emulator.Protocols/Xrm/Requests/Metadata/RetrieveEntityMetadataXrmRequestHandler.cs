using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Metadata;

internal sealed class RetrieveEntityMetadataXrmRequestHandler(
    DataverseXrmMetadataOperations metadataOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveEntity";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveEntityRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveEntityRequest")]);
        }

        var response = new RetrieveEntityResponse();
        response.Results["EntityMetadata"] = Invoke(ct => metadataOperations.RetrieveEntityAsync(typedRequest, ct));
        return response;
    }
}
