using CoreWCF;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm;

[ServiceContract(
    Name = "IOrganizationService",
    Namespace = DataverseXrmConstants.ServiceNamespace)]
[ServiceKnownType(nameof(DataverseXrmKnownTypes.GetKnownTypes), typeof(DataverseXrmKnownTypes))]
public interface IOrganizationServiceSoap
{
    [OperationContract]
    Guid Create(Entity entity);

    [OperationContract]
    Entity Retrieve(string entityName, Guid id, ColumnSet columnSet);

    [OperationContract]
    void Update(Entity entity);

    [OperationContract]
    void Delete(string entityName, Guid id);

    [OperationContract]
    OrganizationResponse Execute(OrganizationRequest request);

    [OperationContract]
    void Associate(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities);

    [OperationContract]
    void Disassociate(
        string entityName,
        Guid entityId,
        Relationship relationship,
        EntityReferenceCollection relatedEntities);

    [OperationContract]
    EntityCollection RetrieveMultiple(QueryBase query);
}
