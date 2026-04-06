using Dataverse.Emulator.Protocols.Common;
using ErrorOr;

namespace Dataverse.Emulator.IntegrationTests;

public sealed class ProtocolErrorMappingTests
{
    [Fact]
    public void Shared_Error_Model_Maps_To_Sdk_Faults()
    {
        var fault = DataverseProtocolErrorMapper.ToFaultException(
            [Error.Validation("Protocol.Test", "A validation error occurred.")]);

        Assert.Equal(unchecked((int)0x80040203), fault.Detail.ErrorCode);
        Assert.Equal("A validation error occurred.", fault.Detail.Message);
        Assert.Equal("Protocol.Test", fault.Detail.ErrorDetails["DataverseEmulator.ErrorCode"]);
    }
}
