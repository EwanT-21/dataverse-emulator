namespace Dataverse.Emulator.Domain.Common;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException(IEnumerable<string> errors)
        : base("Domain validation failed.")
    {
        Errors = errors
            .Where(error => !string.IsNullOrWhiteSpace(error))
            .ToArray();
    }

    public IReadOnlyCollection<string> Errors { get; }
}
