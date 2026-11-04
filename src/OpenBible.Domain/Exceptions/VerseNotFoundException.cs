using OpenBible.Domain.ValueObjects;

namespace OpenBible.Domain.Exceptions;

public sealed class VerseNotFoundException : Exception
{
    public VerseReference Reference { get; }

    public VerseNotFoundException(VerseReference reference)
        : base($"Verse {reference} was not found.")
    {
        Reference = reference;
    }
}
