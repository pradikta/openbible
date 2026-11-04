using OpenBible.Domain.Entities;

namespace OpenBible.Infrastructure.YesFile.Sections;

/// <summary>
/// Reads the YES2 <c>xrefs</c> section into per-verse cross-reference entries.
/// </summary>
internal static class XRefSectionReader
{
    public static IReadOnlyDictionary<int, IReadOnlyList<XRefEntry>> Read(SectionContentStream content)
    {
        var grouped = new Dictionary<int, List<XRefEntry>>();
        foreach (var entry in ArifEntrySectionReader.Read(content))
        {
            var (reference, index) = Yes2Ari.SplitArif(entry.Arif);
            var ari = reference.ToAri();
            if (!grouped.TryGetValue(ari, out var xrefs))
            {
                grouped[ari] = xrefs = [];
            }

            xrefs.Add(new XRefEntry(reference, index, entry.Content));
        }

        return grouped.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<XRefEntry>)pair.Value.OrderBy(static xref => xref.Index).ToList());
    }
}
