using Dataverse.Emulator.Protocols.Common;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm.Execution;

internal abstract class DataverseXrmRequestHandlerBase(IHttpContextAccessor httpContextAccessor)
{
    protected T Invoke<T>(Func<CancellationToken, Task<ErrorOr<T>>> operation)
    {
        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var result = operation(cancellationToken).GetAwaiter().GetResult();

        if (result.IsError)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(result.Errors);
        }

        return result.Value;
    }

    protected void Invoke(Func<CancellationToken, Task<ErrorOr<Success>>> operation)
    {
        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var result = operation(cancellationToken).GetAwaiter().GetResult();

        if (result.IsError)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(result.Errors);
        }
    }

    protected string BuildAbsoluteUri(string relativePath)
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return relativePath;
        }

        var normalizedPath = relativePath.StartsWith("/", StringComparison.Ordinal)
            ? relativePath
            : $"/{relativePath}";

        return $"{request.Scheme}://{request.Host}{request.PathBase}{normalizedPath}";
    }

    protected static Entity? ResolveTargetEntity(OrganizationRequest request)
        => request switch
        {
            CreateRequest createRequest => createRequest.Target,
            UpdateRequest updateRequest => updateRequest.Target,
            _ when request.Parameters.TryGetValue("Target", out var target) => target as Entity,
            _ => null
        };

    protected static EntityReference? ResolveTargetReference(OrganizationRequest request)
        => request switch
        {
            RetrieveRequest retrieveRequest => retrieveRequest.Target,
            DeleteRequest deleteRequest => deleteRequest.Target,
            _ when request.Parameters.TryGetValue("Target", out var target) => target as EntityReference,
            _ => null
        };

    protected static ColumnSet? ResolveColumnSet(OrganizationRequest request)
        => request switch
        {
            RetrieveRequest retrieveRequest => retrieveRequest.ColumnSet,
            _ when request.Parameters.TryGetValue("ColumnSet", out var columnSet) => columnSet as ColumnSet,
            _ => null
        };

    protected static QueryBase? ResolveQuery(OrganizationRequest request)
        => request switch
        {
            RetrieveMultipleRequest retrieveMultipleRequest => retrieveMultipleRequest.Query,
            _ when request.Parameters.TryGetValue("Query", out var query) => query as QueryBase,
            _ => null
        };

    protected static Relationship? ResolveRelationship(OrganizationRequest request)
        => request switch
        {
            AssociateRequest associateRequest => associateRequest.Relationship,
            DisassociateRequest disassociateRequest => disassociateRequest.Relationship,
            _ when request.Parameters.TryGetValue("Relationship", out var relationship) => relationship as Relationship,
            _ => null
        };

    protected static EntityReferenceCollection? ResolveRelatedEntities(OrganizationRequest request)
        => request switch
        {
            AssociateRequest associateRequest => associateRequest.RelatedEntities,
            DisassociateRequest disassociateRequest => disassociateRequest.RelatedEntities,
            _ when request.Parameters.TryGetValue("RelatedEntities", out var relatedEntities) => relatedEntities as EntityReferenceCollection,
            _ => null
        };
}
