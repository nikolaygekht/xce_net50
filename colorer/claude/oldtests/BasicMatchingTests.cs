using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Enums;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Tests for basic regex matching functionality.
/// </summary>
public class BasicMatchingTests
{
    [Fact]
    public void Literal_SingleChar_Matches()
    {
        var regex = new ColorerRegex("a");

        Assert.True(regex.IsMatch("a"));
        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("bac"));
        Assert.False(regex.IsMatch(""));
        Assert.False(regex.IsMatch("bcd"));
    }

    [Fact]
    public void Literal_MultiChar_Matches()
    {
        var regex = new ColorerRegex("abc");

        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("xabcy"));
        Assert.False(regex.IsMatch("ab"));
        Assert.False(regex.IsMatch("ac"));
        Assert.False(regex.IsMatch(""));
    }

    [Fact]
    public void Dot_MatchesSingleChar()
    {
        var regex = new ColorerRegex("a.c");

        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("axc"));
        Assert.True(regex.IsMatch("a c"));
        Assert.False(regex.IsMatch("ac"));
        Assert.False(regex.IsMatch("abbc"));
    }

    [Fact]
    public void Dot_DoesNotMatchNewline_ByDefault()
    {
        var regex = new ColorerRegex("a.b");

        Assert.True(regex.IsMatch("axb"));
        Assert.False(regex.IsMatch("a\nb"));
        Assert.False(regex.IsMatch("a\rb"));
    }

    [Fact]
    public void Dot_MatchesNewline_WithSingleLineOption()
    {
        var regex = new ColorerRegex("a.b", RegexOptions.Singleline);

        Assert.True(regex.IsMatch("axb"));
        Assert.True(regex.IsMatch("a\nb"));
        Assert.True(regex.IsMatch("a\rb"));
    }

    [Fact]
    public void Anchor_Caret_MatchesStartOfLine()
    {
        var regex = new ColorerRegex("^abc");

        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("abcdef"));
        Assert.False(regex.IsMatch("xabc"));
        Assert.False(regex.IsMatch(" abc"));
    }

    [Fact]
    public void Anchor_Dollar_MatchesEndOfLine()
    {
        var regex = new ColorerRegex("abc$");

        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("xyzabc"));
        Assert.False(regex.IsMatch("abcx"));
        Assert.False(regex.IsMatch("abc "));
    }

    [Fact]
    public void Anchor_CaretDollar_MatchesFullLine()
    {
        var regex = new ColorerRegex("^abc$");

        Assert.True(regex.IsMatch("abc"));
        Assert.False(regex.IsMatch("xabc"));
        Assert.False(regex.IsMatch("abcx"));
        Assert.False(regex.IsMatch("xabcx"));
    }

    [Fact]
    public void CaseInsensitive_MatchesUpperLower()
    {
        var regex = new ColorerRegex("abc", RegexOptions.IgnoreCase);

        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("ABC"));
        Assert.True(regex.IsMatch("AbC"));
        Assert.True(regex.IsMatch("aBc"));
    }

    [Fact]
    public void CaseSensitive_DoesNotMatchDifferentCase()
    {
        var regex = new ColorerRegex("abc");

        Assert.True(regex.IsMatch("abc"));
        Assert.False(regex.IsMatch("ABC"));
        Assert.False(regex.IsMatch("AbC"));
    }

    [Fact]
    public void EscapedSpecialChar_MatchesLiteral()
    {
        var regex = new ColorerRegex(@"\.\*\+\?");

        Assert.True(regex.IsMatch(".*+?"));
        Assert.False(regex.IsMatch("abcd"));
    }

    [Fact]
    public void EmptyPattern_MatchesEmptyString()
    {
        var regex = new ColorerRegex("");

        Assert.True(regex.IsMatch(""));
        Assert.True(regex.IsMatch("anything")); // Matches at position 0
    }

    [Fact]
    public void MetaChar_Digit_MatchesDigits()
    {
        var regex = new ColorerRegex(@"\d");

        Assert.True(regex.IsMatch("0"));
        Assert.True(regex.IsMatch("9"));
        Assert.True(regex.IsMatch("a1b"));
        Assert.False(regex.IsMatch("abc"));
    }

    [Fact]
    public void MetaChar_NonDigit_MatchesNonDigits()
    {
        var regex = new ColorerRegex(@"\D");

        Assert.True(regex.IsMatch("a"));
        Assert.True(regex.IsMatch("1a"));
        Assert.False(regex.IsMatch("123"));
    }

    [Fact]
    public void MetaChar_Word_MatchesWordChars()
    {
        var regex = new ColorerRegex(@"\w");

        Assert.True(regex.IsMatch("a"));
        Assert.True(regex.IsMatch("Z"));
        Assert.True(regex.IsMatch("0"));
        Assert.True(regex.IsMatch("_"));
        Assert.False(regex.IsMatch(" "));
        Assert.False(regex.IsMatch("."));
    }

    [Fact]
    public void MetaChar_NonWord_MatchesNonWordChars()
    {
        var regex = new ColorerRegex(@"\W");

        Assert.True(regex.IsMatch(" "));
        Assert.True(regex.IsMatch("."));
        Assert.True(regex.IsMatch("!"));
        Assert.False(regex.IsMatch("a"));
        Assert.False(regex.IsMatch("0"));
    }

    [Fact]
    public void MetaChar_Whitespace_MatchesWhitespace()
    {
        var regex = new ColorerRegex(@"\s");

        Assert.True(regex.IsMatch(" "));
        Assert.True(regex.IsMatch("\t"));
        Assert.True(regex.IsMatch("\n"));
        Assert.True(regex.IsMatch("a b"));
        Assert.False(regex.IsMatch("abc"));
    }

    [Fact]
    public void MetaChar_NonWhitespace_MatchesNonWhitespace()
    {
        var regex = new ColorerRegex(@"\S");

        Assert.True(regex.IsMatch("a"));
        Assert.True(regex.IsMatch("0"));
        Assert.True(regex.IsMatch(" a"));
        Assert.False(regex.IsMatch(" "));
        Assert.False(regex.IsMatch("\t"));
    }

    [Fact]
    public void WordBoundary_MatchesAtWordEdge()
    {
        var regex = new ColorerRegex(@"\bword\b");

        Assert.True(regex.IsMatch("word"));
        Assert.True(regex.IsMatch("a word b"));
        Assert.True(regex.IsMatch("word."));
        Assert.False(regex.IsMatch("sword"));
        Assert.False(regex.IsMatch("words"));
    }

    [Fact]
    public void Match_ReturnsCorrectPosition()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("xyzabc");

        Assert.True(match.Success);
        Assert.Equal(3, match.Index);
        Assert.Equal(3, match.Length);
        Assert.Equal("abc", match.Value);
    }

    [Fact]
    public void Match_Failed_ReturnsFailedMatch()
    {
        var regex = new ColorerRegex("xyz");
        var match = regex.Match("abc");

        Assert.False(match.Success);
        Assert.Equal(-1, match.Index);
        Assert.Equal(0, match.Length);
    }

    [Fact]
    public void Match_Span_Works()
    {
        var regex = new ColorerRegex("test");

        Assert.True(regex.IsMatch("this is a test".AsSpan()));
        Assert.False(regex.IsMatch("no match".AsSpan()));
    }

    [Fact]
    public void Tilde_MatchesSchemeStart()
    {
        var regex = new ColorerRegex("~");

        // Should only match at scheme start position
        var match = regex.Match("test", 0, 4, 0);
        Assert.True(match.Success);

        match = regex.Match("test", 0, 4, 2);
        Assert.False(match.Success); // Starts at 0, but scheme start is 2
    }
}
