using OpenBible.Domain.Entities;

namespace OpenBible.Infrastructure.YesFile.Sections;

/// <summary>
/// Reads the YES2 <c>footnotes</c> section into per-verse footnote entries.
/// </summary>
internal static class FootnoteSectionReader
{
    public static IReadOnlyDictionary<int, IReadOnlyList<FootnoteEntry>> Read(SectionContentStream content)
    {
        var grouped = new Dictionary<int, List<FootnoteEntry>>();
        foreach (var entry in ArifEntrySectionReader.Read(content))
        {
            var (reference, index) = Yes2Ari.SplitArif(entry.Arif);
            var ari = reference.ToAri();
            if (!grouped.TryGetValue(ari, out var footnotes))
            {
                grouped[ari] = footnotes = [];
            }

            footnotes.Add(new FootnoteEntry(reference, index, entry.Content));
        }

        return grouped.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<FootnoteEntry>)pair.Value.OrderBy(static footnote => footnote.Index).ToList());
    }
}
