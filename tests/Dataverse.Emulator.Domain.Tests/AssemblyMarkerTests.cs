using Dataverse.Emulator.Domain;

namespace Dataverse.Emulator.Domain.Tests;

public class AssemblyMarkerTests
{
    [Fact]
    public void DomainAssemblyCanBeResolved()
    {
        Assert.Equal("Dataverse.Emulator.Domain", typeof(AssemblyMarker).Assembly.GetName().Name);
    }
}
