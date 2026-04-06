using System.Linq;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Dataverse.Emulator.AppHost;
using Projects;

namespace Dataverse.Emulator.AspireTests;

public sealed class AppHostModelAspireTests
{
    [Fact]
    public async Task AppHost_Helper_Can_Map_Emulator_ConnectionString_Into_A_Custom_Environment_Variable()
    {
        var builder = DistributedApplication.CreateBuilder();
        var service = builder.AddProject<Dataverse_Emulator_Host>("dataverse-emulator-model");
        var connectionString = builder.AddConnectionString("dataverse-model", expression =>
            expression.AppendLiteral("AuthType=AD;Url=http://localhost:5100/org;Domain=EMULATOR;Username=local;Password=local"));
        var emulator = new DataverseEmulatorAppHostResource(service, connectionString);
        var consumer = builder.AddExecutable("legacy-consumer", "cmd.exe", Environment.SystemDirectory)
            .WithDataverseConnectionString(emulator, "CRM_CONNECTION_STRING");

#pragma warning disable CS0618
        var environment = await consumer.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618
        var environmentKeys = environment.Select(entry => entry.Key).ToArray();

        Assert.Contains("CRM_CONNECTION_STRING", environmentKeys);
        Assert.DoesNotContain("ConnectionStrings__dataverse", environmentKeys);

        var resolvedConnectionString = environment.Single(entry => entry.Key == "CRM_CONNECTION_STRING").Value;
        Assert.Equal("{dataverse-model.connectionString}", resolvedConnectionString);
    }

    [Fact]
    public async Task AppHost_Helper_Can_Set_The_Xrm_Trace_Limit()
    {
        var builder = DistributedApplication.CreateBuilder();
        var emulator = builder.AddDataverseEmulator("dataverse-trace-model")
            .WithXrmTraceLimit(25);

#pragma warning disable CS0618
        var environment = await emulator.Service.Resource.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
#pragma warning restore CS0618

        var traceLimit = environment.Single(entry => entry.Key == "DATAVERSE_EMULATOR_XRM_TRACE_LIMIT").Value;
        Assert.Equal("25", traceLimit);
    }
}
