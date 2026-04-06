using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Domain.Metadata;
using Dataverse.Emulator.Domain.Services;
using Dataverse.Emulator.Persistence.InMemory.Metadata;
using Dataverse.Emulator.Persistence.InMemory.Records;

namespace Dataverse.Emulator.IntegrationTests;

public class SnapshotServiceIntegrationTests
{
    [Fact]
    public async Task Snapshot_Service_Captures_And_Restores_Current_State()
    {
        var metadataRepository = new InMemoryMetadataRepository();
        var recordRepository = new InMemoryRecordRepository();
        var recordValidationService = new RecordValidationService();
        var snapshotMapper = new SeedScenarioSnapshotMapper(recordValidationService);
        var seedExecutor = new SeedScenarioExecutor(metadataRepository, recordRepository);
        var snapshotService = new SeedScenarioSnapshotService(
            metadataRepository,
            recordRepository,
            snapshotMapper,
            seedExecutor);

        var idColumn = ColumnDefinition.Create(
            "accountid",
            AttributeType.UniqueIdentifier,
            RequiredLevel.SystemRequired,
            isPrimaryId: true);
        var nameColumn = ColumnDefinition.Create(
            "name",
            AttributeType.String,
            RequiredLevel.ApplicationRequired,
            isPrimaryName: true);
        var primaryCustomerColumn = ColumnDefinition.Create(
            "primarycontactid",
            AttributeType.Lookup,
            RequiredLevel.None,
            lookupTargetTable: "contact",
            lookupRelationshipName: "account_primary_contact");
        var table = TableDefinition.Create(
            logicalName: "account",
            entitySetName: "accounts",
            primaryIdAttribute: "accountid",
            primaryNameAttribute: "name",
            columns:
            [
                idColumn.Value,
                nameColumn.Value,
                primaryCustomerColumn.Value
            ]);
        var recordValues = Dataverse.Emulator.Domain.Records.RecordValues.Create(
            new Dictionary<string, object?>
            {
                ["accountid"] = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ["name"] = "Snapshot Account"
            });
        var record = Dataverse.Emulator.Domain.Records.EntityRecord.Create(
            "account",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            recordValues.Value);

        Assert.False(idColumn.IsError);
        Assert.False(nameColumn.IsError);
        Assert.False(primaryCustomerColumn.IsError);
        Assert.False(table.IsError);
        Assert.False(recordValues.IsError);
        Assert.False(record.IsError);

        await seedExecutor.ExecuteAsync(new SeedScenario([table.Value], [record.Value]));

        var snapshotResult = await snapshotService.CaptureAsync();
        Assert.False(snapshotResult.IsError);
        Assert.Single(snapshotResult.Value.Tables);
        Assert.Single(snapshotResult.Value.Records);

        await seedExecutor.ExecuteAsync(new SeedScenario(Array.Empty<TableDefinition>(), Array.Empty<Dataverse.Emulator.Domain.Records.EntityRecord>()));

        var restoreResult = await snapshotService.RestoreAsync(snapshotResult.Value);
        Assert.False(restoreResult.IsError);

        var restoredTables = await metadataRepository.ListAsync();
        var restoredRecords = await recordRepository.ListAsync();

        Assert.Single(restoredTables);
        Assert.Single(restoredRecords);
        Assert.Equal("account", restoredTables[0].LogicalName);
        Assert.Equal("account_primary_contact", restoredTables[0].FindColumn("primarycontactid")!.LookupRelationshipName);
        Assert.Equal("Snapshot Account", restoredRecords[0].Values["name"]);
    }
}
