using Dataverse.Emulator.Application;
using Dataverse.Emulator.Application.Records;
using Dataverse.Emulator.Application.Behaviors;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Services;
using Dataverse.Emulator.Persistence.InMemory;
using Dataverse.Emulator.Persistence.InMemory.Metadata;
using Dataverse.Emulator.Persistence.InMemory.Records;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.IntegrationTests;

public class PhaseOneScaffoldTests
{
    [Fact]
    public async Task Create_And_List_Work_Against_InMemoryRepositories()
    {
        var metadataRepository = new InMemoryMetadataRepository();
        var recordRepository = new InMemoryRecordRepository();
        var idColumn = ColumnDefinition.Create(
            "accountid",
            AttributeType.UniqueIdentifier,
            RequiredLevel.None,
            isPrimaryId: true);
        var nameColumn = ColumnDefinition.Create(
            "name",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var accountNumberColumn = ColumnDefinition.Create(
            "accountnumber",
            AttributeType.String,
            RequiredLevel.None);
        var table = TableDefinition.Create(
            logicalName: "account",
            entitySetName: "accounts",
            primaryIdAttribute: "accountid",
            primaryNameAttribute: "name",
            columns:
            [
                idColumn.Value,
                nameColumn.Value,
                accountNumberColumn.Value
            ]);

        Assert.False(idColumn.IsError);
        Assert.False(nameColumn.IsError);
        Assert.False(accountNumberColumn.IsError);
        Assert.False(table.IsError);

        await metadataRepository.AddAsync(
            table.Value);

        var createHandler = new CreateRowCommandHandler(
            metadataRepository,
            recordRepository,
            new RecordValidationService());

        var listHandler = new ListRowsHandler(
            metadataRepository,
            recordRepository,
            new QueryValidationService());

        var createResult = await createHandler.Handle(
            new CreateRowCommand(
                "account",
                new Dictionary<string, object?>
                {
                    ["name"] = "Contoso",
                    ["accountnumber"] = "A-100"
                }));

        var nameCondition = QueryCondition.Create("name", ConditionOperator.Equal, "Contoso");
        var nameSort = QuerySort.Create("name", SortDirection.Ascending);
        var recordQuery = RecordQuery.Create(
            "account",
            selectedColumns: ["name", "accountnumber"],
            conditions: [nameCondition.Value],
            sorts: [nameSort.Value],
            top: 10);

        Assert.False(nameCondition.IsError);
        Assert.False(nameSort.IsError);
        Assert.False(recordQuery.IsError);

        var listResult = await listHandler.Handle(
            new ListRowsQuery(
                recordQuery.Value));

        Assert.False(createResult.IsError);
        Assert.False(listResult.IsError);
        Assert.NotEqual(Guid.Empty, createResult.Value);
        Assert.Single(listResult.Value.Items);
        Assert.Equal("Contoso", listResult.Value.Items[0].Values["name"]);
    }

    [Fact]
    public async Task MediatorPipeline_ReturnsValidationErrors_BeforeHandlerExecution()
    {
        var services = new ServiceCollection();
        services.AddDataverseEmulatorApplication();
        services.AddDataverseEmulatorInMemoryPersistence();
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(Dataverse.Emulator.Application.AssemblyMarker)];
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
        });

        await using var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var result = await mediator.Send(
            new CreateRowCommand(
                string.Empty,
                null!));

        Assert.True(result.IsError);
        Assert.Contains(result.Errors, error => error.Code.StartsWith("Validation.", StringComparison.Ordinal));
    }
}
