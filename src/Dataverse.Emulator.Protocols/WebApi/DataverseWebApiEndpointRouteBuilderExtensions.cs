using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Application.Records;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Records;
using Dataverse.Emulator.Protocols.Common;
using ErrorOr;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Dataverse.Emulator.Protocols.WebApi;

public static partial class DataverseWebApiEndpointRouteBuilderExtensions
{
    private const string RoutePrefix = "/api/data/v9.2";

    public static IEndpointRouteBuilder MapDataverseWebApi(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup(RoutePrefix);

        group.MapGet(string.Empty, GetServiceDocumentAsync);
        group.MapGet("/$metadata", GetMetadataDocumentAsync);
        group.MapGet("/{entitySet}", ListRowsAsync);
        group.MapGet("/{entitySet}({id:guid})", GetRowByIdAsync);
        group.MapPost("/{entitySet}", CreateRowAsync);
        group.MapPatch("/{entitySet}({id:guid})", UpdateRowAsync);
        group.MapDelete("/{entitySet}({id:guid})", DeleteRowAsync);

        return endpoints;
    }

    private static async Task<IResult> GetServiceDocumentAsync(
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var tables = await mediator.Send(new ListTableDefinitionsQuery(), cancellationToken);
        var payload = new Dictionary<string, object?>
        {
            ["@odata.context"] = BuildMetadataDocumentUri(request),
            ["value"] = tables
                .Select(table => new Dictionary<string, object?>
                {
                    ["name"] = table.EntitySetName,
                    ["kind"] = "EntitySet",
                    ["url"] = table.EntitySetName
                })
                .ToArray()
        };

        return CreateJsonResult(payload);
    }

    private static async Task<IResult> GetMetadataDocumentAsync(
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var tables = await mediator.Send(new ListTableDefinitionsQuery(), cancellationToken);
        var document = BuildMetadataDocument(tables);
        return TypedResults.Content(document, "application/xml");
    }

    private static async Task<IResult> ListRowsAsync(
        string entitySet,
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var tableResult = await GetTableAsync(mediator, entitySet, cancellationToken);
        if (tableResult.IsError)
        {
            return ToErrorResult(tableResult.Errors);
        }

        var queryResult = DataverseWebApiQueryParser.ParseListQuery(request, tableResult.Value);
        if (queryResult.IsError)
        {
            return ToErrorResult(queryResult.Errors);
        }

        var rowsResult = await mediator.Send(new ListRowsQuery(queryResult.Value), cancellationToken);
        if (rowsResult.IsError)
        {
            return ToErrorResult(rowsResult.Errors);
        }

        var payload = new Dictionary<string, object?>
        {
            ["@odata.context"] = $"{BuildMetadataDocumentUri(request)}#{tableResult.Value.EntitySetName}",
            ["value"] = rowsResult.Value.Items
                .Select(record => ToEntityPayload(tableResult.Value, record))
                .ToArray()
        };

        var pageSize = DataverseWebApiQueryParser.ResolveRequestedPageSize(request);
        if (rowsResult.Value.ContinuationToken is { } continuationToken && pageSize is int resolvedPageSize)
        {
            payload["@odata.nextLink"] = BuildNextLink(request, entitySet, continuationToken, resolvedPageSize);
        }

        return CreateJsonResult(payload);
    }

    private static async Task<IResult> GetRowByIdAsync(
        string entitySet,
        Guid id,
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var tableResult = await GetTableAsync(mediator, entitySet, cancellationToken);
        if (tableResult.IsError)
        {
            return ToErrorResult(tableResult.Errors);
        }

        var selectedColumnsResult = DataverseWebApiQueryParser.ParseSelectedColumns(request, tableResult.Value);
        if (selectedColumnsResult.IsError)
        {
            return ToErrorResult(selectedColumnsResult.Errors);
        }

        var rowResult = await mediator.Send(new GetRowByIdQuery(tableResult.Value.LogicalName, id), cancellationToken);
        if (rowResult.IsError)
        {
            return ToErrorResult(rowResult.Errors);
        }

        var projected = rowResult.Value.Project(selectedColumnsResult.Value);
        var payload = ToEntityPayload(tableResult.Value, projected);
        payload["@odata.context"] = $"{BuildMetadataDocumentUri(request)}#{tableResult.Value.EntitySetName}/$entity";

        return CreateJsonResult(payload);
    }

    private static async Task<IResult> CreateRowAsync(
        string entitySet,
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var tableResult = await GetTableAsync(mediator, entitySet, cancellationToken);
        if (tableResult.IsError)
        {
            return ToErrorResult(tableResult.Errors);
        }

        var valuesResult = await DataverseWebApiPayloadReader.ReadValuesAsync(request, tableResult.Value, cancellationToken);
        if (valuesResult.IsError)
        {
            return ToErrorResult(valuesResult.Errors);
        }

        var createResult = await mediator.Send(new CreateRowCommand(tableResult.Value.LogicalName, valuesResult.Value), cancellationToken);
        if (createResult.IsError)
        {
            return ToErrorResult(createResult.Errors);
        }

        var entityUri = BuildEntityUri(request, entitySet, createResult.Value);
        request.HttpContext.Response.Headers.Location = entityUri;
        request.HttpContext.Response.Headers.Append("OData-EntityId", entityUri);

        if (DataverseWebApiRequestPreferences.ReturnRepresentation(request))
        {
            var rowResult = await mediator.Send(new GetRowByIdQuery(tableResult.Value.LogicalName, createResult.Value), cancellationToken);
            if (rowResult.IsError)
            {
                return ToErrorResult(rowResult.Errors);
            }

            var payload = ToEntityPayload(tableResult.Value, rowResult.Value);
            payload["@odata.context"] = $"{BuildMetadataDocumentUri(request)}#{tableResult.Value.EntitySetName}/$entity";
            return CreateJsonResult(payload, StatusCodes.Status201Created);
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> UpdateRowAsync(
        string entitySet,
        Guid id,
        HttpRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var tableResult = await GetTableAsync(mediator, entitySet, cancellationToken);
        if (tableResult.IsError)
        {
            return ToErrorResult(tableResult.Errors);
        }

        var valuesResult = await DataverseWebApiPayloadReader.ReadValuesAsync(request, tableResult.Value, cancellationToken);
        if (valuesResult.IsError)
        {
            return ToErrorResult(valuesResult.Errors);
        }

        var updateResult = await mediator.Send(new UpdateRowCommand(tableResult.Value.LogicalName, id, valuesResult.Value), cancellationToken);
        if (updateResult.IsError)
        {
            return ToErrorResult(updateResult.Errors);
        }

        if (DataverseWebApiRequestPreferences.ReturnRepresentation(request))
        {
            var payload = ToEntityPayload(tableResult.Value, updateResult.Value);
            payload["@odata.context"] = $"{BuildMetadataDocumentUri(request)}#{tableResult.Value.EntitySetName}/$entity";
            return CreateJsonResult(payload);
        }

        return TypedResults.NoContent();
    }

    private static async Task<IResult> DeleteRowAsync(
        string entitySet,
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var tableResult = await GetTableAsync(mediator, entitySet, cancellationToken);
        if (tableResult.IsError)
        {
            return ToErrorResult(tableResult.Errors);
        }

        var deleteResult = await mediator.Send(new DeleteRowCommand(tableResult.Value.LogicalName, id), cancellationToken);
        return deleteResult.IsError
            ? ToErrorResult(deleteResult.Errors)
            : TypedResults.NoContent();
    }

    private static async Task<ErrorOr<TableDefinition>> GetTableAsync(
        IMediator mediator,
        string entitySet,
        CancellationToken cancellationToken)
    {
        return await mediator.Send(new GetTableDefinitionByEntitySetNameQuery(entitySet), cancellationToken);
    }

    private static IResult CreateJsonResult(object payload, int statusCode = StatusCodes.Status200OK)
        => Results.Json(payload, statusCode: statusCode, contentType: "application/json");

    private static IResult ToErrorResult(IReadOnlyList<Error> errors)
    {
        return Results.Json(
            DataverseProtocolErrorMapper.ToWebApiPayload(errors),
            statusCode: DataverseProtocolErrorMapper.MapHttpStatusCode(errors[0].Type),
            contentType: "application/json");
    }

    private static Dictionary<string, object?> ToEntityPayload(TableDefinition table, EntityRecord record)
    {
        var payload = new Dictionary<string, object?>
        {
            ["@odata.etag"] = $"W/\"{record.Version}\""
        };

        foreach (var pair in record.Values.Items)
        {
            payload[pair.Key] = pair.Value;
        }

        if (!payload.ContainsKey(table.PrimaryIdAttribute))
        {
            payload[table.PrimaryIdAttribute] = record.Id;
        }

        return payload;
    }

    private static string BuildMetadataDocumentUri(HttpRequest request)
        => $"{BuildServiceRootUri(request)}/$metadata";

    private static string BuildEntityUri(HttpRequest request, string entitySet, Guid id)
        => $"{BuildServiceRootUri(request)}/{entitySet}({id})";

    private static string BuildServiceRootUri(HttpRequest request)
        => $"{request.Scheme}://{request.Host}{request.PathBase}{RoutePrefix}";

    private static string BuildNextLink(HttpRequest request, string entitySet, string continuationToken, int pageSize)
    {
        var queryParameters = request.Query.ToDictionary(
            pair => pair.Key,
            pair => (string?)pair.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        queryParameters["$skiptoken"] = $"{continuationToken}|{pageSize}";

        return QueryHelpers.AddQueryString($"{BuildServiceRootUri(request)}/{entitySet}", queryParameters);
    }

    private static string BuildMetadataDocument(IReadOnlyList<TableDefinition> tables)
    {
        XNamespace edmx = "http://docs.oasis-open.org/odata/ns/edmx";
        XNamespace edm = "http://docs.oasis-open.org/odata/ns/edm";

        var schema = new XElement(
            edm + "Schema",
            new XAttribute("Namespace", "Microsoft.Dynamics.CRM"),
            tables.Select(BuildEntityType),
            new XElement(
                edm + "EntityContainer",
                new XAttribute("Name", "DefaultContainer"),
                tables.Select(table => new XElement(
                    edm + "EntitySet",
                    new XAttribute("Name", table.EntitySetName),
                    new XAttribute("EntityType", $"Microsoft.Dynamics.CRM.{table.LogicalName}")))));

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(
                edmx + "Edmx",
                new XAttribute(XNamespace.Xmlns + "edmx", edmx),
                new XAttribute("Version", "4.0"),
                new XElement(
                    edmx + "DataServices",
                    schema)));

        return document.ToString();
    }

    private static XElement BuildEntityType(TableDefinition table)
    {
        XNamespace edm = "http://docs.oasis-open.org/odata/ns/edm";

        return new XElement(
            edm + "EntityType",
            new XAttribute("Name", table.LogicalName),
            new XElement(
                edm + "Key",
                new XElement(edm + "PropertyRef", new XAttribute("Name", table.PrimaryIdAttribute))),
            table.Columns
                .OrderByDescending(column => column.LogicalName.Equals(table.PrimaryIdAttribute, StringComparison.OrdinalIgnoreCase))
                .ThenBy(column => column.LogicalName, StringComparer.OrdinalIgnoreCase)
                .Select(column => new XElement(
                    edm + "Property",
                    new XAttribute("Name", column.LogicalName),
                    new XAttribute("Type", ToEdmType(column.AttributeType)),
                    new XAttribute("Nullable", IsNullable(column).ToString().ToLowerInvariant()))));
    }

    private static string ToEdmType(AttributeType attributeType)
    {
        if (attributeType == AttributeType.UniqueIdentifier || attributeType == AttributeType.Lookup)
        {
            return "Edm.Guid";
        }

        if (attributeType == AttributeType.String)
        {
            return "Edm.String";
        }

        if (attributeType == AttributeType.Integer)
        {
            return "Edm.Int32";
        }

        if (attributeType == AttributeType.Decimal)
        {
            return "Edm.Decimal";
        }

        if (attributeType == AttributeType.Boolean)
        {
            return "Edm.Boolean";
        }

        if (attributeType == AttributeType.DateTime)
        {
            return "Edm.DateTimeOffset";
        }

        return "Edm.String";
    }

    private static bool IsNullable(ColumnDefinition column)
        => column.RequiredLevel == RequiredLevel.None && !column.IsPrimaryId;
}

internal static partial class DataverseWebApiQueryParser
{
    public static int? ResolveRequestedPageSize(HttpRequest request)
    {
        var explicitPageSize = DataverseWebApiRequestPreferences.TryGetMaxPageSize(request);
        if (explicitPageSize is not null)
        {
            return explicitPageSize;
        }

        var skipToken = request.Query["$skiptoken"].ToString();
        if (string.IsNullOrWhiteSpace(skipToken))
        {
            return null;
        }

        var tokenParts = skipToken.Split('|', 2, StringSplitOptions.TrimEntries);
        if (tokenParts.Length == 2 && int.TryParse(tokenParts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var pageSize) && pageSize > 0)
        {
            return pageSize;
        }

        return null;
    }

    public static ErrorOr<RecordQuery> ParseListQuery(HttpRequest request, TableDefinition table)
    {
        var selectedColumnsResult = ParseSelectedColumns(request, table);
        if (selectedColumnsResult.IsError)
        {
            return selectedColumnsResult.Errors;
        }

        var conditionsResult = ParseConditions(request, table);
        if (conditionsResult.IsError)
        {
            return conditionsResult.Errors;
        }

        var sortsResult = ParseSorts(request, table);
        if (sortsResult.IsError)
        {
            return sortsResult.Errors;
        }

        int? top = null;
        if (request.Query.TryGetValue("$top", out var topValues) && !StringValues.IsNullOrEmpty(topValues))
        {
            if (!int.TryParse(topValues.ToString(), NumberStyles.None, CultureInfo.InvariantCulture, out var parsedTop) || parsedTop <= 0)
            {
                return Error.Validation("Query.Top.Invalid", "Top must be greater than zero when provided.");
            }

            top = parsedTop;
        }

        PageRequest? page = null;
        var pageSize = ResolveRequestedPageSize(request);
        if (pageSize is int resolvedPageSize)
        {
            var continuationToken = NormalizeContinuationToken(request.Query["$skiptoken"].ToString());
            var pageRequestResult = PageRequest.Create(resolvedPageSize, continuationToken);
            if (pageRequestResult.IsError)
            {
                return pageRequestResult.Errors;
            }

            page = pageRequestResult.Value;
        }
        else if (!string.IsNullOrWhiteSpace(request.Query["$skiptoken"].ToString()))
        {
            return Error.Validation(
                "Query.Page.SkipTokenInvalid",
                "Skip tokens must include a page size generated by the emulator.");
        }

        return RecordQuery.Create(
            table.LogicalName,
            EnsurePrimaryIdIncluded(selectedColumnsResult.Value, table.PrimaryIdAttribute),
            conditionsResult.Value,
            sortsResult.Value,
            top,
            page);
    }

    public static ErrorOr<IReadOnlyList<string>> ParseSelectedColumns(HttpRequest request, TableDefinition table)
    {
        if (!request.Query.TryGetValue("$select", out var selectValues) || StringValues.IsNullOrEmpty(selectValues))
        {
            return Array.Empty<string>();
        }

        var selectedColumns = selectValues
            .ToString()
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var invalidColumns = selectedColumns
            .Where(column => !table.HasColumn(column))
            .ToArray();

        if (invalidColumns.Length > 0)
        {
            return invalidColumns
                .Select(column => Error.Validation(
                    "Metadata.Column.Unknown",
                    $"Column '{column}' does not exist on table '{table.LogicalName}'."))
                .ToList();
        }

        return selectedColumns;
    }

    private static ErrorOr<IReadOnlyList<QueryCondition>> ParseConditions(HttpRequest request, TableDefinition table)
    {
        if (!request.Query.TryGetValue("$filter", out var filterValues) || StringValues.IsNullOrEmpty(filterValues))
        {
            return Array.Empty<QueryCondition>();
        }

        var filter = filterValues.ToString();
        var match = FilterPattern().Match(filter);
        if (!match.Success)
        {
            return Error.Validation(
                "Query.Filter.Unsupported",
                "Only simple '<column> eq <value>' filters are currently supported.");
        }

        var columnName = match.Groups["column"].Value;
        var column = table.FindColumn(columnName);
        if (column is null)
        {
            return Error.Validation(
                "Metadata.Column.Unknown",
                $"Column '{columnName}' does not exist on table '{table.LogicalName}'.");
        }

        var valueResult = DataverseWebApiLiteralParser.Parse(match.Groups["value"].Value, column);
        if (valueResult.IsError)
        {
            return valueResult.Errors;
        }

        var conditionResult = QueryCondition.Create(column.LogicalName, ConditionOperator.Equal, valueResult.Value);
        return conditionResult.IsError
            ? conditionResult.Errors
            : new[] { conditionResult.Value };
    }

    private static ErrorOr<IReadOnlyList<QuerySort>> ParseSorts(HttpRequest request, TableDefinition table)
    {
        if (!request.Query.TryGetValue("$orderby", out var orderByValues) || StringValues.IsNullOrEmpty(orderByValues))
        {
            return Array.Empty<QuerySort>();
        }

        var sorts = new List<QuerySort>();
        var errors = new List<Error>();

        foreach (var item in orderByValues.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var columnName = parts[0];

            if (!table.HasColumn(columnName))
            {
                errors.Add(Error.Validation(
                    "Metadata.Column.Unknown",
                    $"Column '{columnName}' does not exist on table '{table.LogicalName}'."));
                continue;
            }

            var direction = SortDirection.Ascending;
            if (parts.Length > 1)
            {
                if (parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
                {
                    direction = SortDirection.Descending;
                }
                else if (!parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(Error.Validation(
                        "Query.OrderBy.Unsupported",
                        $"Sort direction '{parts[1]}' is not supported."));
                    continue;
                }
            }

            var sortResult = QuerySort.Create(columnName, direction);
            if (sortResult.IsError)
            {
                errors.AddRange(sortResult.Errors);
                continue;
            }

            sorts.Add(sortResult.Value);
        }

        return errors.Count > 0
            ? errors
            : sorts.ToArray();
    }

    private static IReadOnlyList<string> EnsurePrimaryIdIncluded(IReadOnlyList<string> selectedColumns, string primaryIdAttribute)
    {
        if (selectedColumns.Count == 0 || selectedColumns.Contains(primaryIdAttribute, StringComparer.OrdinalIgnoreCase))
        {
            return selectedColumns;
        }

        return selectedColumns
            .Concat([primaryIdAttribute])
            .ToArray();
    }

    private static string? NormalizeContinuationToken(string? skipToken)
    {
        if (string.IsNullOrWhiteSpace(skipToken))
        {
            return null;
        }

        return skipToken.Split('|', 2, StringSplitOptions.TrimEntries)[0];
    }

    [GeneratedRegex(@"^\s*(?<column>[A-Za-z_][A-Za-z0-9_]*)\s+eq\s+(?<value>.+?)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FilterPattern();
}

internal static class DataverseWebApiRequestPreferences
{
    public static bool ReturnRepresentation(HttpRequest request)
        => HeaderContainsToken(request, "return=representation");

    public static int? TryGetMaxPageSize(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Prefer", out var preferValues))
        {
            return null;
        }

        foreach (var preferValue in SplitPreferenceValues(preferValues))
        {
            var parts = preferValue.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length == 2
                && parts[0].Equals("odata.maxpagesize", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var pageSize)
                && pageSize > 0)
            {
                return pageSize;
            }
        }

        return null;
    }

    private static bool HeaderContainsToken(HttpRequest request, string token)
    {
        if (!request.Headers.TryGetValue("Prefer", out var preferValues))
        {
            return false;
        }

        return SplitPreferenceValues(preferValues)
            .Any(value => value.Equals(token, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> SplitPreferenceValues(StringValues preferValues)
    {
        foreach (var value in preferValues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var token in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                yield return token;
            }
        }
    }
}

internal static class DataverseWebApiPayloadReader
{
    public static async Task<ErrorOr<IReadOnlyDictionary<string, object?>>> ReadValuesAsync(
        HttpRequest request,
        TableDefinition table,
        CancellationToken cancellationToken)
    {
        var payload = await request.ReadFromJsonAsync<Dictionary<string, JsonElement>>(cancellationToken: cancellationToken)
            ?? [];

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<Error>();

        foreach (var pair in payload)
        {
            if (pair.Key.StartsWith('@'))
            {
                continue;
            }

            var column = table.FindColumn(pair.Key);
            var convertedResult = column is not null
                ? ConvertTypedValue(pair.Key, pair.Value, column)
                : ConvertUntypedValue(pair.Key, pair.Value);

            if (convertedResult.IsError)
            {
                errors.AddRange(convertedResult.Errors);
                continue;
            }

            values[pair.Key] = convertedResult.Value;
        }

        return errors.Count > 0
            ? errors
            : values;
    }

    private static ErrorOr<object?> ConvertTypedValue(string propertyName, JsonElement value, ColumnDefinition column)
    {
        if (value.ValueKind == JsonValueKind.Null)
        {
            return (object?)null;
        }

        if (column.AttributeType == AttributeType.String && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        if ((column.AttributeType == AttributeType.UniqueIdentifier || column.AttributeType == AttributeType.Lookup)
            && value.ValueKind == JsonValueKind.String
            && Guid.TryParse(value.GetString(), out var guid))
        {
            return guid;
        }

        if (column.AttributeType == AttributeType.Integer && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var integer))
        {
            return integer;
        }

        if (column.AttributeType == AttributeType.Decimal && value.ValueKind == JsonValueKind.Number)
        {
            if (value.TryGetDecimal(out var decimalValue))
            {
                return decimalValue;
            }

            if (value.TryGetDouble(out var doubleValue))
            {
                return doubleValue;
            }
        }

        if (column.AttributeType == AttributeType.Boolean && (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False))
        {
            return value.GetBoolean();
        }

        if (column.AttributeType == AttributeType.DateTime
            && value.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffset))
        {
            return dateTimeOffset;
        }

        return Error.Validation(
            "Protocol.Payload.UnsupportedValue",
            $"Property '{propertyName}' could not be converted to '{column.AttributeType.Name}'.");
    }

    private static ErrorOr<object?> ConvertUntypedValue(string propertyName, JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Null => (object?)null,
            JsonValueKind.String => value.GetString(),
            JsonValueKind.True or JsonValueKind.False => value.GetBoolean(),
            JsonValueKind.Number when value.TryGetInt32(out var integer) => integer,
            JsonValueKind.Number when value.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number when value.TryGetDouble(out var doubleValue) => doubleValue,
            _ => Error.Validation(
                "Protocol.Payload.UnsupportedValue",
                $"Property '{propertyName}' uses a JSON shape the emulator does not support yet.")
        };
    }
}

internal static class DataverseWebApiLiteralParser
{
    public static ErrorOr<object?> Parse(string literal, ColumnDefinition column)
    {
        var trimmed = literal.Trim();

        if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return (object?)null;
        }

        if (column.AttributeType == AttributeType.String)
        {
            if (trimmed.Length >= 2 && trimmed.StartsWith('\'') && trimmed.EndsWith('\''))
            {
                return trimmed[1..^1].Replace("''", "'", StringComparison.Ordinal);
            }

            return Error.Validation(
                "Query.Filter.UnsupportedLiteral",
                $"Column '{column.LogicalName}' requires a quoted string literal.");
        }

        if (column.AttributeType == AttributeType.UniqueIdentifier || column.AttributeType == AttributeType.Lookup)
        {
            var candidate = trimmed;
            if (candidate.StartsWith("guid'", StringComparison.OrdinalIgnoreCase) && candidate.EndsWith('\''))
            {
                candidate = candidate[5..^1];
            }
            else
            {
                candidate = candidate.Trim('\'');
            }

            if (Guid.TryParse(candidate, out var guid))
            {
                return guid;
            }
        }

        if (column.AttributeType == AttributeType.Integer && int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer))
        {
            return integer;
        }

        if (column.AttributeType == AttributeType.Decimal && decimal.TryParse(trimmed, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
        {
            return decimalValue;
        }

        if (column.AttributeType == AttributeType.Boolean && bool.TryParse(trimmed, out var booleanValue))
        {
            return booleanValue;
        }

        if (column.AttributeType == AttributeType.DateTime)
        {
            var candidate = trimmed.Trim('\'');
            if (DateTimeOffset.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTimeOffset))
            {
                return dateTimeOffset;
            }
        }

        return Error.Validation(
            "Query.Filter.UnsupportedLiteral",
            $"Literal '{literal}' is not supported for column '{column.LogicalName}'.");
    }
}
