namespace Dataverse.Emulator.Protocols.Xrm.Tracing;

public sealed record DataverseXrmTraceOptions(int TraceLimit)
{
    public const int DefaultTraceLimit = 200;
}
