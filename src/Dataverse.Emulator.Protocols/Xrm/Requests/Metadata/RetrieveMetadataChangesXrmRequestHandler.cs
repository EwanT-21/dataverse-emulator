using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Metadata;

internal sealed class RetrieveMetadataChangesXrmRequestHandler(
    DataverseXrmMetadataOperations metadataOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveMetadataChanges";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveMetadataChangesRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveMetadataChangesRequest")]);
        }

        var result = Invoke(ct => metadataOperations.RetrieveMetadataChangesAsync(typedRequest, ct));

        var response = new RetrieveMetadataChangesResponse();
        response.Results["EntityMetadata"] = result.EntityMetadata;
        response.Results["ServerVersionStamp"] = result.ServerVersionStamp;
        return response;
    }
}
