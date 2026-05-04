using Dataverse.Emulator.Host.Telemetry;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class DataverseCompatibilityTelemetryConfigurationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_Enabled_Defaults_To_Disabled_When_Configuration_Is_Absent(string? configuredValue)
    {
        Assert.False(DataverseCompatibilityTelemetryConfiguration.ResolveEnabled(configuredValue));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("FALSE", false)]
    public void Resolve_Enabled_Honours_Boolean_Configuration(string configuredValue, bool expected)
    {
        Assert.Equal(expected, DataverseCompatibilityTelemetryConfiguration.ResolveEnabled(configuredValue));
    }

    [Fact]
    public void Resolve_Enabled_Throws_For_Non_Boolean_Configuration()
    {
        Assert.Throws<InvalidOperationException>(
            () => DataverseCompatibilityTelemetryConfiguration.ResolveEnabled("yes-please"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_Endpoint_Returns_Null_When_Configuration_Is_Absent(string? configuredValue)
    {
        Assert.Null(DataverseCompatibilityTelemetryConfiguration.ResolveEndpoint(configuredValue));
    }

    [Fact]
    public void Resolve_Endpoint_Accepts_Https_Endpoint()
    {
        var endpoint = DataverseCompatibilityTelemetryConfiguration.ResolveEndpoint(
            "https://telemetry.example.test/v1/events");

        Assert.NotNull(endpoint);
        Assert.Equal("https://telemetry.example.test/v1/events", endpoint!.ToString());
    }

    [Fact]
    public void Resolve_Endpoint_Accepts_Loopback_Http_Endpoint()
    {
        var endpoint = DataverseCompatibilityTelemetryConfiguration.ResolveEndpoint(
            "http://127.0.0.1:5050/telemetry");

        Assert.NotNull(endpoint);
        Assert.Equal("http://127.0.0.1:5050/telemetry", endpoint!.ToString());
    }

    [Fact]
    public void Resolve_Endpoint_Rejects_Non_Loopback_Http_Endpoint()
    {
        Assert.Throws<InvalidOperationException>(
            () => DataverseCompatibilityTelemetryConfiguration.ResolveEndpoint(
                "http://telemetry.example.test/v1/events"));
    }

    [Fact]
    public void Resolve_Endpoint_Rejects_Malformed_Uri()
    {
        Assert.Throws<InvalidOperationException>(
            () => DataverseCompatibilityTelemetryConfiguration.ResolveEndpoint("not a uri"));
    }
}
