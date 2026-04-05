using Dataverse.Emulator.Domain.Queries;
using System.Globalization;

namespace Dataverse.Emulator.Domain.Services;

public sealed class ContinuationPagingService
{
    public PageResult<T> Apply<T>(
        IReadOnlyList<T> items,
        PageRequest? page)
    {
        if (page is null)
        {
            return new PageResult<T>(items);
        }

        var offset = DecodeOffset(page.ContinuationToken);
        var pagedItems = items
            .Skip(offset)
            .Take(page.Size)
            .ToArray();

        var nextOffset = offset + pagedItems.Length;
        var continuationToken = nextOffset < items.Count
            ? EncodeOffset(nextOffset)
            : null;

        return new PageResult<T>(pagedItems, continuationToken);
    }

    public int DecodeOffset(string? continuationToken)
    {
        var normalizedToken = continuationToken?
            .Split('|', 2, StringSplitOptions.TrimEntries)[0];

        return int.TryParse(normalizedToken, NumberStyles.None, CultureInfo.InvariantCulture, out var offset) && offset > 0
            ? offset
            : 0;
    }

    public string EncodeOffset(int offset)
        => offset.ToString(CultureInfo.InvariantCulture);
}
