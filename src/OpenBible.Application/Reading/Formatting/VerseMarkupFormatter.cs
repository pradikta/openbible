using System.Globalization;
using System.Text;

namespace OpenBible.Application.Reading.Formatting;

/// <summary>
/// Converts the inline formatting markers stored in YES2 verse text into display text.
/// Ported from androidbible <c>FormattedVerseText</c> and <c>VerseRenderer</c>: markers are
/// hidden, paragraphs and forced line breaks become a space, footnote markers become
/// superscript digits, and cross-reference markers become <see cref="XRefMark"/>.
/// </summary>
public static class VerseMarkupFormatter
{
    /// <summary>U+203B REFERENCE MARK, rendered where a cross-reference marker sits in the verse.</summary>
    public const char XRefMark = '\u203b';

    private static readonly char[] SuperscriptDigits =
    [
        '\u2070', '\u00b9', '\u00b2', '\u00b3', '\u2074', '\u2075', '\u2076', '\u2077', '\u2078', '\u2079'
    ];

    /// <summary>
    /// Returns the verse text without formatting markers, keeping footnote and
    /// cross-reference marks so readers can see that a note is attached.
    /// </summary>
    public static string ToDisplayText(string? text) => Format(text, includeMarks: true);

    /// <summary>
    /// Returns the verse text without any formatting marker, suitable for search and copying.
    /// </summary>
    public static string ToPlainText(string? text) => Format(text, includeMarks: false);

    private static string Format(string? text, bool includeMarks)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        var position = 0;
        while (position < text.Length)
        {
            var at = text.IndexOf('@', position);
            if (at < 0)
            {
                builder.Append(text, position, text.Length - position);
                break;
            }

            builder.Append(text, position, at - position);
            if (at + 1 >= text.Length)
            {
                break; // A trailing '@' opens nothing.
            }

            var marker = text[at + 1];
            position = at + 2;
            switch (marker)
            {
                case '<':
                    {
                        var headEnd = text.IndexOf("@>", position, StringComparison.Ordinal);
                        if (headEnd < 0)
                        {
                            return builder.ToString(); // Unterminated inline element: ignore the remainder.
                        }

                        if (includeMarks)
                        {
                            AppendInlineMark(builder, text.AsSpan(position, headEnd - position));
                        }

                        position = headEnd + 2;
                        break;
                    }

                case '0' or '1' or '2' or '3' or '4' or '^' or '8':
                    AppendSeparator(builder);
                    break;

                default:
                    // Character spans (@5 @6 @7 @9), the '@@' verse prefix, and '@/' closers are invisible.
                    break;
            }
        }

        return builder.ToString();
    }

    private static void AppendInlineMark(StringBuilder builder, ReadOnlySpan<char> head)
    {
        if (head.IsEmpty)
        {
            return;
        }

        var kind = head[0];
        if (kind == 'f' && TryReadIndex(head[1..], out var footnoteIndex))
        {
            foreach (var digit in footnoteIndex.ToString(CultureInfo.InvariantCulture))
            {
                builder.Append(SuperscriptDigits[digit - '0']);
            }
        }
        else if (kind == 'x' && TryReadIndex(head[1..], out _))
        {
            builder.Append(XRefMark);
        }

        // Other inline elements (for example link targets) contribute only their inner text.
    }

    private static void AppendSeparator(StringBuilder builder)
    {
        if (builder.Length > 0 && !char.IsWhiteSpace(builder[^1]))
        {
            builder.Append(' ');
        }
    }

    private static bool TryReadIndex(ReadOnlySpan<char> digits, out int value)
    {
        value = 0;
        if (digits.IsEmpty || digits.Length > 3)
        {
            return false;
        }

        foreach (var digit in digits)
        {
            if (!char.IsAsciiDigit(digit))
            {
                return false;
            }

            value = (value * 10) + (digit - '0');
        }

        return value > 0;
    }
}
