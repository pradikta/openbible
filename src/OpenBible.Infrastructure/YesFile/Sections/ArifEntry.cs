namespace OpenBible.Infrastructure.YesFile.Sections;

/// <summary>
/// One entry of an ARIF-indexed YES2 section: the packed <c>arif</c> key and its Bintex string payload.
/// </summary>
internal sealed record ArifEntry(int Arif, string Content);
