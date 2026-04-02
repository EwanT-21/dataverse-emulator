using Dataverse.Emulator.Domain.Common;
using ErrorOr;

namespace Dataverse.Emulator.Domain.Queries;

public sealed record PageRequest
{
    internal PageRequest(
        int size,
        string? continuationToken = null)
    {
        Size = size;
        ContinuationToken = continuationToken;
    }

    public int Size { get; }

    public string? ContinuationToken { get; }

    public static ErrorOr<PageRequest> Create(
        int size = 50,
        string? continuationToken = null)
    {
        if (size <= 0)
        {
            return DomainErrors.Validation(
                "Query.Page.SizeInvalid",
                "Page size must be greater than zero.");
        }

        return new PageRequest(size, continuationToken);
    }
}
