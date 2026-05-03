using Dataverse.Emulator.Application;
using Dataverse.Emulator.Application.Abstractions;
using Dataverse.Emulator.Application.Behaviors;
using Dataverse.Emulator.Application.Records;
using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Domain.Queries;
using Dataverse.Emulator.Persistence.InMemory;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class EmulatorStateSnapshotIntegrationTests
{
    [Fact]
    public async Task Snapshot_Service_Restores_Record_State_In_The_Application_Di_Graph()
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
        var seedExecutor = serviceProvider.GetRequiredService<SeedScenarioExecutor>();
        await seedExecutor.ExecuteAsync(ProtocolTestMetadataFactory.CreateDefaultXrmScenario());

        var snapshotService = serviceProvider.GetRequiredService<IEmulatorStateSnapshotService>();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        var snapshot = await snapshotService.CaptureAsync();

        var createResult = await mediator.Send(
            new CreateRowCommand(
                "account",
                new Dictionary<string, object?>
                {
                    ["name"] = "Snapshot Rollback",
                    ["accountnumber"] = "SR-100"
                }));

        Assert.False(createResult.IsError);

        await snapshotService.RestoreAsync(snapshot);

        var nameCondition = QueryCondition.Create("name", ConditionOperator.Equal, "Snapshot Rollback");
        Assert.False(nameCondition.IsError);

        var query = RecordQuery.Create(
            "account",
            selectedColumns: ["name"],
            conditions: [nameCondition.Value]);
        Assert.False(query.IsError);

        var verifyResult = await mediator.Send(new ListRowsQuery(query.Value));

        Assert.False(verifyResult.IsError);
        Assert.Empty(verifyResult.Value.Items);
    }
}
