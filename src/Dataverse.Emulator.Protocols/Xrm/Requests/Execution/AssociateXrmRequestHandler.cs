using Dataverse.Emulator.Protocols.Common;
using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Dataverse.Emulator.Protocols.Xrm.Requests.Execution;

internal sealed class AssociateXrmRequestHandler(
    DataverseXrmRelationshipOperations relationshipOperations,
    IHttpContextAccessor httpContextAccessor)
    : DataverseXrmRequestHandlerBase(httpContextAccessor), IXrmOrganizationRequestHandler
{
    public string RequestName => "Associate";

    public OrganizationResponse Handle(OrganizationRequest request)
    {
        if (request is not AssociateRequest typedRequest)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("AssociateRequest")]);
        }

        Invoke(ct => relationshipOperations.AssociateAsync(
            typedRequest.Target?.LogicalName ?? string.Empty,
            typedRequest.Target?.Id ?? Guid.Empty,
            typedRequest.Relationship!,
            typedRequest.RelatedEntities!,
            ct));

        return new AssociateResponse();
    }
}
