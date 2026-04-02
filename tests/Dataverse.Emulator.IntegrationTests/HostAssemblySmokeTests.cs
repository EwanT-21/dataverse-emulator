namespace Dataverse.Emulator.IntegrationTests;

public class HostAssemblySmokeTests
{
    [Fact]
    public void HostAssemblyCanBeResolved()
    {
        Assert.Equal("Dataverse.Emulator.Host", typeof(Program).Assembly.GetName().Name);
    }

    [Fact]
    public void HostAssemblyContainsGeneratedMediatorType()
    {
        Assert.NotNull(typeof(Program).Assembly.GetType("Mediator.Mediator"));
    }
}
