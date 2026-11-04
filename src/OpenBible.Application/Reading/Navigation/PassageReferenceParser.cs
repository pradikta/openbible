using OpenBible.Application.Reading.DTOs;
using OpenBible.Domain.Enums;

namespace OpenBible.Application.Reading.Navigation;

/// <summary>Parses free-text passage references such as "John 3:16", "1 cor 13" or "Ps 23".</summary>
public static class PassageReferenceParser
{
    public static bool TryParse(
        string? input,
        IReadOnlyList<BookListItemDto> books,
        out BookId book,
        out int chapter,
        out int verse)
    {
        book = default;
        chapter = 1;
        verse = 0;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        // Split from the end: trailing digits, ':' and '-' are chapter/verse; the rest is the book name.
        var text = input.Trim();
        var split = text.Length;
        while (split > 0 && (char.IsDigit(text[split - 1]) || text[split - 1] is ':' or '.' or ' ' or '-'))
        {
            split--;
        }

        var name = Normalize(text[..split]);
        var numbers = text[split..].Trim();
        if (name.Length == 0)
        {
            return false;
        }

        var match = books.FirstOrDefault(b => Normalize(b.ShortName) == name || Normalize(b.FullName) == name)
            ?? books.FirstOrDefault(b => Normalize(b.FullName).StartsWith(name, StringComparison.Ordinal)
                || Normalize(b.ShortName).StartsWith(name, StringComparison.Ordinal));
        if (match is null)
        {
            return false;
        }

        if (numbers.Length > 0)
        {
            var parts = numbers.Split([':', '.', ' '], StringSplitOptions.RemoveEmptyEntries);
            if (!int.TryParse(parts[0], out chapter) || chapter < 1 || chapter > match.ChapterCount)
            {
                return false;
            }

            if (parts.Length > 1)
            {
                var first = parts[1].Split('-')[0];
                if (!int.TryParse(first, out verse) || verse < 1)
                {
                    return false;
                }
            }
        }

        book = match.BookId;
        return true;
    }

    private static string Normalize(string value) =>
        new([.. value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)]);
}
