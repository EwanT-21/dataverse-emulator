using Dataverse.Emulator.Application.Records;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Domain.Services;
using Dataverse.Emulator.Persistence.InMemory.Metadata;
using Dataverse.Emulator.Persistence.InMemory.Records;

namespace Dataverse.Emulator.IntegrationTests;

public class PhaseOneScaffoldTests
{
    [Fact]
    public async Task Create_And_List_Work_Against_InMemoryRepositories()
    {
        var metadataRepository = new InMemoryMetadataRepository();
        var recordRepository = new InMemoryRecordRepository();

        await metadataRepository.StoreAsync(
            new TableDefinition(
                logicalName: "account",
                entitySetName: "accounts",
                primaryIdAttribute: "accountid",
                primaryNameAttribute: "name",
                columns:
                [
                    new ColumnDefinition("accountid", AttributeType.UniqueIdentifier, RequiredLevel.None, IsPrimaryId: true),
                    new ColumnDefinition("name", AttributeType.String, RequiredLevel.ApplicationRequired, IsPrimaryName: true),
                    new ColumnDefinition("accountnumber", AttributeType.String, RequiredLevel.None)
                ]));

        var createHandler = new CreateRowCommandHandler(
            metadataRepository,
            recordRepository,
            new CreateRowCommandValidator(),
            new RecordValidationService());

        var listHandler = new ListRowsHandler(
            metadataRepository,
            recordRepository,
            new ListRowsQueryValidator(),
            new QueryValidationService());

        var createResult = await createHandler.HandleAsync(
            new CreateRowCommand(
                "account",
                new Dictionary<string, object?>
                {
                    ["name"] = "Contoso",
                    ["accountnumber"] = "A-100"
                }));

        var listResult = await listHandler.HandleAsync(
            new ListRowsQuery(
                new RecordQuery("account")
                {
                    SelectedColumns = ["name", "accountnumber"],
                    Conditions = [new QueryCondition("name", ConditionOperator.Equal, "Contoso")],
                    Sorts = [new QuerySort("name", SortDirection.Ascending)],
                    Top = 10
                }));

        Assert.False(createResult.IsError);
        Assert.False(listResult.IsError);
        Assert.NotEqual(Guid.Empty, createResult.Value);
        Assert.Single(listResult.Value.Items);
        Assert.Equal("Contoso", listResult.Value.Items[0].Values["name"]);
    }
}
