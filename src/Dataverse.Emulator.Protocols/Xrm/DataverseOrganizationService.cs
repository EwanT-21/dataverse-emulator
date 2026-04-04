using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Application.Records;
using Dataverse.Emulator.Protocols.Common;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using OrganizationEndpointType = Microsoft.Xrm.Sdk.Organization.EndpointType;

namespace Dataverse.Emulator.Protocols.Xrm;

public sealed class DataverseOrganizationService(
    IMediator mediator,
    IHttpContextAccessor httpContextAccessor)
    : IOrganizationServiceSoap
{
    public Guid Create(Entity entity)
        => Invoke(ct => CreateAsync(entity, ct));

    public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        => Invoke(ct => RetrieveAsync(entityName, id, columnSet, ct));

    public void Update(Entity entity)
        => Invoke(ct => UpdateAsync(entity, ct));

    public void Delete(string entityName, Guid id)
        => Invoke(ct => DeleteAsync(entityName, id, ct));

    public OrganizationResponse Execute(OrganizationRequest request)
    {
        if (request is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("request")]);
        }

        return request.RequestName switch
        {
            "RetrieveCurrentOrganization" => CreateRetrieveCurrentOrganizationResponse(),
            "WhoAmI" => CreateWhoAmIResponse(),
            "Create" => CreateCreateResponse(request),
            "Retrieve" => CreateRetrieveResponse(request),
            "Update" => CreateUpdateResponse(request),
            "Delete" => CreateDeleteResponse(request),
            "RetrieveMultiple" => CreateRetrieveMultipleResponse(request),
            _ => throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.UnsupportedOrganizationRequest(
                    request.RequestName ?? request.GetType().Name)])
        };
    }

    public void Associate(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities)
        => throw DataverseProtocolErrorMapper.ToFaultException(
            [DataverseXrmErrors.UnsupportedOperation("Associate")]);

    public void Disassociate(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities)
        => throw DataverseProtocolErrorMapper.ToFaultException(
            [DataverseXrmErrors.UnsupportedOperation("Disassociate")]);

    public EntityCollection RetrieveMultiple(QueryBase query)
        => Invoke(ct => RetrieveMultipleAsync(query, ct));

    private OrganizationResponse CreateCreateResponse(OrganizationRequest request)
    {
        var target = ResolveTargetEntity(request);
        if (target is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Target")]);
        }

        var response = new CreateResponse();
        response.Results["id"] = Create(target);
        return response;
    }

    private OrganizationResponse CreateRetrieveResponse(OrganizationRequest request)
    {
        if (request is RetrieveRequest { RelatedEntitiesQuery: not null })
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.UnsupportedOperation("Retrieve related entities")]);
        }

        var target = ResolveTargetReference(request);
        if (target is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Target")]);
        }

        var columnSet = ResolveColumnSet(request);
        if (columnSet is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("ColumnSet")]);
        }

        var response = new RetrieveResponse();
        response.Results["Entity"] = Retrieve(target.LogicalName, target.Id, columnSet);
        return response;
    }

    private OrganizationResponse CreateUpdateResponse(OrganizationRequest request)
    {
        var target = ResolveTargetEntity(request);
        if (target is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Target")]);
        }

        Update(target);
        return new UpdateResponse();
    }

    private OrganizationResponse CreateDeleteResponse(OrganizationRequest request)
    {
        var target = ResolveTargetReference(request);
        if (target is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Target")]);
        }

        Delete(target.LogicalName, target.Id);
        return new DeleteResponse();
    }

    private OrganizationResponse CreateRetrieveMultipleResponse(OrganizationRequest request)
    {
        var query = ResolveQuery(request);
        if (query is null)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(
                [DataverseXrmErrors.ParameterRequired("Query")]);
        }

        var response = new RetrieveMultipleResponse();
        response.Results["EntityCollection"] = RetrieveMultiple(query);
        return response;
    }

    private OrganizationResponse CreateRetrieveCurrentOrganizationResponse()
    {
        var response = new RetrieveCurrentOrganizationResponse();
        response.Results["Detail"] = new OrganizationDetail
        {
            OrganizationId = DataverseXrmConstants.OrganizationId,
            FriendlyName = DataverseXrmConstants.OrganizationFriendlyName,
            OrganizationVersion = DataverseXrmConstants.OrganizationVersion,
            UniqueName = DataverseXrmConstants.OrganizationUniqueName,
            UrlName = DataverseXrmConstants.OrganizationUniqueName,
            State = OrganizationState.Enabled,
            Endpoints = new EndpointCollection
            {
                { OrganizationEndpointType.OrganizationService, BuildAbsoluteUri(DataverseXrmConstants.OrganizationServicePath) },
                { OrganizationEndpointType.WebApplication, BuildAbsoluteUri("/") }
            }
        };

        return response;
    }

    private OrganizationResponse CreateWhoAmIResponse()
    {
        var response = new WhoAmIResponse();
        response.Results["UserId"] = DataverseXrmConstants.DefaultUserId;
        response.Results["BusinessUnitId"] = DataverseXrmConstants.DefaultBusinessUnitId;
        response.Results["OrganizationId"] = DataverseXrmConstants.OrganizationId;
        return response;
    }

    private async Task<ErrorOr<Guid>> CreateAsync(Entity entity, CancellationToken cancellationToken)
    {
        if (entity is null)
        {
            return DataverseXrmErrors.ParameterRequired("entity");
        }

        var tableResult = await mediator.Send(new GetTableDefinitionQuery(entity.LogicalName), cancellationToken);
        if (tableResult.IsError)
        {
            return tableResult.Errors;
        }

        var valuesResult = DataverseXrmEntityMapper.ToCreateValues(entity, tableResult.Value);
        if (valuesResult.IsError)
        {
            return valuesResult.Errors;
        }

        return await mediator.Send(
            new CreateRowCommand(tableResult.Value.LogicalName, valuesResult.Value),
            cancellationToken);
    }

    private async Task<ErrorOr<Entity>> RetrieveAsync(
        string entityName,
        Guid id,
        ColumnSet columnSet,
        CancellationToken cancellationToken)
    {
        var tableResult = await mediator.Send(new GetTableDefinitionQuery(entityName), cancellationToken);
        if (tableResult.IsError)
        {
            return tableResult.Errors;
        }

        var selectedColumnsResult = DataverseXrmEntityMapper.ResolveSelectedColumns(columnSet);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        var recordResult = await mediator.Send(new GetRowByIdQuery(tableResult.Value.LogicalName, id), cancellationToken);
        if (recordResult.IsError)
        {
            return recordResult.Errors;
        }

        var projected = recordResult.Value.Project(selectedColumnsResult.Value);
        return DataverseXrmEntityMapper.ToEntity(tableResult.Value, projected);
    }

    private async Task<ErrorOr<Success>> UpdateAsync(Entity entity, CancellationToken cancellationToken)
    {
        if (entity is null)
        {
            return DataverseXrmErrors.ParameterRequired("entity");
        }

        var tableResult = await mediator.Send(new GetTableDefinitionQuery(entity.LogicalName), cancellationToken);
        if (tableResult.IsError)
        {
            return tableResult.Errors;
        }

        var idResult = DataverseXrmEntityMapper.ResolveRecordId(entity, tableResult.Value);
        if (idResult.IsError)
        {
            return idResult.Errors;
        }

        var valuesResult = DataverseXrmEntityMapper.ToUpdateValues(entity, tableResult.Value);
        if (valuesResult.IsError)
        {
            return valuesResult.Errors;
        }

        var updateResult = await mediator.Send(
            new UpdateRowCommand(tableResult.Value.LogicalName, idResult.Value, valuesResult.Value),
            cancellationToken);

        return updateResult.IsError
            ? updateResult.Errors
            : Result.Success;
    }

    private async Task<ErrorOr<Success>> DeleteAsync(
        string entityName,
        Guid id,
        CancellationToken cancellationToken)
    {
        var tableResult = await mediator.Send(new GetTableDefinitionQuery(entityName), cancellationToken);
        if (tableResult.IsError)
        {
            return tableResult.Errors;
        }

        var deleteResult = await mediator.Send(
            new DeleteRowCommand(tableResult.Value.LogicalName, id),
            cancellationToken);

        return deleteResult.IsError
            ? deleteResult.Errors
            : Result.Success;
    }

    private async Task<ErrorOr<EntityCollection>> RetrieveMultipleAsync(
        QueryBase query,
        CancellationToken cancellationToken)
    {
        var queryResult = DataverseXrmQueryExpressionTranslator.Translate(query);
        if (queryResult.IsError)
        {
            return queryResult.Errors;
        }

        var tableResult = await mediator.Send(
            new GetTableDefinitionQuery(queryResult.Value.TableLogicalName),
            cancellationToken);
        if (tableResult.IsError)
        {
            return tableResult.Errors;
        }

        var rowsResult = await mediator.Send(new ListRowsQuery(queryResult.Value), cancellationToken);
        if (rowsResult.IsError)
        {
            return rowsResult.Errors;
        }

        return DataverseXrmEntityMapper.ToEntityCollection(tableResult.Value, rowsResult.Value.Items);
    }

    private T Invoke<T>(Func<CancellationToken, Task<ErrorOr<T>>> operation)
    {
        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var result = operation(cancellationToken).GetAwaiter().GetResult();

        if (result.IsError)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(result.Errors);
        }

        return result.Value;
    }

    private void Invoke(Func<CancellationToken, Task<ErrorOr<Success>>> operation)
    {
        var cancellationToken = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var result = operation(cancellationToken).GetAwaiter().GetResult();

        if (result.IsError)
        {
            throw DataverseProtocolErrorMapper.ToFaultException(result.Errors);
        }
    }

    private string BuildAbsoluteUri(string relativePath)
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

    private static Entity? ResolveTargetEntity(OrganizationRequest request)
        => request switch
        {
            CreateRequest createRequest => createRequest.Target,
            UpdateRequest updateRequest => updateRequest.Target,
            _ when request.Parameters.TryGetValue("Target", out var target) => target as Entity,
            _ => null
        };

    private static EntityReference? ResolveTargetReference(OrganizationRequest request)
        => request switch
        {
            RetrieveRequest retrieveRequest => retrieveRequest.Target,
            DeleteRequest deleteRequest => deleteRequest.Target,
            _ when request.Parameters.TryGetValue("Target", out var target) => target as EntityReference,
            _ => null
        };

    private static ColumnSet? ResolveColumnSet(OrganizationRequest request)
        => request switch
        {
            RetrieveRequest retrieveRequest => retrieveRequest.ColumnSet,
            _ when request.Parameters.TryGetValue("ColumnSet", out var columnSet) => columnSet as ColumnSet,
            _ => null
        };

    private static QueryBase? ResolveQuery(OrganizationRequest request)
        => request switch
        {
            RetrieveMultipleRequest retrieveMultipleRequest => retrieveMultipleRequest.Query,
            _ when request.Parameters.TryGetValue("Query", out var query) => query as QueryBase,
            _ => null
        };
}
