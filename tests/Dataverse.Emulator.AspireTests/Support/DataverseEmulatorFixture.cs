using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Projects;

namespace Dataverse.Emulator.AspireTests;

public sealed class DataverseEmulatorFixture : IAsyncLifetime
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);
    private DistributedApplication? app;
    private string? connectionString;

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(DefaultTimeout);

        try
        {
            var builder = await DistributedApplicationTestingBuilder.CreateAsync<Dataverse_Emulator_AppHost>(cts.Token);
            builder.Services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddFilter(builder.Environment.ApplicationName, LogLevel.Debug);
                logging.AddFilter("Aspire.", LogLevel.Debug);
                logging.AddSimpleConsole();
            });

            app = await builder.BuildAsync(cts.Token).WaitAsync(DefaultTimeout, cts.Token);
            await app.StartAsync(cts.Token).WaitAsync(DefaultTimeout, cts.Token);
            await app.ResourceNotifications
                .WaitForResourceAsync("dataverse-emulator", KnownResourceStates.Running, cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("dataverse-emulator", cts.Token)
                .WaitAsync(DefaultTimeout, cts.Token);
            connectionString = await app.GetConnectionStringAsync("dataverse", cts.Token).AsTask()
                .WaitAsync(DefaultTimeout, cts.Token);
        }
        catch
        {
            if (app is not null)
            {
                await app.DisposeAsync();
                app = null;
            }

            throw;
        }
    }

    public HttpClient CreateClient()
    {
        Assert.NotNull(app);
        return app.CreateHttpClient("dataverse-emulator", "http");
    }

    public Task<string> GetConnectionStringAsync()
        => Task.FromResult(connectionString ?? throw new InvalidOperationException("The emulator connection string has not been initialized."));

    public async Task ResetAsync()
        => await ResetAsync(scenario: null);

    public async Task ResetAsync(string? scenario)
    {
        using var client = CreateClient();
        var path = string.IsNullOrWhiteSpace(scenario)
            ? "/_emulator/v1/reset"
            : $"/_emulator/v1/reset?scenario={Uri.EscapeDataString(scenario)}";

        var response = await client.PostAsync(path, content: null);
        var payload = await response.ReadRequiredJsonAsync();

        Assert.Equal("reset", payload.GetProperty("status").GetString());
        Assert.Equal("scenario", payload.GetProperty("baselineKind").GetString());
        Assert.Equal(
            string.IsNullOrWhiteSpace(scenario) ? "default-seed" : scenario,
            payload.GetProperty("baselineName").GetString());
    }

    public async Task<JsonElement> ExportSnapshotAsync()
    {
        using var client = CreateClient();
        var response = await client.GetAsync("/_emulator/v1/snapshot");
        return await response.ReadRequiredJsonAsync();
    }

    public async Task<JsonElement> ImportSnapshotAsync(JsonElement snapshot)
    {
        using var client = CreateClient();
        using var content = new StringContent(snapshot.GetRawText(), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/_emulator/v1/snapshot", content);
        return await response.ReadRequiredJsonAsync();
    }

    public async Task<JsonElement> GetXrmTracesAsync(int? limit = null)
    {
        using var client = CreateClient();
        var path = limit.HasValue
            ? $"/_emulator/v1/traces/xrm?limit={limit.Value}"
            : "/_emulator/v1/traces/xrm";
        var response = await client.GetAsync(path);
        return await response.ReadRequiredJsonAsync();
    }

    public async Task ClearXrmTracesAsync()
    {
        using var client = CreateClient();
        var response = await client.DeleteAsync("/_emulator/v1/traces/xrm");
        var payload = await response.ReadRequiredJsonAsync();

        Assert.Equal("cleared", payload.GetProperty("status").GetString());
        Assert.Equal("xrm", payload.GetProperty("traceKind").GetString());
    }

    public async Task<JsonElement> RunCrmHarnessAsync(string scenario, params string[] args)
    {
        var harnessPath = ResolveHarnessPath();
        Assert.True(File.Exists(harnessPath), $"Could not find CrmServiceClient harness at '{harnessPath}'.");

        var startInfo = new ProcessStartInfo(harnessPath)
        {
            WorkingDirectory = Path.GetDirectoryName(harnessPath)!,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add(scenario);
        startInfo.ArgumentList.Add(await GetConnectionStringAsync());

        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }

        using var process = new Process { StartInfo = startInfo };
        Assert.True(process.Start(), $"Failed to start CrmServiceClient harness at '{harnessPath}'.");

        using var cts = new CancellationTokenSource(DefaultTimeout);
        var outputTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        var errorTask = process.StandardError.ReadToEndAsync(cts.Token);

        await process.WaitForExitAsync(cts.Token).WaitAsync(DefaultTimeout, cts.Token);

        var output = await outputTask;
        var error = await errorTask;

        Assert.Equal(0, process.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(output), error);

        return JsonSerializer.Deserialize<JsonElement>(output);
    }

    public async Task DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
            app = null;
        }
    }

    private static string ResolveHarnessPath()
    {
        var configuration = new DirectoryInfo(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."))).Name;

        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "Dataverse.Emulator.CrmServiceClientHarness",
                "bin",
                configuration,
                "net48",
                "Dataverse.Emulator.CrmServiceClientHarness.exe"));
    }
}
