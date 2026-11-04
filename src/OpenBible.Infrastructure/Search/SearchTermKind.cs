namespace OpenBible.Infrastructure.Search;

internal enum SearchTermKind
{
    /// <summary>Ordinary term: matches anywhere inside the verse text.</summary>
    Substring,

    /// <summary>Term prefixed with <c>+</c>: matches a whole word only.</summary>
    WholeWord,

    /// <summary>Quoted phrase or consecutive plus-prefixed words: matches a word sequence.</summary>
    Phrase,
}
