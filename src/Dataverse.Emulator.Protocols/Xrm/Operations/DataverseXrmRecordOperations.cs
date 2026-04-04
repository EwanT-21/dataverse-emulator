using Dataverse.Emulator.Application.Metadata;
using Dataverse.Emulator.Application.Records;
using ErrorOr;
using Mediator;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Dataverse.Emulator.Protocols.Xrm.Operations;

public sealed class DataverseXrmRecordOperations(IMediator mediator)
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

        var currentPageNumber = query is QueryExpression queryExpression && queryExpression.PageInfo?.PageNumber > 1
            ? queryExpression.PageInfo.PageNumber
            : 1;

        return DataverseXrmEntityMapper.ToEntityCollection(tableResult.Value, rowsResult.Value, currentPageNumber);
    }
}
