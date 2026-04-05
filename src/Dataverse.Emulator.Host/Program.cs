using Dataverse.Emulator.Application;
using Dataverse.Emulator.Application.Behaviors;
using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Host;
using Dataverse.Emulator.Persistence.InMemory;
using Dataverse.Emulator.Protocols.WebApi;
using Dataverse.Emulator.Protocols.Xrm;
using ErrorOr;
using Mediator;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDataverseEmulatorApplication();
builder.Services.AddDataverseEmulatorInMemoryPersistence();
builder.Services.AddDataverseEmulatorXrmProtocol();
builder.Services.AddHostedService<DefaultSeedHostedService>();
builder.Services.AddMediator(options =>
{
    options.Assemblies = [typeof(Dataverse.Emulator.Application.AssemblyMarker)];
    options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
});

var app = builder.Build();

app.MapGet(
    "/",
    () => Results.Ok(
        new EmulatorDescriptor(
            "Dataverse Emulator",
            "Local emulator slice implemented for hosted Xrm/C# compatibility, shared account and contact semantics, secondary Web API CRUD, and reset plus snapshot workflows",
            [
                "Xrm/C# organization service",
                "Dataverse Web API"
            ],
            "In-memory persistence",
            "AuthType=AD;Url=http://localhost:{port}/org;Domain=EMULATOR;Username=local;Password=local")));

app.MapGet("/status", () => Results.Ok(new HealthDescriptor("healthy", DateTimeOffset.UtcNow)));

app.MapPost(
    "/_emulator/v1/reset",
    async (SeedScenarioExecutor seedScenarioExecutor, CancellationToken cancellationToken) =>
    {
        await seedScenarioExecutor.ExecuteAsync(DefaultSeedScenarioFactory.Create(), cancellationToken);
        return Results.Ok(new EmulatorResetDescriptor(
            "reset",
            "default-seed",
            DateTimeOffset.UtcNow));
    });

app.MapGet(
    "/_emulator/v1/snapshot",
    async (SeedScenarioSnapshotService snapshotService, CancellationToken cancellationToken) =>
    {
        var snapshotResult = await snapshotService.CaptureAsync(cancellationToken);
        return snapshotResult.IsError
            ? ToAdminErrorResult(snapshotResult.Errors)
            : Results.Ok(snapshotResult.Value);
    });

app.MapPost(
    "/_emulator/v1/snapshot",
    async (
        SeedScenarioSnapshotDocument? snapshot,
        SeedScenarioSnapshotService snapshotService,
        CancellationToken cancellationToken) =>
    {
        if (snapshot is null)
        {
            return ToAdminErrorResult(
            [
                Error.Validation(
                    "Seeding.Snapshot.Required",
                    "Snapshot payload is required.")
            ]);
        }

        var restoreResult = await snapshotService.RestoreAsync(snapshot, cancellationToken);
        if (restoreResult.IsError)
        {
            return ToAdminErrorResult(restoreResult.Errors);
        }

        return Results.Ok(new EmulatorSnapshotImportedDescriptor(
            "imported",
            snapshot.SchemaVersion,
            restoreResult.Value.Tables.Count,
            restoreResult.Value.Records.Count,
            DateTimeOffset.UtcNow));
    });

app.MapGet(
    "/roadmap",
    () => Results.Ok(
        new[]
        {
            "Primary compatibility target: hosted CrmServiceClient bootstrap against /org with real Xrm/C# CRUD, query, metadata, and demand-driven Execute coverage.",
            "Current table slice: seeded account and contact metadata, shared single-table and linked-query semantics, and matching Web API CRUD on /api/data/v9.2/accounts and /api/data/v9.2/contacts.",
            "Current local workflow support: reset the emulator to its default seeded state through /_emulator/v1/reset.",
            "Current local workflow support: export and import snapshot documents through /_emulator/v1/snapshot."
        }));

app.MapDataverseWebApi();
app.MapDataverseXrm();
app.MapDefaultEndpoints();
app.Run();

static IResult ToAdminErrorResult(IReadOnlyList<Error> errors)
    => Results.BadRequest(new EmulatorAdminErrorDescriptor(
        "invalid-request",
        errors.Select(error => new EmulatorAdminErrorItem(error.Code, error.Description)).ToArray()));

public sealed record EmulatorDescriptor(
    string Name,
    string Status,
    string[] Protocols,
    string Persistence,
    string ConnectionStringTemplate);

public sealed record HealthDescriptor(string Status, DateTimeOffset UtcNow);

public sealed record EmulatorResetDescriptor(
    string Status,
    string Scenario,
    DateTimeOffset UtcNow);

public sealed record EmulatorSnapshotImportedDescriptor(
    string Status,
    string SchemaVersion,
    int TableCount,
    int RecordCount,
    DateTimeOffset UtcNow);

public sealed record EmulatorAdminErrorDescriptor(
    string Error,
    EmulatorAdminErrorItem[] Details);

public sealed record EmulatorAdminErrorItem(
    string Code,
    string Description);

public partial class Program
{
}
