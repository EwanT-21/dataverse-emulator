using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Application.Records;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Protocols.Xrm.Queries;
using ErrorOr;
using Mediator;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm.Operations;

public sealed class DataverseXrmRecordOperations(
    IMediator mediator)
{
    public async Task<ErrorOr<Guid>> CreateAsync(Entity entity, CancellationToken cancellationToken)
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

    public async Task<ErrorOr<Entity>> RetrieveAsync(
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

    public async Task<ErrorOr<Success>> UpdateAsync(Entity entity, CancellationToken cancellationToken)
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

    public async Task<ErrorOr<Success>> DeleteAsync(
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

    public async Task<ErrorOr<EntityCollection>> RetrieveMultipleAsync(
        QueryBase query,
        CancellationToken cancellationToken)
    {
        if (query is null)
        {
            return DataverseXrmErrors.ParameterRequired("query");
        }

        return query switch
        {
            QueryExpression queryExpression => await RetrieveMultipleQueryExpressionAsync(queryExpression, cancellationToken),
            FetchExpression fetchExpression => await RetrieveMultipleFetchExpressionAsync(fetchExpression, cancellationToken),
            _ => DataverseXrmErrors.UnsupportedQueryType(query.GetType().Name)
        };
    }

    private async Task<ErrorOr<EntityCollection>> RetrieveMultipleQueryExpressionAsync(
        QueryExpression queryExpression,
        CancellationToken cancellationToken)
    {
        if (queryExpression.LinkEntities.Count > 0)
        {
            var linkedQueryResult = DataverseXrmLinkedQueryTranslator.Translate(queryExpression);
            if (linkedQueryResult.IsError)
            {
                return linkedQueryResult.Errors;
            }

            var rootTableResult = await mediator.Send(
                new GetTableDefinitionQuery(linkedQueryResult.Value.RootTableLogicalName),
                cancellationToken);
            if (rootTableResult.IsError)
            {
                return rootTableResult.Errors;
            }

            var rowsResult = await mediator.Send(
                new ListLinkedRowsQuery(linkedQueryResult.Value),
                cancellationToken);
            if (rowsResult.IsError)
            {
                return rowsResult.Errors;
            }

            var linkedTablesResult = await ResolveLinkedTablesByAliasAsync(linkedQueryResult.Value, cancellationToken);
            if (linkedTablesResult.IsError)
            {
                return linkedTablesResult.Errors;
            }

            return DataverseXrmEntityMapper.ToEntityCollection(
                rootTableResult.Value,
                linkedTablesResult.Value,
                linkedQueryResult.Value,
                rowsResult.Value,
                linkedQueryResult.Value.CurrentPageNumber);
        }

        var queryResult = DataverseXrmQueryExpressionTranslator.Translate(queryExpression);
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

        var currentPageNumber = queryExpression.PageInfo?.PageNumber > 1
            ? queryExpression.PageInfo.PageNumber
            : 1;

        return await ExecuteRecordQueryAsync(tableResult.Value, queryResult.Value, currentPageNumber, cancellationToken);
    }

    private async Task<ErrorOr<EntityCollection>> RetrieveMultipleFetchExpressionAsync(
        FetchExpression fetchExpression,
        CancellationToken cancellationToken)
    {
        var entityNameResult = DataverseXrmFetchExpressionTranslator.ResolveEntityLogicalName(fetchExpression);
        if (entityNameResult.IsError)
        {
            return entityNameResult.Errors;
        }

        var tableResult = await mediator.Send(
            new GetTableDefinitionQuery(entityNameResult.Value),
            cancellationToken);
        if (tableResult.IsError)
        {
            return tableResult.Errors;
        }

        var translationResult = DataverseXrmFetchExpressionTranslator.Translate(fetchExpression, tableResult.Value);
        if (translationResult.IsError)
        {
            return translationResult.Errors;
        }

        return await ExecuteRecordQueryAsync(
            tableResult.Value,
            translationResult.Value.Query,
            translationResult.Value.CurrentPageNumber,
            cancellationToken);
    }

    private async Task<ErrorOr<EntityCollection>> ExecuteRecordQueryAsync(
        TableDefinition table,
        Domain.Queries.RecordQuery query,
        int currentPageNumber,
        CancellationToken cancellationToken)
    {
        var rowsResult = await mediator.Send(new ListRowsQuery(query), cancellationToken);
        if (rowsResult.IsError)
        {
            return rowsResult.Errors;
        }

        return DataverseXrmEntityMapper.ToEntityCollection(table, rowsResult.Value, currentPageNumber);
    }

    private async Task<ErrorOr<IReadOnlyDictionary<string, TableDefinition>>> ResolveLinkedTablesByAliasAsync(
        LinkedRecordQuery query,
        CancellationToken cancellationToken)
    {
        var tablesByAlias = new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var join in query.Joins)
        {
            if (tablesByAlias.ContainsKey(join.Alias))
            {
                continue;
            }

            var tableResult = await mediator.Send(
                new GetTableDefinitionQuery(join.TableLogicalName),
                cancellationToken);
            if (tableResult.IsError)
            {
                return tableResult.Errors;
            }

            tablesByAlias[join.Alias] = tableResult.Value;
        }

        return tablesByAlias;
    }
}
