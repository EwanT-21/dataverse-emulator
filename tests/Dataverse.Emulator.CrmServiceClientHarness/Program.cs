using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Web.Script.Serialization;
using Microsoft.Crm.Sdk.Messages;
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
                "advanced-query" => RunAdvancedQuery(connectionString),
                "linked-query" => RunLinkedQuery(connectionString),
                "fetchxml" => RunFetchXmlQuery(connectionString),
                "execute-multiple" => RunExecuteMultiple(connectionString),
                "upsert" => RunUpsert(connectionString),
                "version" => RunVersion(connectionString),
                "provisioned-languages" => RunProvisionedLanguages(connectionString),
                "metadata" => RunMetadata(connectionString),
                "associate" => RunAssociate(connectionString),
                "relationship-metadata" => RunRelationshipMetadata(connectionString),
                "create" => RunCreate(connectionString, scenarioArgs),
                "retrieve" => RunRetrieve(connectionString, scenarioArgs),
                "unsupported-request" => RunUnsupportedRequest(connectionString),
                "unsupported-link-query" => RunUnsupportedLinkQuery(connectionString),
                "unsupported-fetchxml-link-entity" => RunUnsupportedFetchXmlLinkEntity(connectionString),
                "unsupported-upsert-alternate-key" => RunUnsupportedUpsertAlternateKey(connectionString),
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

    private static IDictionary<string, object> RunAdvancedQuery(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            CreateAccount(client, "Alpha", "A-100", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            CreateAccount(client, "Alpine", null, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            CreateAccount(client, "Bravo", "B-100", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
            CreateAccount(client, "Charlie", "C-100", new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc));

            var groupedQuery = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name", "accountnumber")
            };
            groupedQuery.Criteria.AddCondition("accountnumber", ConditionOperator.NotNull);
            var groupedNames = groupedQuery.Criteria.AddFilter(LogicalOperator.Or);
            groupedNames.AddCondition("name", ConditionOperator.BeginsWith, "Al");
            groupedNames.AddCondition("name", ConditionOperator.Equal, "Charlie");
            groupedQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

            var groupedResults = client.RetrieveMultiple(groupedQuery);

            var inQuery = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name", "accountnumber")
            };
            inQuery.Criteria.AddCondition("accountnumber", ConditionOperator.In, "A-100", "C-100");
            inQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

            var inResults = client.RetrieveMultiple(inQuery);

            var likeQuery = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name")
            };
            likeQuery.Criteria.AddCondition("name", ConditionOperator.Like, "Al%");
            likeQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

            var likeResults = client.RetrieveMultiple(likeQuery);

            var rangeQuery = new QueryExpression("account")
            {
                ColumnSet = new ColumnSet("name", "createdon")
            };
            rangeQuery.Criteria.AddCondition("createdon", ConditionOperator.GreaterEqual, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            rangeQuery.Criteria.AddCondition("createdon", ConditionOperator.LessThan, new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc));
            rangeQuery.Orders.Add(new OrderExpression("name", OrderType.Ascending));

            var rangeResults = client.RetrieveMultiple(rangeQuery);

            return new Dictionary<string, object>
            {
                ["groupedCount"] = groupedResults.Entities.Count,
                ["groupedNames"] = groupedResults.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray(),
                ["inCount"] = inResults.Entities.Count,
                ["inNames"] = inResults.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray(),
                ["likeCount"] = likeResults.Entities.Count,
                ["likeNames"] = likeResults.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray(),
                ["rangeCount"] = rangeResults.Entities.Count,
                ["rangeNames"] = rangeResults.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray()
            };
        }
    }

    private static IDictionary<string, object> RunLinkedQuery(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var alphaAccountId = CreateAccount(client, "Alpha Account", "A-100", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var bravoAccountId = CreateAccount(client, "Bravo Account", "B-100", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

            CreateContact(client, "Alice Alpha", "alice@example.com", alphaAccountId, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
            CreateContact(client, "Aria Alpha", "aria@example.com", alphaAccountId, new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc));
            CreateContact(client, "Brett Bravo", "brett@example.com", bravoAccountId, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc));

            var query = new QueryExpression("contact")
            {
                ColumnSet = new ColumnSet("fullname", "emailaddress1"),
                TopCount = 10
            };
            query.Criteria.AddCondition("fullname", ConditionOperator.BeginsWith, "A");
            query.Orders.Add(new OrderExpression("fullname", OrderType.Ascending));

            var parentAccountLink = new LinkEntity("contact", "account", "parentcustomerid", "accountid", JoinOperator.Inner)
            {
                EntityAlias = "parentaccount",
                Columns = new ColumnSet("name", "accountnumber")
            };
            parentAccountLink.LinkCriteria.AddCondition("accountnumber", ConditionOperator.BeginsWith, "A");
            query.LinkEntities.Add(parentAccountLink);

            var results = client.RetrieveMultiple(query);

            return new Dictionary<string, object>
            {
                ["count"] = results.Entities.Count,
                ["names"] = results.Entities.Select(entity => entity.GetAttributeValue<string>("fullname")).ToArray(),
                ["accountNames"] = results.Entities
                    .Select(entity => entity.GetAttributeValue<AliasedValue>("parentaccount.name"))
                    .Where(value => value != null)
                    .Select(value => (string)value.Value)
                    .ToArray(),
                ["accountNumbers"] = results.Entities
                    .Select(entity => entity.GetAttributeValue<AliasedValue>("parentaccount.accountnumber"))
                    .Where(value => value != null)
                    .Select(value => (string)value.Value)
                    .ToArray()
            };
        }
    }

    private static IDictionary<string, object> RunFetchXmlQuery(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            CreateAccount(client, "Alpha", "A-100", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            CreateAccount(client, "Alpine", "AL-200", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            CreateAccount(client, "Bravo", "B-100", new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));
            CreateAccount(client, "Charlie", "C-100", new DateTime(2026, 1, 4, 0, 0, 0, DateTimeKind.Utc));

            var firstPage = client.RetrieveMultiple(new FetchExpression(
                "<fetch count='2' page='1'>" +
                "<entity name='account'>" +
                "<attribute name='name' />" +
                "<attribute name='accountnumber' />" +
                "<filter type='or'>" +
                "<condition attribute='name' operator='begins-with' value='Al' />" +
                "<condition attribute='name' operator='eq' value='Charlie' />" +
                "</filter>" +
                "<order attribute='name' />" +
                "</entity>" +
                "</fetch>"));

            var escapedPagingCookie = System.Security.SecurityElement.Escape(firstPage.PagingCookie ?? string.Empty);
            var secondPage = client.RetrieveMultiple(new FetchExpression(
                "<fetch count='2' page='2' paging-cookie='" + escapedPagingCookie + "'>" +
                "<entity name='account'>" +
                "<attribute name='name' />" +
                "<attribute name='accountnumber' />" +
                "<filter type='or'>" +
                "<condition attribute='name' operator='begins-with' value='Al' />" +
                "<condition attribute='name' operator='eq' value='Charlie' />" +
                "</filter>" +
                "<order attribute='name' />" +
                "</entity>" +
                "</fetch>"));

            return new Dictionary<string, object>
            {
                ["firstPageCount"] = firstPage.Entities.Count,
                ["firstPageNames"] = firstPage.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray(),
                ["firstMoreRecords"] = firstPage.MoreRecords,
                ["firstPagingCookiePresent"] = !string.IsNullOrWhiteSpace(firstPage.PagingCookie),
                ["secondPageCount"] = secondPage.Entities.Count,
                ["secondPageNames"] = secondPage.Entities.Select(entity => entity.GetAttributeValue<string>("name")).ToArray(),
                ["secondMoreRecords"] = secondPage.MoreRecords
            };
        }
    }

    private static IDictionary<string, object> RunExecuteMultiple(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var batch = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                Requests = new OrganizationRequestCollection()
            };

            batch.Requests.Add(new CreateRequest
            {
                Target = CreateAccountEntity("Alpha", "A-100")
            });
            batch.Requests.Add(new CreateRequest
            {
                Target = CreateAccountEntity("Bravo", "B-100")
            });
            batch.Requests.Add(new RetrieveMultipleRequest
            {
                Query = new QueryExpression("account")
                {
                    ColumnSet = new ColumnSet("name"),
                    Orders =
                    {
                        new OrderExpression("name", OrderType.Ascending)
                    }
                }
            });

            var batchResponse = (ExecuteMultipleResponse)client.Execute(batch);
            var successIndices = new List<int>();
            EntityCollection retrievedEntities = null;

            foreach (var responseItem in batchResponse.Responses)
            {
                if (responseItem.Response is RetrieveMultipleResponse retrieveMultipleResponse)
                {
                    retrievedEntities = retrieveMultipleResponse.EntityCollection;
                }

                successIndices.Add(responseItem.RequestIndex);
            }

            return new Dictionary<string, object>
            {
                ["isFaulted"] = batchResponse.IsFaulted,
                ["responseCount"] = batchResponse.Responses.Count,
                ["successIndices"] = successIndices.ToArray(),
                ["createdCount"] = retrievedEntities != null ? retrievedEntities.Entities.Count : 0,
                ["createdNames"] = retrievedEntities != null
                    ? retrievedEntities.Entities
                    .Select(entity => entity.GetAttributeValue<string>("name"))
                    .ToArray()
                    : new string[0]
            };
        }
    }

    private static IDictionary<string, object> RunUpsert(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var createResponse = (UpsertResponse)client.Execute(new UpsertRequest
            {
                Target = CreateAccountEntity("Upserted", "UP-100")
            });

            var createdId = createResponse.Target.Id;

            var updateTarget = new Entity("account", createdId);
            updateTarget["accountnumber"] = "UP-200";

            var updateResponse = (UpsertResponse)client.Execute(new UpsertRequest
            {
                Target = updateTarget
            });

            var retrieved = client.Retrieve(
                "account",
                createdId,
                new ColumnSet("name", "accountnumber"));

            return new Dictionary<string, object>
            {
                ["createdId"] = createdId.ToString(),
                ["createRecordCreated"] = createResponse.RecordCreated,
                ["updateRecordCreated"] = updateResponse.RecordCreated,
                ["updateTargetId"] = updateResponse.Target.Id.ToString(),
                ["retrievedName"] = retrieved.GetAttributeValue<string>("name"),
                ["retrievedAccountNumber"] = retrieved.GetAttributeValue<string>("accountnumber")
            };
        }
    }

    private static IDictionary<string, object> RunVersion(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var response = (RetrieveVersionResponse)client.Execute(new RetrieveVersionRequest());

            return new Dictionary<string, object>
            {
                ["version"] = response.Version
            };
        }
    }

    private static IDictionary<string, object> RunProvisionedLanguages(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var response = (RetrieveProvisionedLanguagesResponse)client.Execute(new RetrieveProvisionedLanguagesRequest());

            return new Dictionary<string, object>
            {
                ["languages"] = response.RetrieveProvisionedLanguages.Cast<int>().ToArray()
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

    private static IDictionary<string, object> RunAssociate(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var accountId = CreateAccount(client, "Associated Account", "AS-100", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var contactId = CreateContact(client, "Unassigned Contact", "unassigned@example.com", null, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
            var relationship = new Relationship("contact_customer_accounts");
            var relatedEntities = new EntityReferenceCollection
            {
                new EntityReference("contact", contactId)
            };

            client.Associate("account", accountId, relationship, relatedEntities);

            var associatedContact = client.Retrieve(
                "contact",
                contactId,
                new ColumnSet("fullname", "parentcustomerid"));
            var associatedReference = associatedContact.GetAttributeValue<EntityReference>("parentcustomerid");

            client.Execute(new DisassociateRequest
            {
                Target = new EntityReference("account", accountId),
                Relationship = relationship,
                RelatedEntities = relatedEntities
            });

            var disassociatedContact = client.Retrieve(
                "contact",
                contactId,
                new ColumnSet("fullname", "parentcustomerid"));
            var disassociatedReference = disassociatedContact.GetAttributeValue<EntityReference>("parentcustomerid");

            return new Dictionary<string, object>
            {
                ["accountId"] = accountId.ToString(),
                ["contactId"] = contactId.ToString(),
                ["associatedParentId"] = associatedReference != null ? associatedReference.Id.ToString() : string.Empty,
                ["associatedParentLogicalName"] = associatedReference != null ? associatedReference.LogicalName : string.Empty,
                ["disassociatedParentPresent"] = disassociatedReference != null
            };
        }
    }

    private static IDictionary<string, object> RunRelationshipMetadata(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            var relationshipResponse = (RetrieveRelationshipResponse)client.Execute(new RetrieveRelationshipRequest
            {
                Name = "contact_customer_accounts"
            });
            var relationshipMetadata = (OneToManyRelationshipMetadata)relationshipResponse.RelationshipMetadata;
            var accountEntityResponse = (RetrieveEntityResponse)client.Execute(new RetrieveEntityRequest
            {
                LogicalName = "account",
                EntityFilters = EntityFilters.Entity | EntityFilters.Relationships
            });
            var contactEntityResponse = (RetrieveEntityResponse)client.Execute(new RetrieveEntityRequest
            {
                LogicalName = "contact",
                EntityFilters = EntityFilters.Entity | EntityFilters.Relationships
            });

            return new Dictionary<string, object>
            {
                ["schemaName"] = relationshipMetadata.SchemaName,
                ["referencedEntity"] = relationshipMetadata.ReferencedEntity,
                ["referencingEntity"] = relationshipMetadata.ReferencingEntity,
                ["referencingAttribute"] = relationshipMetadata.ReferencingAttribute,
                ["accountOneToManyCount"] = accountEntityResponse.EntityMetadata.OneToManyRelationships.Length,
                ["accountOneToManyNames"] = accountEntityResponse.EntityMetadata.OneToManyRelationships.Select(relationship => relationship.SchemaName).ToArray(),
                ["contactManyToOneCount"] = contactEntityResponse.EntityMetadata.ManyToOneRelationships.Length,
                ["contactManyToOneNames"] = contactEntityResponse.EntityMetadata.ManyToOneRelationships.Select(relationship => relationship.SchemaName).ToArray()
            };
        }
    }

    private static IDictionary<string, object> RunUnsupportedLinkQuery(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            try
            {
                var query = new QueryExpression("contact")
                {
                    ColumnSet = new ColumnSet("fullname")
                };
                query.LinkEntities.Add(new LinkEntity("contact", "account", "parentcustomerid", "accountid", JoinOperator.LeftOuter));

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

    private static IDictionary<string, object> RunUnsupportedFetchXmlLinkEntity(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            try
            {
                client.RetrieveMultiple(new FetchExpression(
                    "<fetch>" +
                    "<entity name='account'>" +
                    "<attribute name='name' />" +
                    "<link-entity name='account' from='accountid' to='accountid' alias='child' />" +
                    "</entity>" +
                    "</fetch>"));

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

    private static IDictionary<string, object> RunUnsupportedUpsertAlternateKey(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            try
            {
                var target = new Entity("account");
                target.KeyAttributes["accountnumber"] = "A-100";
                target["name"] = "Alternate Key Upsert";

                client.Execute(new UpsertRequest
                {
                    Target = target
                });

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

    private static IDictionary<string, object> RunUnsupportedRequest(string connectionString)
    {
        using (var client = OpenClient(connectionString))
        {
            try
            {
                var request = new OrganizationRequest
                {
                    RequestName = "RetrieveUserLicenseInfo"
                };

                client.Execute(request);

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

    private static Guid CreateAccount(
        CrmServiceClient client,
        string name,
        string accountNumber,
        DateTime createdOn)
    {
        var target = CreateAccountEntity(name, accountNumber);
        target["createdon"] = createdOn;
        return client.Create(target);
    }

    private static Guid CreateContact(
        CrmServiceClient client,
        string fullName,
        string emailAddress,
        Guid? parentCustomerId,
        DateTime createdOn)
    {
        var target = new Entity("contact");
        target["fullname"] = fullName;
        target["emailaddress1"] = emailAddress;
        if (parentCustomerId.HasValue)
        {
            target["parentcustomerid"] = new EntityReference("account", parentCustomerId.Value);
        }

        target["createdon"] = createdOn;
        return client.Create(target);
    }

    private static Entity CreateAccountEntity(string name, string accountNumber)
    {
        var target = new Entity("account");
        target["name"] = name;

        if (!string.IsNullOrEmpty(accountNumber))
        {
            target["accountnumber"] = accountNumber;
        }

        return target;
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

        if (value is AliasedValue aliasedValue)
        {
            return new Dictionary<string, object>
            {
                ["entityLogicalName"] = aliasedValue.EntityLogicalName,
                ["attributeLogicalName"] = aliasedValue.AttributeLogicalName,
                ["value"] = NormalizeValue(aliasedValue.Value)
            };
        }

        if (value is IEnumerable enumerable && !(value is string))
        {
            return enumerable.Cast<object>().Select(NormalizeValue).ToArray();
        }

        return value;
    }
}
