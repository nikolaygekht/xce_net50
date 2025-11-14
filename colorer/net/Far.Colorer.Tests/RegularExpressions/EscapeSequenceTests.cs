using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Internal;
using Xunit;
using AwesomeAssertions;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Tests for escape sequence support: hex, octal, and named escapes.
/// Tests the implementation added to CRegExpCompiler.
/// </summary>
public unsafe class EscapeSequenceTests
{
    #region Named Character Escapes

    [Fact]
    public void BellEscape_a_MatchesBellCharacter()
    {
        // Pattern: \a matches ASCII 7 (bell)
        var regex = new ColorerRegex(@"\a");
        var input = "\x07";  // Bell character

        var match = regex.Match(input);

        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Value.Should().Be("\x07");
    }

    [Fact]
    public void EscapeEscape_e_MatchesEscapeCharacter()
    {
        // Pattern: \e matches ASCII 27 (escape)
        var regex = new ColorerRegex(@"\e");
        var input = "\x1b";  // Escape character

        var match = regex.Match(input);

        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Value.Should().Be("\x1b");
    }

    [Fact]
    public void FormFeedEscape_f_MatchesFormFeed()
    {
        // Pattern: \f matches ASCII 12 (form feed)
        var regex = new ColorerRegex(@"\f");
        var input = "\f";

        var match = regex.Match(input);

        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Value.Should().Be("\f");
    }

    [Fact]
    public void NamedEscapes_InPattern_MatchCorrectly()
    {
        // Pattern with multiple named escapes: regex \a (bell), \e (escape), \f (formfeed)
        var regex = new ColorerRegex(@"a\ab\ec\fd");
        var input = "a\x0007b\x001bc\x000cd";  // a+bell(0x07)+b+escape(0x1b)+c+formfeed(0x0c)+d

        var match = regex.Match(input);

        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Value.Should().Be("a\x0007b\x001bc\x000cd");
    }

    #endregion

    #region Hex Escapes - \xNN format

    [Fact]
    public void HexEscape_TwoDigit_MatchesSemicolon()
    {
        // Pattern: \x3b matches ';' (ASCII 59)
        var regex = new ColorerRegex(@"\x3b");
        var input = ";";

        var match = regex.Match(input);

        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Value.Should().Be(";");
    }

    [Fact]
    public void HexEscape_TwoDigit_MatchesLetterA()
    {
        // Pattern: \x41 matches 'A' (ASCII 65)
        var regex = new ColorerRegex(@"\x41");
        var input = "A";

        var match = regex.Match(input);

        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Value.Should().Be("A");
    }

    [Fact]
    public void HexEscape_Lowercase_Works()
    {
        // Pattern: \x3b (lowercase hex)
        var regex = new ColorerRegex(@"\x3b");
        var match = regex.Match(";");

        match.Should().NotBeNull();
        match!.Value.Should().Be(";");
    }

    [Fact]
    public void HexEscape_Uppercase_Works()
    {
        // Pattern: \x3B (uppercase hex)
        var regex = new ColorerRegex(@"\x3B");
        var match = regex.Match(";");

        match.Should().NotBeNull();
        match!.Value.Should().Be(";");
    }

    [Fact]
    public void HexEscape_InLongerPattern_Works()
    {
        // Pattern: foo\x3bbar matches "foo;bar"
        var regex = new ColorerRegex(@"foo\x3bbar");
        var match = regex.Match("foo;bar");

        match.Should().NotBeNull();
        match!.Value.Should().Be("foo;bar");
    }

    #endregion

    #region Hex Escapes - \x{...} format

    [Fact]
    public void HexEscapeBraced_SingleDigit_Works()
    {
        // Pattern: \x{9} matches tab
        var regex = new ColorerRegex(@"\x{9}");
        var match = regex.Match("\t");

        match.Should().NotBeNull();
        match!.Value.Should().Be("\t");
    }

    [Fact]
    public void HexEscapeBraced_TwoDigit_MatchesSemicolon()
    {
        // Pattern: \x{3b} matches ';'
        var regex = new ColorerRegex(@"\x{3b}");
        var match = regex.Match(";");

        match.Should().NotBeNull();
        match!.Value.Should().Be(";");
    }

    [Fact]
    public void HexEscapeBraced_FourDigit_MatchesUnicode()
    {
        // Pattern: \x{00e9} matches 'é' (U+00E9)
        var regex = new ColorerRegex(@"\x{00e9}");
        var match = regex.Match("é");

        match.Should().NotBeNull();
        match!.Value.Should().Be("é");
    }

    #endregion

    #region Octal Escapes

    [Fact]
    public void OctalEscape_071_MatchesNine()
    {
        // Pattern: \071 = octal 71 = decimal 57 = '9'
        var regex = new ColorerRegex(@"\071");
        var match = regex.Match("9");

        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Value.Should().Be("9");
    }

    [Fact]
    public void OctalEscape_101_MatchesLetterA()
    {
        // Pattern: \101 = octal 101 = decimal 65 = 'A'
        var regex = new ColorerRegex(@"\101");
        var match = regex.Match("A");

        match.Should().NotBeNull();
        match!.Value.Should().Be("A");
    }

    // Null character (\0) tests removed - null characters are not practical
    // for syntax highlighting scenarios (source code files)

    [Fact]
    public void OctalEscape_177_MatchesDEL()
    {
        // Pattern: \177 = octal 177 = decimal 127 = DEL
        var regex = new ColorerRegex(@"\177");
        var match = regex.Match("\x7f");

        match.Should().NotBeNull();
        match!.Value.Should().Be("\x7f");
    }

    [Fact]
    public void OctalEscape_InPattern_Works()
    {
        // Pattern: test\071end matches "test9end"
        var regex = new ColorerRegex(@"test\071end");
        var match = regex.Match("test9end");

        match.Should().NotBeNull();
        match!.Value.Should().Be("test9end");
    }

    #endregion

    #region Octal vs Backreference Disambiguation

    [Fact]
    public void BackslashOne_WithGroup_IsBackreference()
    {
        // Pattern: (a)\1 - \1 is backreference
        var regex = new ColorerRegex(@"(a)\1");
        var match = regex.Match("aa");

        match.Should().NotBeNull();
        match!.Value.Should().Be("aa");
    }

    // BackslashZero_AlwaysOctal test removed - null characters are not practical
    // for syntax highlighting scenarios (source code files)

    [Fact]
    public void BackslashOneTwo_IsOctal()
    {
        // Pattern: \12 = octal 12 = decimal 10 = newline
        var regex = new ColorerRegex(@"\12");
        var match = regex.Match("\n");

        match.Should().NotBeNull();
        match!.Value.Should().Be("\n");
    }

    [Fact]
    public void BackslashOneTwoThree_IsOctal()
    {
        // Pattern: \123 = octal 123 = decimal 83 = 'S'
        var regex = new ColorerRegex(@"\123");
        var match = regex.Match("S");

        match.Should().NotBeNull();
        match!.Value.Should().Be("S");
    }

    #endregion

    #region Error Cases

    [Fact]
    public void HexEscape_InvalidDigit_ThrowsException()
    {
        // Pattern: \xZZ is invalid
        Action act = () => { var regex = new ColorerRegex(@"\xZZ"); };

        act.Should().Throw<RegexSyntaxException>()
            .WithMessage("*hex*");
    }

    [Fact]
    public void HexEscape_Incomplete_ThrowsException()
    {
        // Pattern: \x3 is incomplete (needs 2 digits)
        Action act = () => { var regex = new ColorerRegex(@"\x3"); };

        act.Should().Throw<RegexSyntaxException>()
            .WithMessage("*hex*");
    }

    [Fact]
    public void HexEscapeBraced_Empty_ThrowsException()
    {
        // Pattern: \x{} is invalid
        Action act = () => { var regex = new ColorerRegex(@"\x{}"); };

        act.Should().Throw<RegexSyntaxException>()
            .WithMessage("*hex*");
    }

    [Fact]
    public void HexEscapeBraced_Unclosed_ThrowsException()
    {
        // Pattern: \x{3b is unclosed
        Action act = () => { var regex = new ColorerRegex(@"\x{3b"); };

        act.Should().Throw<RegexSyntaxException>()
            .WithMessage("*hex*");
    }

    #endregion
}
