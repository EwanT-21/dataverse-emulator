using System.Diagnostics;
using CoreWCF;
using Microsoft.Xrm.Sdk;

namespace Dataverse.Emulator.Protocols.Xrm.Tracing;

public sealed class DataverseXrmRequestTraceStore(DataverseXrmTraceOptions traceOptions)
{
    private readonly object gate = new();
    private readonly LinkedList<DataverseXrmRequestTraceEntry> entries = [];
    private long nextSequence;

    public T Trace<T>(string source, string name, Func<T> operation)
    {
        var startedAtUtc = DateTimeOffset.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = operation();
            Record(source, name, true, errorCode: null, message: null, startedAtUtc, stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (FaultException<OrganizationServiceFault> fault)
        {
            Record(
                source,
                name,
                false,
                fault.Detail.ErrorCode,
                fault.Detail.Message,
                startedAtUtc,
                stopwatch.ElapsedMilliseconds);
            throw;
        }
        catch (Exception ex)
        {
            Record(source, name, false, errorCode: null, ex.Message, startedAtUtc, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public void Trace(string source, string name, Action operation)
        => Trace<object?>(
            source,
            name,
            () =>
            {
                operation();
                return null;
            });

    public IReadOnlyList<DataverseXrmRequestTraceEntry> List(int? limit = null)
    {
        lock (gate)
        {
            return entries
                .Take(Math.Max(0, limit ?? traceOptions.TraceLimit))
                .ToArray();
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            entries.Clear();
        }
    }

    private void Record(
        string source,
        string name,
        bool succeeded,
        int? errorCode,
        string? message,
        DateTimeOffset startedAtUtc,
        long durationMilliseconds)
    {
        lock (gate)
        {
            entries.AddFirst(new DataverseXrmRequestTraceEntry(
                Sequence: ++nextSequence,
                Source: source,
                Name: name,
                Succeeded: succeeded,
                ErrorCode: errorCode,
                Message: message,
                StartedAtUtc: startedAtUtc,
                DurationMilliseconds: durationMilliseconds));

            while (entries.Count > traceOptions.TraceLimit)
            {
                entries.RemoveLast();
            }
        }
    }
}
