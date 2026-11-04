using OpenBible.Application.Reading.Formatting;
using Xunit;

namespace OpenBible.Application.Tests;

public sealed class VerseMarkupFormatterTests
{
    [Fact]
    public void ToDisplayText_TextWithoutMarkers_IsUnchanged() =>
        Assert.Equal("In the beginning God created.", VerseMarkupFormatter.ToDisplayText("In the beginning God created."));

    [Fact]
    public void ToDisplayText_FootnoteMarkers_BecomeSuperscriptDigits() =>
        Assert.Equal(
            "Open the first footnote \u00b9 or the second footnote \u00b2.",
            VerseMarkupFormatter.ToDisplayText("@@Open the first footnote @<f1@>@/ or the second footnote @<f2@>@/."));

    [Fact]
    public void ToDisplayText_CrossReferenceMarkers_BecomeReferenceMarks() =>
        Assert.Equal(
            "Tap here: \u203b or here: \u203b.",
            VerseMarkupFormatter.ToDisplayText("@@Tap here: @<x1@>@/ or here: @<x2@>@/."));

    [Fact]
    public void ToDisplayText_CharacterSpanMarkers_AreRemoved() =>
        Assert.Equal(
            "Dan kata-Nya: \u0022Truly\u0022.",
            VerseMarkupFormatter.ToDisplayText("@@Dan kata-Nya: @6\u0022Truly\u0022@5."));

    [Fact]
    public void ToDisplayText_ParagraphMarkers_BecomeSpaces() =>
        Assert.Equal("One Two Three", VerseMarkupFormatter.ToDisplayText("@@@1One@2Two@^Three"));

    [Fact]
    public void ToDisplayText_UnsupportedInlineElement_KeepsInnerText() =>
        Assert.Equal("Furigana", VerseMarkupFormatter.ToDisplayText("@<to:Gen.3.3@>Furigana@/"));

    [Fact]
    public void ToDisplayText_InvalidFootnoteIndex_IsDropped() =>
        Assert.Equal("Text", VerseMarkupFormatter.ToDisplayText("@<f0@>@/Text"));

    [Fact]
    public void ToDisplayText_UnterminatedInlineElement_KeepsPrecedingText() =>
        Assert.Equal("abc", VerseMarkupFormatter.ToDisplayText("abc@<f1"));

    [Fact]
    public void ToDisplayText_TrailingAtSign_IsDropped() =>
        Assert.Equal("abc", VerseMarkupFormatter.ToDisplayText("abc@"));

    [Fact]
    public void ToPlainText_RemovesFootnoteMarks() =>
        Assert.Equal("Note ", VerseMarkupFormatter.ToPlainText("@@Note @<f1@>@/"));

    [Fact]
    public void ToPlainText_RemovesCrossReferenceMarks() =>
        Assert.Equal("See also ", VerseMarkupFormatter.ToPlainText("@@See also @<x1@>@/"));

    [Fact]
    public void ToDisplayText_EmptyOrNullText_StaysEmpty()
    {
        Assert.Equal(string.Empty, VerseMarkupFormatter.ToDisplayText(string.Empty));
        Assert.Equal(string.Empty, VerseMarkupFormatter.ToDisplayText(null));
    }
}
