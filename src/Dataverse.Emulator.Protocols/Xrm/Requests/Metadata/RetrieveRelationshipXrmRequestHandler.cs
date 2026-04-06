using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Metadata;

internal sealed class RetrieveRelationshipXrmRequestHandler(
    DataverseXrmMetadataOperations metadataOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveRelationship";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveRelationshipRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveRelationshipRequest")]);
        }

        var response = new RetrieveRelationshipResponse();
        response.Results["RelationshipMetadata"] = Invoke(ct => metadataOperations.RetrieveRelationshipAsync(typedRequest, ct));
        return response;
    }
}
