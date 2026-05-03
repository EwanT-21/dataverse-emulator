using Dataverse.Emulator.Domain.Common;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Protocols.Xrm.Queries;
using ErrorOr;
using Microsoft.Xrm.Sdk.Query;
using System.Globalization;
using System.Xml.Linq;
using QueryConditionOperator = Dataverse.Emulator.Domain.Queries.ConditionOperator;
using QueryFilterOperator = Dataverse.Emulator.Domain.Queries.FilterOperator;

namespace Dataverse.Emulator.Protocols.Xrm;

internal static class DataverseXrmFetchExpressionTranslator
{
    public static ErrorOr<bool> ContainsLinkEntity(FetchExpression fetchExpression)
    {
        var fetchDocumentResult = LoadFetchDocument(fetchExpression);
        if (fetchDocumentResult.IsError)
        {
            return fetchDocumentResult.Errors;
        }

        var entityResult = ResolveEntityElement(fetchDocumentResult.Value);
        if (entityResult.IsError)
        {
            return entityResult.Errors;
        }

        return entityResult.Value
            .Elements()
            .Any(element => element.Name.LocalName.Equals("link-entity", StringComparison.OrdinalIgnoreCase));
    }

    public static ErrorOr<string> ResolveEntityLogicalName(FetchExpression fetchExpression)
    {
        var fetchDocumentResult = LoadFetchDocument(fetchExpression);
        if (fetchDocumentResult.IsError)
        {
            return fetchDocumentResult.Errors;
        }

        var entityResult = ResolveEntityElement(fetchDocumentResult.Value);
        if (entityResult.IsError)
        {
            return entityResult.Errors;
        }

        var entityName = entityResult.Value.Attribute("name")?.Value;
        return string.IsNullOrWhiteSpace(entityName)
            ? DataverseXrmErrors.InvalidFetchXml("FetchXML requires an <entity> element with a non-empty 'name' attribute.")
            : entityName;
    }

    public static ErrorOr<IReadOnlyList<string>> ExtractLinkedEntityLogicalNames(FetchExpression fetchExpression)
    {
        var fetchDocumentResult = LoadFetchDocument(fetchExpression);
        if (fetchDocumentResult.IsError)
        {
            return fetchDocumentResult.Errors;
        }

        var entityResult = ResolveEntityElement(fetchDocumentResult.Value);
        if (entityResult.IsError)
        {
            return entityResult.Errors;
        }

        var names = new List<string>();
        CollectLinkEntityNames(entityResult.Value, names);
        return names.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static ErrorOr<DataverseTranslatedLinkedFetchQuery> TranslateLinked(
        FetchExpression fetchExpression,
        TableDefinition table,
        IReadOnlyDictionary<string, TableDefinition> linkedTables)
    {
        var fetchDocumentResult = LoadFetchDocument(fetchExpression);
        if (fetchDocumentResult.IsError)
        {
            return fetchDocumentResult.Errors;
        }

        var fetchElement = fetchDocumentResult.Value.Root!;
        if (IsTruthy(fetchElement.Attribute("aggregate")?.Value))
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("aggregate");
        }

        if (IsTruthy(fetchElement.Attribute("distinct")?.Value))
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("distinct");
        }

        if (IsTruthy(fetchElement.Attribute("returntotalrecordcount")?.Value))
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("returntotalrecordcount");
        }

        var entityResult = ResolveEntityElement(fetchDocumentResult.Value);
        if (entityResult.IsError)
        {
            return entityResult.Errors;
        }

        var entityElement = entityResult.Value;
        var entityNameResult = ResolveEntityLogicalName(fetchExpression);
        if (entityNameResult.IsError)
        {
            return entityNameResult.Errors;
        }

        if (!entityNameResult.Value.Equals(table.LogicalName, StringComparison.OrdinalIgnoreCase))
        {
            return DataverseXrmErrors.InvalidFetchXml(
                $"FetchXML entity '{entityNameResult.Value}' does not match table '{table.LogicalName}'.");
        }

        var selectedColumnsResult = TranslateSelectedColumns(entityElement);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        var topResult = TranslateTop(fetchElement);
        if (topResult.IsError)
        {
            return topResult.Errors;
        }

        var pageResult = TranslatePage(fetchElement, topResult.Value);
        if (pageResult.IsError)
        {
            return pageResult.Errors;
        }

        var ordersResult = TranslateOrders(entityElement);
        if (ordersResult.IsError)
        {
            return ordersResult.Errors;
        }

        var filterResult = TranslateFilters(entityElement, table);
        if (filterResult.IsError)
        {
            return filterResult.Errors;
        }

        var rawJoins = new List<RawLinkedFetchJoin>();
        foreach (var linkEntityElement in entityElement.Elements()
                     .Where(element => element.Name.LocalName.Equals("link-entity", StringComparison.OrdinalIgnoreCase)))
        {
            var joinResult = BuildLinkedJoins(
                linkEntityElement,
                entityNameResult.Value,
                entityNameResult.Value,
                rawJoins,
                linkedTables);
            if (joinResult.IsError)
            {
                return joinResult.Errors;
            }
        }

        var linkedSorts = new List<LinkedRecordSort>();
        var joinsList = new List<LinkedRecordJoin>(rawJoins.Count);
        foreach (var join in rawJoins)
        {
            joinsList.Add(new LinkedRecordJoin(
                TableLogicalName: join.TableLogicalName,
                Alias: join.Alias,
                FromAttributeName: join.FromAttributeName,
                ToAttributeName: join.ToAttributeName,
                SelectedColumns: join.SelectedColumns,
                ReturnAllColumns: join.ReturnAllColumns,
                Filter: ToLinkedFilter(join.Filter, join.Alias),
                ParentScopeName: join.ParentScopeName,
                JoinType: join.JoinType));
            linkedSorts.AddRange(ToLinkedSorts(join.Orders, join.Alias));
        }

        return new DataverseTranslatedLinkedFetchQuery(
            new LinkedRecordQuery(
                entityNameResult.Value,
                selectedColumnsResult.Value,
                joinsList.ToArray(),
                ToLinkedFilter(filterResult.Value, entityNameResult.Value),
                ToLinkedSorts(ordersResult.Value, entityNameResult.Value).Concat(linkedSorts).ToArray(),
                topResult.Value,
                pageResult.Value.PageRequest,
                pageResult.Value.CurrentPageNumber),
            pageResult.Value.CurrentPageNumber);
    }

    public static ErrorOr<DataverseTranslatedFetchQuery> Translate(
        FetchExpression fetchExpression,
        TableDefinition table)
    {
        var fetchDocumentResult = LoadFetchDocument(fetchExpression);
        if (fetchDocumentResult.IsError)
        {
            return fetchDocumentResult.Errors;
        }

        var fetchElement = fetchDocumentResult.Value.Root!;
        if (IsTruthy(fetchElement.Attribute("aggregate")?.Value))
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("aggregate");
        }

        if (IsTruthy(fetchElement.Attribute("distinct")?.Value))
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("distinct");
        }

        if (IsTruthy(fetchElement.Attribute("returntotalrecordcount")?.Value))
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("returntotalrecordcount");
        }

        var entityResult = ResolveEntityElement(fetchDocumentResult.Value);
        if (entityResult.IsError)
        {
            return entityResult.Errors;
        }

        var entityElement = entityResult.Value;
        var entityNameResult = ResolveEntityLogicalName(fetchExpression);
        if (entityNameResult.IsError)
        {
            return entityNameResult.Errors;
        }

        if (!entityNameResult.Value.Equals(table.LogicalName, StringComparison.OrdinalIgnoreCase))
        {
            return DataverseXrmErrors.InvalidFetchXml(
                $"FetchXML entity '{entityNameResult.Value}' does not match table '{table.LogicalName}'.");
        }

        if (entityElement.Elements().Any(element => element.Name.LocalName.Equals("link-entity", StringComparison.OrdinalIgnoreCase)))
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("link-entity");
        }

        var selectedColumnsResult = TranslateSelectedColumns(entityElement);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        var topResult = TranslateTop(fetchElement);
        if (topResult.IsError)
        {
            return topResult.Errors;
        }

        var pageResult = TranslatePage(fetchElement, topResult.Value);
        if (pageResult.IsError)
        {
            return pageResult.Errors;
        }

        var ordersResult = TranslateOrders(entityElement);
        if (ordersResult.IsError)
        {
            return ordersResult.Errors;
        }

        var filterResult = TranslateFilters(entityElement, table);
        if (filterResult.IsError)
        {
            return filterResult.Errors;
        }

        var recordQueryResult = RecordQuery.Create(
            entityNameResult.Value,
            selectedColumnsResult.Value,
            filter: filterResult.Value,
            sorts: ordersResult.Value,
            top: topResult.Value,
            page: pageResult.Value.PageRequest);
        if (recordQueryResult.IsError)
        {
            return recordQueryResult.Errors;
        }

        return new DataverseTranslatedFetchQuery(recordQueryResult.Value, pageResult.Value.CurrentPageNumber);
    }

    private static ErrorOr<XDocument> LoadFetchDocument(FetchExpression fetchExpression)
    {
        if (fetchExpression is null)
        {
            return DataverseXrmErrors.ParameterRequired("query");
        }

        if (string.IsNullOrWhiteSpace(fetchExpression.Query))
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML query text is required.");
        }

        try
        {
            var document = XDocument.Parse(fetchExpression.Query, LoadOptions.None);
            if (document.Root is null
                || !document.Root.Name.LocalName.Equals("fetch", StringComparison.OrdinalIgnoreCase))
            {
                return DataverseXrmErrors.InvalidFetchXml("FetchXML must contain a root <fetch> element.");
            }

            return document;
        }
        catch (Exception ex)
        {
            return DataverseXrmErrors.InvalidFetchXml($"FetchXML could not be parsed: {ex.Message}");
        }
    }

    private static ErrorOr<XElement> ResolveEntityElement(XDocument fetchDocument)
    {
        var entityElements = fetchDocument.Root!
            .Elements()
            .Where(element => element.Name.LocalName.Equals("entity", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return entityElements.Length switch
        {
            0 => DataverseXrmErrors.InvalidFetchXml("FetchXML must contain a single <entity> element."),
            > 1 => DataverseXrmErrors.UnsupportedFetchXmlFeature("multiple entity elements"),
            _ => entityElements[0]
        };
    }

    private static ErrorOr<IReadOnlyList<string>> TranslateSelectedColumns(XElement entityElement)
    {
        if (entityElement.Elements().Any(element => element.Name.LocalName.Equals("all-attributes", StringComparison.OrdinalIgnoreCase)))
        {
            return Array.Empty<string>();
        }

        var selectedColumns = new List<string>();
        foreach (var attributeElement in entityElement.Elements().Where(element => element.Name.LocalName.Equals("attribute", StringComparison.OrdinalIgnoreCase)))
        {
            if (attributeElement.Attribute("alias") is not null)
            {
                return DataverseXrmErrors.UnsupportedFetchXmlFeature("attribute alias");
            }

            var logicalName = attributeElement.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                return DataverseXrmErrors.InvalidFetchXml("FetchXML <attribute> elements require a non-empty 'name' attribute.");
            }

            selectedColumns.Add(logicalName);
        }

        return selectedColumns
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ErrorOr<IReadOnlyList<QuerySort>> TranslateOrders(XElement entityElement)
    {
        var orders = new List<QuerySort>();

        foreach (var orderElement in entityElement.Elements().Where(element => element.Name.LocalName.Equals("order", StringComparison.OrdinalIgnoreCase)))
        {
            if (orderElement.Attribute("alias") is not null)
            {
                return DataverseXrmErrors.UnsupportedFetchXmlFeature("order alias");
            }

            if (orderElement.Attribute("entityname") is not null)
            {
                return DataverseXrmErrors.UnsupportedFetchXmlFeature("cross-entity ordering");
            }

            var attributeName = orderElement.Attribute("attribute")?.Value;
            if (string.IsNullOrWhiteSpace(attributeName))
            {
                return DataverseXrmErrors.InvalidFetchXml("FetchXML <order> elements require a non-empty 'attribute' attribute.");
            }

            var descendingResult = TryParseOptionalBoolean(orderElement.Attribute("descending")?.Value);
            if (descendingResult.IsError)
            {
                return descendingResult.Errors;
            }

            var sortResult = QuerySort.Create(
                attributeName,
                descendingResult.Value
                    ? SortDirection.Descending
                    : SortDirection.Ascending);
            if (sortResult.IsError)
            {
                return sortResult.Errors;
            }

            orders.Add(sortResult.Value);
        }

        return orders;
    }

    private static ErrorOr<QueryFilter?> TranslateFilters(
        XElement entityElement,
        TableDefinition table)
    {
        var filterElements = entityElement.Elements()
            .Where(element => element.Name.LocalName.Equals("filter", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (filterElements.Length == 0)
        {
            return (QueryFilter?)null;
        }

        if (filterElements.Length == 1)
        {
            return TranslateFilter(filterElements[0], table);
        }

        var childFilters = new List<QueryFilter>();
        foreach (var filterElement in filterElements)
        {
            var childFilterResult = TranslateFilter(filterElement, table);
            if (childFilterResult.IsError)
            {
                return childFilterResult.Errors;
            }

            if (childFilterResult.Value is not null)
            {
                childFilters.Add(childFilterResult.Value);
            }
        }

        if (childFilters.Count == 0)
        {
            return (QueryFilter?)null;
        }

        var rootFilterResult = QueryFilter.Create(QueryFilterOperator.And, filters: childFilters);
        return rootFilterResult.IsError
            ? rootFilterResult.Errors
            : (QueryFilter?)rootFilterResult.Value;
    }

    private static ErrorOr<QueryFilter?> TranslateFilter(
        XElement filterElement,
        TableDefinition table)
    {
        var filterType = filterElement.Attribute("type")?.Value;
        var filterOperator = string.Equals(filterType, "or", StringComparison.OrdinalIgnoreCase)
            ? QueryFilterOperator.Or
            : QueryFilterOperator.And;

        if (!string.IsNullOrWhiteSpace(filterType)
            && !string.Equals(filterType, "and", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(filterType, "or", StringComparison.OrdinalIgnoreCase))
        {
            return DataverseXrmErrors.InvalidFetchXml($"FetchXML filter type '{filterType}' is not supported.");
        }

        var conditions = new List<QueryCondition>();
        var childFilters = new List<QueryFilter>();

        foreach (var childElement in filterElement.Elements())
        {
            if (childElement.Name.LocalName.Equals("condition", StringComparison.OrdinalIgnoreCase))
            {
                var conditionResult = TranslateCondition(childElement, table);
                if (conditionResult.IsError)
                {
                    return conditionResult.Errors;
                }

                conditions.Add(conditionResult.Value);
                continue;
            }

            if (childElement.Name.LocalName.Equals("filter", StringComparison.OrdinalIgnoreCase))
            {
                var childFilterResult = TranslateFilter(childElement, table);
                if (childFilterResult.IsError)
                {
                    return childFilterResult.Errors;
                }

                if (childFilterResult.Value is not null)
                {
                    childFilters.Add(childFilterResult.Value);
                }

                continue;
            }

            return DataverseXrmErrors.UnsupportedFetchXmlFeature($"filter child '{childElement.Name.LocalName}'");
        }

        if (conditions.Count == 0 && childFilters.Count == 0)
        {
            return (QueryFilter?)null;
        }

        var filterResult = QueryFilter.Create(filterOperator, conditions, childFilters);
        return filterResult.IsError
            ? filterResult.Errors
            : (QueryFilter?)filterResult.Value;
    }

    private static ErrorOr<QueryCondition> TranslateCondition(
        XElement conditionElement,
        TableDefinition table)
    {
        if (conditionElement.Attribute("entityname") is not null)
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("cross-entity conditions");
        }

        if (conditionElement.Attribute("valueof") is not null)
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("valueof conditions");
        }

        var attributeName = conditionElement.Attribute("attribute")?.Value;
        if (string.IsNullOrWhiteSpace(attributeName))
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML <condition> elements require a non-empty 'attribute' attribute.");
        }

        var column = table.FindColumn(attributeName);
        if (column is null)
        {
            return DomainErrors.UnknownColumn(table.LogicalName, attributeName);
        }

        var operatorResult = TranslateConditionOperator(conditionElement.Attribute("operator")?.Value);
        if (operatorResult.IsError)
        {
            return operatorResult.Errors;
        }

        var valuesResult = TranslateConditionValues(conditionElement, column, operatorResult.Value);
        if (valuesResult.IsError)
        {
            return valuesResult.Errors;
        }

        return QueryCondition.Create(attributeName, operatorResult.Value, valuesResult.Value);
    }

    private static ErrorOr<QueryConditionOperator> TranslateConditionOperator(string? @operator)
    {
        if (string.IsNullOrWhiteSpace(@operator))
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML conditions require a non-empty 'operator' attribute.");
        }

        return @operator.ToLowerInvariant() switch
        {
            "eq" => QueryConditionOperator.Equal,
            "ne" => QueryConditionOperator.NotEqual,
            "null" => QueryConditionOperator.Null,
            "not-null" => QueryConditionOperator.NotNull,
            "like" => QueryConditionOperator.Like,
            "begins-with" => QueryConditionOperator.BeginsWith,
            "ends-with" => QueryConditionOperator.EndsWith,
            "gt" => QueryConditionOperator.GreaterThan,
            "ge" => QueryConditionOperator.GreaterThanOrEqual,
            "lt" => QueryConditionOperator.LessThan,
            "le" => QueryConditionOperator.LessThanOrEqual,
            "in" => QueryConditionOperator.In,
            _ => DataverseXrmErrors.UnsupportedFetchXmlFeature($"condition operator '{@operator}'")
        };
    }

    private static ErrorOr<IReadOnlyList<object?>> TranslateConditionValues(
        XElement conditionElement,
        ColumnDefinition column,
        QueryConditionOperator @operator)
    {
        var rawValues = new List<string>();

        var attributeValue = conditionElement.Attribute("value")?.Value;
        if (!string.IsNullOrWhiteSpace(attributeValue))
        {
            rawValues.Add(attributeValue);
        }

        foreach (var valueElement in conditionElement.Elements().Where(element => element.Name.LocalName.Equals("value", StringComparison.OrdinalIgnoreCase)))
        {
            rawValues.Add(valueElement.Value);
        }

        if (@operator == QueryConditionOperator.Null || @operator == QueryConditionOperator.NotNull)
        {
            return Array.Empty<object?>();
        }

        if (@operator != QueryConditionOperator.In && rawValues.Count != 1)
        {
            return DataverseXrmErrors.InvalidFetchXml(
                $"FetchXML condition operator '{@operator.Name}' requires exactly one value.");
        }

        if (@operator == QueryConditionOperator.In && rawValues.Count == 0)
        {
            return DataverseXrmErrors.InvalidFetchXml(
                "FetchXML condition operator 'in' requires one or more <value> elements or a 'value' attribute.");
        }

        var values = new List<object?>(rawValues.Count);
        foreach (var rawValue in rawValues)
        {
            var convertedValueResult = ConvertConditionValue(rawValue, column);
            if (convertedValueResult.IsError)
            {
                return convertedValueResult.Errors;
            }

            values.Add(convertedValueResult.Value);
        }

        return values;
    }

    private static ErrorOr<object?> ConvertConditionValue(
        string rawValue,
        ColumnDefinition column)
    {
        if (column.AttributeType == AttributeType.String)
        {
            return rawValue;
        }

        if (column.AttributeType == AttributeType.UniqueIdentifier || column.AttributeType == AttributeType.Lookup)
        {
            return Guid.TryParse(rawValue, out var guid)
                ? guid
                : DataverseXrmErrors.InvalidFetchXml(
                    $"FetchXML value '{rawValue}' is not a valid GUID for attribute '{column.LogicalName}'.");
        }

        if (column.AttributeType == AttributeType.Boolean)
        {
            return rawValue switch
            {
                "1" => true,
                "0" => false,
                _ when bool.TryParse(rawValue, out var boolean) => boolean,
                _ => DataverseXrmErrors.InvalidFetchXml(
                    $"FetchXML value '{rawValue}' is not a valid boolean for attribute '{column.LogicalName}'.")
            };
        }

        if (column.AttributeType == AttributeType.Integer)
        {
            return int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)
                ? integer
                : DataverseXrmErrors.InvalidFetchXml(
                    $"FetchXML value '{rawValue}' is not a valid integer for attribute '{column.LogicalName}'.");
        }

        if (column.AttributeType == AttributeType.Decimal)
        {
            return decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue)
                ? decimalValue
                : DataverseXrmErrors.InvalidFetchXml(
                    $"FetchXML value '{rawValue}' is not a valid decimal for attribute '{column.LogicalName}'.");
        }

        if (column.AttributeType == AttributeType.DateTime)
        {
            return DateTimeOffset.TryParse(
                    rawValue,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var dateTimeOffset)
                ? dateTimeOffset
                : DataverseXrmErrors.InvalidFetchXml(
                    $"FetchXML value '{rawValue}' is not a valid datetime for attribute '{column.LogicalName}'.");
        }

        return DataverseXrmErrors.InvalidFetchXml(
            $"FetchXML attribute type '{column.AttributeType.Name}' is not supported for attribute '{column.LogicalName}'.");
    }

    private static ErrorOr<int?> TranslateTop(XElement fetchElement)
    {
        var topValue = fetchElement.Attribute("top")?.Value;
        if (string.IsNullOrWhiteSpace(topValue))
        {
            return (int?)null;
        }

        if (!int.TryParse(topValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var top) || top <= 0)
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML 'top' must be a positive integer.");
        }

        return top;
    }

    private static ErrorOr<DataverseTranslatedFetchPage> TranslatePage(
        XElement fetchElement,
        int? top)
    {
        var countValue = fetchElement.Attribute("count")?.Value;
        var pageValue = fetchElement.Attribute("page")?.Value;
        var pagingCookie = fetchElement.Attribute("paging-cookie")?.Value;

        if (string.IsNullOrWhiteSpace(countValue))
        {
            if (!string.IsNullOrWhiteSpace(pageValue) || !string.IsNullOrWhiteSpace(pagingCookie))
            {
                return DataverseXrmErrors.InvalidFetchXml(
                    "FetchXML paging requires a positive 'count' value.");
            }

            return new DataverseTranslatedFetchPage(null, CurrentPageNumber: 1);
        }

        if (top is not null)
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("combining 'top' with paging");
        }

        if (!int.TryParse(countValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) || count <= 0)
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML 'count' must be a positive integer.");
        }

        var currentPageNumber = 1;
        if (!string.IsNullOrWhiteSpace(pageValue)
            && (!int.TryParse(pageValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out currentPageNumber)
                || currentPageNumber <= 0))
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML 'page' must be a positive integer.");
        }

        var continuationToken = DataverseXrmPagingCookie.ExtractContinuationToken(pagingCookie);
        if (string.IsNullOrWhiteSpace(continuationToken) && currentPageNumber > 1)
        {
            var offset = checked((currentPageNumber - 1) * count);
            continuationToken = offset.ToString(CultureInfo.InvariantCulture);
        }

        var pageRequestResult = PageRequest.Create(count, continuationToken);
        if (pageRequestResult.IsError)
        {
            return pageRequestResult.Errors;
        }

        return new DataverseTranslatedFetchPage(pageRequestResult.Value, currentPageNumber);
    }

    private static ErrorOr<bool> TryParseOptionalBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value switch
        {
            "1" => true,
            "0" => false,
            _ when bool.TryParse(value, out var boolean) => boolean,
            _ => DataverseXrmErrors.InvalidFetchXml($"FetchXML boolean value '{value}' is not valid.")
        };
    }

    private static bool IsTruthy(string? value)
        => value switch
        {
            null => false,
            "1" => true,
            _ => bool.TryParse(value, out var boolean) && boolean
        };

    private static ErrorOr<Success> BuildLinkedJoins(
        XElement linkEntityElement,
        string parentScopeName,
        string parentTableLogicalName,
        ICollection<RawLinkedFetchJoin> joins,
        IReadOnlyDictionary<string, TableDefinition> linkedTables)
    {
        var joinResult = BuildLinkedJoin(linkEntityElement, parentScopeName, parentTableLogicalName, linkedTables);
        if (joinResult.IsError)
        {
            return joinResult.Errors;
        }

        joins.Add(joinResult.Value);

        foreach (var childLinkEntityElement in linkEntityElement.Elements()
                     .Where(element => element.Name.LocalName.Equals("link-entity", StringComparison.OrdinalIgnoreCase)))
        {
            var childJoinResult = BuildLinkedJoins(
                childLinkEntityElement,
                joinResult.Value.Alias,
                joinResult.Value.TableLogicalName,
                joins,
                linkedTables);
            if (childJoinResult.IsError)
            {
                return childJoinResult.Errors;
            }
        }

        return Result.Success;
    }

    private static ErrorOr<RawLinkedFetchJoin> BuildLinkedJoin(
        XElement linkEntityElement,
        string parentScopeName,
        string parentTableLogicalName,
        IReadOnlyDictionary<string, TableDefinition> linkedTables)
    {
        var tableLogicalName = linkEntityElement.Attribute("name")?.Value;
        if (string.IsNullOrWhiteSpace(tableLogicalName))
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML <link-entity> elements require a non-empty 'name' attribute.");
        }

        var toAttributeName = linkEntityElement.Attribute("from")?.Value;
        if (string.IsNullOrWhiteSpace(toAttributeName))
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML <link-entity> elements require a non-empty 'from' attribute.");
        }

        var fromAttributeName = linkEntityElement.Attribute("to")?.Value;
        if (string.IsNullOrWhiteSpace(fromAttributeName))
        {
            return DataverseXrmErrors.InvalidFetchXml("FetchXML <link-entity> elements require a non-empty 'to' attribute.");
        }

        var joinTypeResult = TranslateFetchJoinType(linkEntityElement.Attribute("link-type")?.Value);
        if (joinTypeResult.IsError)
        {
            return joinTypeResult.Errors;
        }

        var alias = linkEntityElement.Attribute("alias")?.Value;
        if (string.IsNullOrWhiteSpace(alias))
        {
            alias = tableLogicalName;
        }

        var selectedColumnsResult = TranslateSelectedColumns(linkEntityElement);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        if (linkEntityElement.Attribute("intersect") is not null)
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("intersect");
        }

        if (linkEntityElement.Attribute("entityname") is not null)
        {
            return DataverseXrmErrors.UnsupportedFetchXmlFeature("link-entity entityname");
        }

        if (!linkedTables.TryGetValue(tableLogicalName, out var linkedTable))
        {
            return DataverseXrmErrors.InvalidFetchXml($"No table definition was provided for link-entity '{tableLogicalName}'.");
        }

        var filterResult = TranslateFilters(linkEntityElement, linkedTable);
        if (filterResult.IsError)
        {
            return filterResult.Errors;
        }

        var ordersResult = TranslateOrders(linkEntityElement);
        if (ordersResult.IsError)
        {
            return ordersResult.Errors;
        }

        return new RawLinkedFetchJoin(
            TableLogicalName: tableLogicalName,
            Alias: alias,
            ParentScopeName: parentScopeName,
            ParentTableLogicalName: parentTableLogicalName,
            FromAttributeName: fromAttributeName,
            ToAttributeName: toAttributeName,
            JoinType: joinTypeResult.Value,
            SelectedColumns: selectedColumnsResult.Value,
            ReturnAllColumns: linkEntityElement.Elements().Any(element => element.Name.LocalName.Equals("all-attributes", StringComparison.OrdinalIgnoreCase)),
            Filter: filterResult.Value,
            Orders: ordersResult.Value);
    }

    private static ErrorOr<LinkedRecordJoinType> TranslateFetchJoinType(string? linkType)
    {
        if (string.IsNullOrWhiteSpace(linkType) || string.Equals(linkType, "inner", StringComparison.OrdinalIgnoreCase))
        {
            return LinkedRecordJoinType.Inner;
        }

        if (string.Equals(linkType, "outer", StringComparison.OrdinalIgnoreCase))
        {
            return LinkedRecordJoinType.LeftOuter;
        }

        return DataverseXrmErrors.UnsupportedFetchXmlFeature($"link-type '{linkType}'");
    }

    private static LinkedRecordFilter? ToLinkedFilter(
        QueryFilter? filter,
        string rootScopeName)
    {
        if (filter is null)
        {
            return null;
        }

        return new LinkedRecordFilter(
            filter.Operator,
            filter.Conditions
                .Select(condition => new LinkedRecordCondition(
                    rootScopeName,
                    condition.ColumnLogicalName,
                    condition.Operator,
                    condition.Values))
                .ToArray(),
            filter.Filters
                .Select(childFilter => ToLinkedFilter(childFilter, rootScopeName)!)
                .ToArray());
    }

    private static IReadOnlyList<LinkedRecordSort> ToLinkedSorts(
        IReadOnlyList<QuerySort> sorts,
        string rootScopeName)
        => sorts
            .Select(sort => new LinkedRecordSort(rootScopeName, sort.ColumnLogicalName, sort.Direction))
            .ToArray();

    private static void CollectLinkEntityNames(XElement element, ICollection<string> names)
    {
        foreach (var linkEntityElement in element.Elements()
                     .Where(e => e.Name.LocalName.Equals("link-entity", StringComparison.OrdinalIgnoreCase)))
        {
            var name = linkEntityElement.Attribute("name")?.Value;
            if (!string.IsNullOrWhiteSpace(name))
            {
                names.Add(name);
            }

            CollectLinkEntityNames(linkEntityElement, names);
        }
    }
}

internal sealed record DataverseTranslatedFetchQuery(
    RecordQuery Query,
    int CurrentPageNumber);

internal sealed record DataverseTranslatedLinkedFetchQuery(
    LinkedRecordQuery Query,
    int CurrentPageNumber);

internal sealed record DataverseTranslatedFetchPage(
    PageRequest? PageRequest,
    int CurrentPageNumber);

internal sealed record RawLinkedFetchJoin(
    string TableLogicalName,
    string Alias,
    string ParentScopeName,
    string ParentTableLogicalName,
    string FromAttributeName,
    string ToAttributeName,
    LinkedRecordJoinType JoinType,
    IReadOnlyList<string> SelectedColumns,
    bool ReturnAllColumns,
    QueryFilter? Filter,
    IReadOnlyList<QuerySort> Orders);
