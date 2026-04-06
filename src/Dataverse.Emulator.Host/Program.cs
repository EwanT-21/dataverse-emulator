using Dataverse.Emulator.Application;
using Dataverse.Emulator.Application.Behaviors;
using Dataverse.Emulator.Application.Runtime;
using Dataverse.Emulator.Application.Seeding;
using Dataverse.Emulator.Host;
using Dataverse.Emulator.Persistence.InMemory;
using Dataverse.Emulator.Protocols.WebApi;
using Dataverse.Emulator.Protocols.Xrm;
using Dataverse.Emulator.Protocols.Xrm.Tracing;
using ErrorOr;
using Mediator;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddDataverseEmulatorApplication();
builder.Services.AddDataverseEmulatorInMemoryPersistence();
builder.Services.AddDataverseEmulatorXrmProtocol();
builder.Services.AddSingleton(_ => new DataverseEmulatorRuntimeSettings(
    SeedScenarioName: ResolveSeedScenarioName(builder.Configuration[DataverseEmulatorRuntimeSettings.SeedScenarioEnvironmentVariableName]),
    SnapshotPath: ResolveSnapshotPath(builder.Configuration[DataverseEmulatorRuntimeSettings.SnapshotPathEnvironmentVariableName]),
    OrganizationVersion: ResolveOrganizationVersion(builder.Configuration[DataverseEmulatorRuntimeSettings.OrganizationVersionEnvironmentVariableName]),
    XrmTraceLimit: ResolveXrmTraceLimit(builder.Configuration[DataverseEmulatorRuntimeSettings.XrmTraceLimitEnvironmentVariableName])));
builder.Services.AddTransient<DataverseEmulatorBaselineStateService>();
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
            "Local emulator slice implemented for hosted Xrm/C# compatibility, shared account and contact semantics, demand-driven Execute coverage, secondary Web API CRUD, reset plus snapshot workflows, Xrm trace capture, and Aspire-friendly baseline shaping",
            [
                "Xrm/C# organization service",
                "Dataverse Web API"
            ],
            "In-memory persistence",
            "AuthType=AD;Url=http://localhost:{port}/org;Domain=EMULATOR;Username=local;Password=local")));

app.MapGet("/status", () => Results.Ok(new HealthDescriptor("healthy", DateTimeOffset.UtcNow)));

app.MapPost(
    "/_emulator/v1/reset",
    async (
        string? scenario,
        DataverseEmulatorBaselineStateService baselineStateService,
        CancellationToken cancellationToken) =>
    {
        var restoreResult = string.IsNullOrWhiteSpace(scenario)
            ? await baselineStateService.RestoreConfiguredBaselineAsync(cancellationToken)
            : await baselineStateService.RestoreScenarioAsync(scenario, cancellationToken);

        return restoreResult.IsError
            ? ToAdminErrorResult(restoreResult.Errors)
            : Results.Ok(new EmulatorResetDescriptor(
                "reset",
                restoreResult.Value.Kind,
                restoreResult.Value.Name,
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
            "Current local workflow support: reset the emulator to a configured or named baseline through /_emulator/v1/reset.",
            "Current local workflow support: export and import snapshot documents through /_emulator/v1/snapshot.",
            "Current local workflow support: inspect and clear captured Xrm request traces through /_emulator/v1/traces/xrm."
        }));

app.MapGet(
    "/_emulator/v1/traces/xrm",
    (int? limit, DataverseXrmRequestTraceStore traceStore) =>
    {
        var items = traceStore.List(limit)
            .Select(entry => new EmulatorXrmTraceItem(
                entry.Sequence,
                entry.Source,
                entry.Name,
                entry.Succeeded,
                entry.ErrorCode,
                entry.Message,
                entry.StartedAtUtc,
                entry.DurationMilliseconds))
            .ToArray();

        return Results.Ok(new EmulatorXrmTraceDescriptor(items.Length, items));
    });

app.MapDelete(
    "/_emulator/v1/traces/xrm",
    (DataverseXrmRequestTraceStore traceStore) =>
    {
        traceStore.Clear();
        return Results.Ok(new EmulatorTraceResetDescriptor("cleared", "xrm"));
    });

app.MapDataverseWebApi();
app.MapDataverseXrm();
app.MapDefaultEndpoints();
app.Run();

static IResult ToAdminErrorResult(IReadOnlyList<Error> errors)
    => Results.BadRequest(new EmulatorAdminErrorDescriptor(
        "invalid-request",
        errors.Select(error => new EmulatorAdminErrorItem(error.Code, error.Description)).ToArray()));

static string ResolveSeedScenarioName(string? configuredSeedScenarioName)
    => string.IsNullOrWhiteSpace(configuredSeedScenarioName)
        ? DataverseEmulatorRuntimeSettings.DefaultSeedScenarioName
        : configuredSeedScenarioName.Trim();

static string? ResolveSnapshotPath(string? configuredSnapshotPath)
    => string.IsNullOrWhiteSpace(configuredSnapshotPath)
        ? null
        : configuredSnapshotPath.Trim();

static string ResolveOrganizationVersion(string? configuredOrganizationVersion)
    => string.IsNullOrWhiteSpace(configuredOrganizationVersion)
        ? DataverseEmulatorRuntimeSettings.DefaultOrganizationVersion
        : configuredOrganizationVersion.Trim();

static int ResolveXrmTraceLimit(string? configuredTraceLimit)
    => int.TryParse(configuredTraceLimit, out var traceLimit) && traceLimit > 0
        ? traceLimit
        : DataverseEmulatorRuntimeSettings.DefaultXrmTraceLimit;

public sealed record EmulatorDescriptor(
    string Name,
    string Status,
    string[] Protocols,
    string Persistence,
    string ConnectionStringTemplate);

public sealed record HealthDescriptor(string Status, DateTimeOffset UtcNow);

public sealed record EmulatorResetDescriptor(
    string Status,
    string BaselineKind,
    string BaselineName,
    DateTimeOffset UtcNow);

public sealed record EmulatorSnapshotImportedDescriptor(
    string Status,
    string SchemaVersion,
    int TableCount,
    int RecordCount,
    DateTimeOffset UtcNow);

public sealed record EmulatorXrmTraceDescriptor(
    int Count,
    EmulatorXrmTraceItem[] Items);

public sealed record EmulatorXrmTraceItem(
    long Sequence,
    string Source,
    string Name,
    bool Succeeded,
    int? ErrorCode,
    string? Message,
    DateTimeOffset StartedAtUtc,
    long DurationMilliseconds);

public sealed record EmulatorTraceResetDescriptor(
    string Status,
    string TraceKind);

public sealed record EmulatorAdminErrorDescriptor(
    string Error,
    EmulatorAdminErrorItem[] Details);

public sealed record EmulatorAdminErrorItem(
    string Code,
    string Description);

public partial class Program
{
}
