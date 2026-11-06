using System.Text;
using System.Text.RegularExpressions;
using OpenBible.Domain.Enums;
using OpenBible.Domain.ValueObjects;

namespace OpenBible.Infrastructure.Usfm;

internal static class UsfmBookParser
{
    private static readonly Regex Footnote = new(
        @"\\(?<marker>f|x|fig)\b.*?\\\k<marker>\*",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex WordMarker = new(
        @"\\w\s+([^|\\]+)(?:\|[^\\]*)?\\w\*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex PairedMarker = new(
        @"\\(add|nd|wj|em|bd|it|sc|sup|no|qt|sig|pn)\s+(.*?)\\\1\*",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex Marker = new(@"\\[a-z0-9]+\*?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static UsfmBookData? Parse(string source)
    {
        BookId? bookId = null;
        string? title = null;
        var chapters = new SortedDictionary<int, List<UsfmVerse>>();
        var pericopes = new List<UsfmPericope>();
        var currentChapter = 0;
        var currentVerse = 0;
        var currentText = new StringBuilder();
        string? pendingHeading = null;

        void Flush()
        {
            if (currentChapter <= 0 || currentVerse <= 0)
            {
                return;
            }

            var text = Clean(currentText.ToString());
            if (text.Length == 0)
            {
                return;
            }

            if (!chapters.TryGetValue(currentChapter, out var verses))
            {
                verses = [];
                chapters[currentChapter] = verses;
            }

            verses.Add(new UsfmVerse(currentVerse, text));
            currentText.Clear();
        }

        foreach (var rawLine in source.Split('\n'))
        {
            var line = rawLine.Trim('\r', ' ', '\t');
            if (line.Length == 0)
            {
                continue;
            }

            if (line[0] != '\\')
            {
                if (currentVerse > 0)
                {
                    currentText.Append(' ').Append(Clean(line));
                }

                continue;
            }

            var separator = line.IndexOf(' ');
            var command = separator < 0 ? line[1..] : line[1..separator];
            var rest = separator < 0 ? string.Empty : line[(separator + 1)..];

            switch (command.ToLowerInvariant())
            {
                case "id":
                    var code = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (UsfmBookCodeMap.TryGet(code, out var mapped))
                    {
                        bookId = mapped;
                    }

                    break;
                case "h":
                case "mt":
                    title ??= Clean(rest);
                    break;
                case "c":
                    Flush();
                    if (int.TryParse(rest.Trim(), out var chapter) && chapter > 0)
                    {
                        currentChapter = chapter;
                        currentVerse = 0;
                    }

                    break;
                case "v":
                    Flush();
                    var parts = rest.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0 || !int.TryParse(parts[0], out var verseNumber))
                    {
                        break;
                    }

                    currentVerse = verseNumber;
                    if (bookId is { } id && pendingHeading is not null)
                    {
                        pericopes.Add(new UsfmPericope(new VerseReference(id, currentChapter, verseNumber), pendingHeading));
                        pendingHeading = null;
                    }

                    if (parts.Length > 1)
                    {
                        currentText.Append(parts[1]);
                    }

                    break;
                case "s":
                case "s1":
                case "s2":
                case "s3":
                    var heading = Clean(rest);
                    if (heading.Length > 0)
                    {
                        pendingHeading = heading;
                    }

                    break;
                default:
                    if (currentVerse > 0)
                    {
                        var fragment = Clean(rest);
                        if (fragment.Length > 0)
                        {
                            currentText.Append(' ').Append(fragment);
                        }
                    }

                    break;
            }
        }

        Flush();

        if (bookId is null || chapters.Count == 0)
        {
            return null;
        }

        return new UsfmBookData(
            bookId.Value,
            string.IsNullOrWhiteSpace(title) ? null : title,
            [.. chapters.Select(pair => new UsfmChapter(pair.Key, pair.Value))],
            pericopes);
    }

    private static string Clean(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        text = Footnote.Replace(text, " ");
        text = WordMarker.Replace(text, "$1");
        text = PairedMarker.Replace(text, "$2");
        text = Marker.Replace(text, " ");
        return Whitespace.Replace(text, " ").Trim();
    }
}
