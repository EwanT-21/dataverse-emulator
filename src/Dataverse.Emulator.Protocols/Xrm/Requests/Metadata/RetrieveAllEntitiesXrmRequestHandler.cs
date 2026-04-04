using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Metadata;

internal sealed class RetrieveAllEntitiesXrmRequestHandler(
    DataverseXrmMetadataOperations metadataOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "RetrieveAllEntities";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not RetrieveAllEntitiesRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("RetrieveAllEntitiesRequest")]);
        }

        var response = new RetrieveAllEntitiesResponse();
        response.Results["EntityMetadata"] = Invoke(ct => metadataOperations.RetrieveAllEntitiesAsync(typedRequest, ct)).ToArray();
        return response;
    }
}
