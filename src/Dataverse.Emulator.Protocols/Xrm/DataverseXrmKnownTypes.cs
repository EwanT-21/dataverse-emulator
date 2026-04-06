using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Organization;
using Microsoft.Xrm.Sdk.Query;
using System.Reflection;

namespace Dataverse.Emulator.Protocols.Xrm;

internal static class DataverseXrmKnownTypes
{
    private static readonly Type[] KnownTypes = BuildKnownTypes()
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static IEnumerable<Type> GetKnownTypes(ICustomAttributeProvider provider) => KnownTypes;

    private static IEnumerable<Type> BuildKnownTypes()
    {
        var knownTypes = new HashSet<Type>
        {
            typeof(OrganizationRequest),
            typeof(OrganizationRequestCollection),
            typeof(OrganizationResponse),
            typeof(OrganizationServiceFault),
            typeof(ExecuteMultipleSettings),
            typeof(ExecuteMultipleResponseItem),
            typeof(ExecuteMultipleResponseItemCollection),
            typeof(AssociateRequest),
            typeof(AssociateResponse),
            typeof(DisassociateRequest),
            typeof(DisassociateResponse),
            typeof(RetrieveVersionRequest),
            typeof(RetrieveVersionResponse),
            typeof(RetrieveProvisionedLanguagesRequest),
            typeof(RetrieveProvisionedLanguagesResponse),
            typeof(RetrieveRelationshipRequest),
            typeof(RetrieveRelationshipResponse),
            typeof(Entity),
            typeof(EntityCollection),
            typeof(EntityReference),
            typeof(EntityReferenceCollection),
            typeof(Relationship),
            typeof(ColumnSet),
            typeof(QueryBase),
            typeof(QueryExpression),
            typeof(LinkEntity),
            typeof(FilterExpression),
            typeof(ConditionExpression),
            typeof(OrderExpression),
            typeof(OrganizationDetail),
            typeof(EndpointCollection),
            typeof(OptionSetValue),
            typeof(Money),
            typeof(AliasedValue),
            typeof(Label),
            typeof(LocalizedLabel),
            typeof(LocalizedLabelCollection),
            typeof(BooleanManagedProperty),
            typeof(AttributeMetadata),
            typeof(EntityMetadata),
            typeof(OneToManyRelationshipMetadata),
            typeof(RelationshipMetadataBase),
            typeof(StringAttributeMetadata),
            typeof(BooleanAttributeMetadata),
            typeof(DateTimeAttributeMetadata),
            typeof(DecimalAttributeMetadata),
            typeof(IntegerAttributeMetadata),
            typeof(UniqueIdentifierAttributeMetadata),
            typeof(LookupAttributeMetadata),
            typeof(AssociatedMenuConfiguration),
            typeof(CascadeConfiguration),
            typeof(OptionMetadata),
            typeof(BooleanOptionSetMetadata),
            typeof(AttributeRequiredLevelManagedProperty)
        };

        var assemblies = new[]
        {
            typeof(Entity).Assembly,
            typeof(CreateRequest).Assembly,
            typeof(RetrieveVersionRequest).Assembly,
            typeof(RetrieveCurrentOrganizationRequest).Assembly,
            typeof(EntityMetadata).Assembly
        }.Distinct();

        foreach (var assembly in assemblies)
        {
            foreach (var type in SafeGetTypes(assembly))
            {
                if (!type.IsPublic || type.IsAbstract || type.ContainsGenericParameters)
                {
                    continue;
                }

                if (typeof(OrganizationRequest).IsAssignableFrom(type)
                    || typeof(OrganizationResponse).IsAssignableFrom(type)
                    || typeof(QueryBase).IsAssignableFrom(type))
                {
                    knownTypes.Add(type);
                    continue;
                }

                if (typeof(MetadataBase).IsAssignableFrom(type))
                {
                    knownTypes.Add(type);
                }
            }
        }

        return knownTypes;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null)!;
        }
    }
}
