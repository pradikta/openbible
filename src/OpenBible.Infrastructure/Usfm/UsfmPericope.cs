using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.Usfm;

internal sealed record UsfmPericope(VerseReference Reference, string Title);
