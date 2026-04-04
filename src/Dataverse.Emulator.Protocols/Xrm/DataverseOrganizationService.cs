using Dataverse.Emulator.Protocols.Xrm.Execution;
using Dataverse.Emulator.Protocols.Xrm.Operations;
using Dataverse.Emulator.Protocols.Common;
using ErrorOr;
using Microsoft.AspNetCore.Http;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm;

public sealed class DataverseOrganizationService(
    DataverseXrmRecordOperations recordOperations,
    DataverseXrmOrganizationRequestDispatcher requestDispatcher,
    IHttpContextAccessor httpContextAccessor)
    : IOrganizationServiceSoap
{
    public Guid Create(Entity entity)
        => Invoke(ct => recordOperations.CreateAsync(entity, ct));

    public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        => Invoke(ct => recordOperations.RetrieveAsync(entityName, id, columnSet, ct));

    public void Update(Entity entity)
        => Invoke(ct => recordOperations.UpdateAsync(entity, ct));

    public void Delete(string entityName, Guid id)
        => Invoke(ct => recordOperations.DeleteAsync(entityName, id, ct));

    public OrganizationResponse Execute(OrganizationRequest request)
        => requestDispatcher.Dispatch(request);

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
        => Invoke(ct => recordOperations.RetrieveMultipleAsync(query, ct));

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

}
