using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web.Script.Serialization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

internal static class Program
{
    private static readonly JavaScriptSerializer Serializer = new JavaScriptSerializer();

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length < 2)
            {
                throw new InvalidOperationException("Usage: <scenario> <connectionString> [args...]");
            }

            var scenario = args[0];
            var connectionString = args[1];
            var scenarioArgs = args.Skip(2).ToArray();

            IDictionary<string, object> payload = scenario switch
            {
                "ready" => RunReady(connectionString),
                "crud" => RunCrud(connectionString),
                "paged-query" => RunPagedQuery(connectionString),
                "metadata" => RunMetadata(connectionString),
                "create" => RunCreate(connectionString, scenarioArgs),
                "retrieve" => RunRetrieve(connectionString, scenarioArgs),
                "unsupported-link-query" => RunUnsupportedLinkQuery(connectionString),
                _ => throw new InvalidOperationException("Unknown scenario '" + scenario + "'.")
            };

            Console.Out.Write(Serializer.Serialize(payload));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static IDictionary<string, object> RunReady(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            return new Dictionary<string, object>
            {
                ["isReady"] = client.IsReady,
                ["connectedOrgPublishedEndpoints"] = client.ConnectedOrgPublishedEndpoints != null
            };
        }
    }

    private static IDictionary<string, object> RunCrud(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var createTarget = new Entity("account");
            createTarget["name"] = "Contoso";
            createTarget["accountnumber"] = "A-100";
            createTarget["isactive"] = true;

            var createdId = client.Create(createTarget);

            var retrieved = client.Retrieve(
                "account",
                createdId,
                new ColumnSet("name", "accountnumber", "isactive"));

            var updateTarget = new Entity("account", createdId);
            updateTarget["accountnumber"] = "A-200";
            client.Update(updateTarget);

            var query = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name", "accountnumber"),
                TopCount = 10
            };
            query.Criteria.AddCondition("name", ConditionOperator.Equal, "Contoso");
            query.Orders.Add(new OrderExpression("name", OrderType.Ascending));

            var results = client.RetrieveMultiple(query);

            client.Delete("account", createdId);

            return new Dictionary<string, object>
            {
                ["createdId"] = createdId.ToString(),
                ["retrievedName"] = retrieved.GetAttributeValue<string>("name"),
                ["retrievedAccountNumber"] = retrieved.GetAttributeValue<string>("accountnumber"),
                ["updatedAccountNumber"] = results.Entities[0].GetAttributeValue<string>("accountnumber"),
                ["queryCount"] = results.Entities.Count,
                ["moreRecords"] = results.MoreRecords
            };
        }
    }

    private static IDictionary<string, object> RunCreate(string connectionString, string[] args)
    {
        if (args.Length < 2)
        {
            throw new InvalidOperationException("Scenario 'create' requires <name> <accountnumber>.");
        }

        using (var client = OpenClient(connectionString))
        {
            var target = new Entity("account");
            target["name"] = args[0];
            target["accountnumber"] = args[1];

            var id = client.Create(target);

            return new Dictionary<string, object>
            {
                ["id"] = id.ToString(),
                ["name"] = args[0],
                ["accountnumber"] = args[1]
            };
        }
    }

    private static IDictionary<string, object> RunPagedQuery(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            foreach (var name in new[] { "Alpha", "Bravo", "Charlie" })
            {
                var target = new Entity("account");
                target["name"] = name;
                target["accountnumber"] = name.Substring(0, 1);
                client.Create(target);
            }

            var firstPageQuery = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                PageInfo = new PagingInfo
                {
                    Count = 1,
                    PageNumber = 1
                }
            };
            firstPageQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

            var firstPage = client.RetrieveMultiple(firstPageQuery);

            var secondPageQuery = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name"),
                PageInfo = new PagingInfo
                {
                    Count = 1,
                    PageNumber = 2,
                    PagingCookie = firstPage.PagingCookie
                }
            };
            secondPageQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

            var secondPage = client.RetrieveMultiple(secondPageQuery);

            return new Dictionary<string, object>
            {
                ["firstPageName"] = firstPage.Entities[0].GetAttributeValue<string>("name"),
                ["firstPageCount"] = firstPage.Entities.Count,
                ["firstMoreRecords"] = firstPage.MoreRecords,
                ["firstPagingCookiePresent"] = !string.IsNullOrWhiteSpace(firstPage.PagingCookie),
                ["secondPageName"] = secondPage.Entities[0].GetAttributeValue<string>("name"),
                ["secondPageCount"] = secondPage.Entities.Count,
                ["secondMoreRecords"] = secondPage.MoreRecords
            };
        }
    }

    private static IDictionary<string, object> RunRetrieve(string connectionString, string[] args)
    {
        if (args.Length < 1)
        {
            throw new InvalidOperationException("Scenario 'retrieve' requires <id>.");
        }

        using (var client = OpenClient(connectionString))
        {
            var entity = client.Retrieve(
                "account",
                Guid.Parse(args[0]),
                new ColumnSet("name", "accountnumber", "isactive"));

            return new Dictionary<string, object>
            {
                ["id"] = entity.Id.ToString(),
                ["logicalName"] = entity.LogicalName,
                ["attributes"] = ToDictionary(entity.Attributes)
            };
        }
    }

    private static IDictionary<string, object> RunMetadata(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var entityResponse = (RetrieveEntityResponse)client.Execute(new RetrieveEntityRequest
            {
                LogicalName = "account",
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes
            });

            var attributeResponse = (RetrieveAttributeResponse)client.Execute(new RetrieveAttributeRequest
            {
                EntityLogicalName = "account",
                LogicalName = "name"
            });

            var allEntitiesResponse = (RetrieveAllEntitiesResponse)client.Execute(new RetrieveAllEntitiesRequest
            {
                EntityFilters = EntityFilters.Entity
            });

            var entityMetadata = entityResponse.EntityMetadata;
            var attributeMetadata = attributeResponse.AttributeMetadata;

            return new Dictionary<string, object>
            {
                ["entityLogicalName"] = entityMetadata.LogicalName,
                ["entitySetName"] = entityMetadata.EntitySetName,
                ["primaryIdAttribute"] = entityMetadata.PrimaryIdAttribute,
                ["primaryNameAttribute"] = entityMetadata.PrimaryNameAttribute,
                ["attributeCount"] = entityMetadata.Attributes.Length,
                ["objectTypeCode"] = entityMetadata.ObjectTypeCode.GetValueOrDefault(),
                ["attributeLogicalName"] = attributeMetadata.LogicalName,
                ["attributeType"] = attributeMetadata.AttributeType.HasValue
                    ? attributeMetadata.AttributeType.Value.ToString()
                    : string.Empty,
                ["attributeRequiredLevel"] = attributeMetadata.RequiredLevel != null
                    ? attributeMetadata.RequiredLevel.Value.ToString()
                    : string.Empty,
                ["allEntitiesCount"] = allEntitiesResponse.EntityMetadata.Length
            };
        }
    }

    private static IDictionary<string, object> RunUnsupportedLinkQuery(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            try
            {
                var query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet("name")
                };
                query.LinkEntities.Add(new LinkEntity("account", "account", "accountid", "accountid", JoinOperator.Inner));

                client.RetrieveMultiple(query);

                return new Dictionary<string, object>
                {
                    ["faulted"] = false
                };
            }
            catch (FaultException<OrganizationServiceFault> fault)
            {
                return new Dictionary<string, object>
                {
                    ["faulted"] = true,
                    ["errorCode"] = fault.Detail.ErrorCode,
                    ["message"] = fault.Detail.Message
                };
            }
        }
    }

    private static CrmServiceClient OpenClient(string connectionString)
    {
        var client = new CrmServiceClient(connectionString);
        if (!client.IsReady)
        {
            client.Dispose();
            throw new InvalidOperationException(
                "CrmServiceClient was not ready. LastCrmError=" + client.LastCrmError);
        }

        return client;
    }

    private static IDictionary<string, object> ToDictionary(AttributeCollection attributes)
    {
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in attributes)
        {
            values[pair.Key] = NormalizeValue(pair.Value);
        }

        return values;
    }

    private static object NormalizeValue(object value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is DateTime dateTime)
        {
            return dateTime.ToString("o");
        }

        if (value is EntityReference entityReference)
        {
            return new Dictionary<string, object>
            {
                ["logicalName"] = entityReference.LogicalName,
                ["id"] = entityReference.Id.ToString()
            };
        }

        if (value is IEnumerable enumerable && !(value is string))
        {
            return enumerable.Cast<object>().Select(NormalizeValue).ToArray();
        }

        return value;
    }
}
