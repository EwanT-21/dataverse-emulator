using System;

namespace Dataverse.Emulator.AspireTests;

public sealed class AppHostPackagingAspireTests(DataverseEmulatorFixture fixture)
    : IClassFixture<DataverseEmulatorFixture>
{
    [Fact]
    public async Task AppHost_Exposes_Emulator_ConnectionString()
    {
        var connectionString = await fixture.GetConnectionStringAsync();

        Assert.Contains("AuthType=AD;", connectionString, StringComparison.Ordinal);
        Assert.Contains("/org;", connectionString, StringComparison.Ordinal);
        Assert.Contains("Domain=EMULATOR;", connectionString, StringComparison.Ordinal);
        Assert.Contains("Username=local;", connectionString, StringComparison.Ordinal);
        Assert.Contains("Password=local", connectionString, StringComparison.Ordinal);
    }
}
