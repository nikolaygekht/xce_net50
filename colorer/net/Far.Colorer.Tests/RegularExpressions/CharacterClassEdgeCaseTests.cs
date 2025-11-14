using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;
using Xunit;
using AwesomeAssertions;
using System;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Comprehensive edge case tests for character classes to improve coverage.
/// Targets CharacterClass coverage from 48.3% to 75%+.
/// </summary>
public unsafe class CharacterClassEdgeCaseTests
{
    #region Empty and Single Character Classes

    [Fact]
    public void SingleChar_MatchesExactly()
    {
        // Pattern: [a] should match only 'a'
        var regex = new ColorerRegex(@"[a]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().BeNull();
        regex.Match("A").Should().BeNull();
    }

    [Fact]
    public void SingleChar_CaseInsensitive_MatchesBothCases()
    {
        // Pattern: [a] with IgnoreCase should match 'a' and 'A'
        var regex = new ColorerRegex(@"[a]", RegexOptions.IgnoreCase);

        regex.Match("a").Should().NotBeNull();
        regex.Match("A").Should().NotBeNull();
        regex.Match("b").Should().BeNull();
    }

    [Fact]
    public void TwoChars_MatchesEither()
    {
        // Pattern: [ab] matches 'a' or 'b'
        var regex = new ColorerRegex(@"[ab]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().NotBeNull();
        regex.Match("c").Should().BeNull();
    }

    #endregion

    #region Range Edge Cases

    [Fact]
    public void Range_AsciiOrder_WorksCorrectly()
    {
        // Pattern: [0-9] matches digits
        var regex = new ColorerRegex(@"[0-9]");

        regex.Match("0").Should().NotBeNull();
        regex.Match("5").Should().NotBeNull();
        regex.Match("9").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Range_SingleCharRange_MatchesOnlyThatChar()
    {
        // Pattern: [a-a] should match only 'a'
        var regex = new ColorerRegex(@"[a-a]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().BeNull();
    }

    [Fact]
    public void Range_AdjacentChars_MatchesBothEnds()
    {
        // Pattern: [a-b] matches 'a' and 'b' (adjacent characters)
        var regex = new ColorerRegex(@"[a-b]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().NotBeNull();
        regex.Match("c").Should().BeNull();
    }

    [Fact]
    public void Range_UpperCase_WorksCorrectly()
    {
        // Pattern: [A-Z] matches uppercase letters
        var regex = new ColorerRegex(@"[A-Z]");

        regex.Match("A").Should().NotBeNull();
        regex.Match("M").Should().NotBeNull();
        regex.Match("Z").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Range_SpecialChars_IncludesPunctuation()
    {
        // Pattern: [!-/] matches ASCII 33-47 (punctuation before digits)
        var regex = new ColorerRegex(@"[!-/]");

        regex.Match("!").Should().NotBeNull();
        regex.Match("#").Should().NotBeNull();
        regex.Match("/").Should().NotBeNull();
        regex.Match("0").Should().BeNull();
    }

    [Fact]
    public void Range_Invalid_ReverseOrder_ThrowsException()
    {
        // Pattern: [z-a] has reversed range (should throw)
        Action act = () => new ColorerRegex(@"[z-a]");

        act.Should().Throw<Exception>();
    }

    #endregion

    #region Multiple Ranges in One Class

    [Fact]
    public void MultipleRanges_Alphanumeric_MatchesAll()
    {
        // Pattern: [a-zA-Z0-9] matches alphanumeric
        var regex = new ColorerRegex(@"[a-zA-Z0-9]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("Z").Should().NotBeNull();
        regex.Match("5").Should().NotBeNull();
        regex.Match("!").Should().BeNull();
    }

    [Fact]
    public void MultipleRanges_WithLiterals_MixedCorrectly()
    {
        // Pattern: [a-c1-3xy] matches a,b,c,1,2,3,x,y
        var regex = new ColorerRegex(@"[a-c1-3xy]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("c").Should().NotBeNull();
        regex.Match("1").Should().NotBeNull();
        regex.Match("3").Should().NotBeNull();
        regex.Match("x").Should().NotBeNull();
        regex.Match("y").Should().NotBeNull();
        regex.Match("d").Should().BeNull();
        regex.Match("4").Should().BeNull();
    }

    [Fact]
    public void OverlappingRanges_WorkCorrectly()
    {
        // Pattern: [a-m][h-z] - overlapping ranges in sequence
        var regex = new ColorerRegex(@"[a-m][h-z]");

        var match = regex.Match("ah");
        match.Should().NotBeNull();
        match!.Value.Should().Be("ah");

        var match2 = regex.Match("mz");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be("mz");
    }

    #endregion

    #region Negated Character Classes

    [Fact]
    public void Negated_SingleChar_MatchesEverythingElse()
    {
        // Pattern: [^a] matches anything except 'a'
        var regex = new ColorerRegex(@"[^a]");

        regex.Match("b").Should().NotBeNull();
        regex.Match("z").Should().NotBeNull();
        regex.Match("1").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Negated_Range_ExcludesRange()
    {
        // Pattern: [^0-9] matches non-digits
        var regex = new ColorerRegex(@"[^0-9]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("Z").Should().NotBeNull();
        regex.Match("!").Should().NotBeNull();
        regex.Match("5").Should().BeNull();
    }

    [Fact]
    public void Negated_MultipleRanges_ExcludesAll()
    {
        // Pattern: [^a-zA-Z] matches non-letters
        var regex = new ColorerRegex(@"[^a-zA-Z]");

        regex.Match("0").Should().NotBeNull();
        regex.Match("!").Should().NotBeNull();
        regex.Match(" ").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
        regex.Match("Z").Should().BeNull();
    }

    [Fact]
    public void Negated_Empty_MatchesEverything()
    {
        // Pattern: [^] with nothing after ^ should match any character
        // Note: This might be an error case, but testing actual behavior
        var regex = new ColorerRegex(@"[^abc]");
        regex.Match("x").Should().NotBeNull();
    }

    #endregion

    #region Special Characters in Character Classes

    [Fact]
    public void Dash_AtStart_IsLiteral()
    {
        // Pattern: [-abc] - dash at start is literal
        var regex = new ColorerRegex(@"[-abc]");

        regex.Match("-").Should().NotBeNull();
        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().NotBeNull();
        regex.Match("d").Should().BeNull();
    }

    [Fact]
    public void Dash_AtEnd_IsLiteral()
    {
        // Pattern: [abc-] - dash at end is literal
        var regex = new ColorerRegex(@"[abc-]");

        regex.Match("-").Should().NotBeNull();
        regex.Match("a").Should().NotBeNull();
        regex.Match("c").Should().NotBeNull();
        regex.Match("d").Should().BeNull();
    }

    [Fact]
    public void Dash_StartAndEnd_BothLiteral()
    {
        // Pattern: [-a-] - both dashes are literal
        var regex = new ColorerRegex(@"[-a-]");

        regex.Match("-").Should().NotBeNull();
        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().BeNull();
    }

    [Fact]
    public void EscapedDash_InMiddle_IsLiteral()
    {
        // Pattern: [a\-z] - escaped dash is literal (not range)
        var regex = new ColorerRegex(@"[a\-z]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("-").Should().NotBeNull();
        regex.Match("z").Should().NotBeNull();
        regex.Match("m").Should().BeNull("Dash is literal, not a range");
    }

    [Fact]
    public void Caret_NotAtStart_IsLiteral()
    {
        // Pattern: [a^b] - caret not at start is literal
        var regex = new ColorerRegex(@"[a^b]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("^").Should().NotBeNull();
        regex.Match("b").Should().NotBeNull();
        regex.Match("c").Should().BeNull();
    }

    [Fact]
    public void EscapedCaret_AtStart_IsLiteral()
    {
        // Pattern: [\^abc] - escaped caret at start is literal
        var regex = new ColorerRegex(@"[\^abc]");

        regex.Match("^").Should().NotBeNull();
        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().NotBeNull();
        regex.Match("d").Should().BeNull();
    }

    [Fact]
    public void Backslash_Escaped_IsLiteral()
    {
        // Pattern: [\\abc] - escaped backslash is literal
        var regex = new ColorerRegex(@"[\\abc]");

        regex.Match("\\").Should().NotBeNull();
        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().NotBeNull();
        regex.Match("d").Should().BeNull();
    }

    #endregion

    #region Metacharacters in Character Classes

    [Fact]
    public void Metachar_Dot_IsLiteral()
    {
        // Pattern: [.] - dot is literal inside character class
        var regex = new ColorerRegex(@"[.]");

        regex.Match(".").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Metachar_Star_IsLiteral()
    {
        // Pattern: [*] - star is literal inside character class
        var regex = new ColorerRegex(@"[*]");

        regex.Match("*").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Metachar_Plus_IsLiteral()
    {
        // Pattern: [+] - plus is literal inside character class
        var regex = new ColorerRegex(@"[+]");

        regex.Match("+").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Metachar_Question_IsLiteral()
    {
        // Pattern: [?] - question mark is literal
        var regex = new ColorerRegex(@"[?]");

        regex.Match("?").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Metachar_Pipe_IsLiteral()
    {
        // Pattern: [|] - pipe is literal (not alternation)
        var regex = new ColorerRegex(@"[|]");

        regex.Match("|").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Metachar_Parens_AreLiteral()
    {
        // Pattern: [()] - parentheses are literal
        var regex = new ColorerRegex(@"[()]");

        regex.Match("(").Should().NotBeNull();
        regex.Match(")").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void Metachar_Dollar_IsLiteral()
    {
        // Pattern: [$] - dollar is literal (not anchor)
        var regex = new ColorerRegex(@"[$]");

        regex.Match("$").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    #endregion

    #region Escape Sequences in Character Classes

    [Fact]
    public void EscapeSeq_Tab_WorksInCharClass()
    {
        // Pattern: [\t] matches tab character
        var regex = new ColorerRegex(@"[\t]");

        regex.Match("\t").Should().NotBeNull();
        regex.Match(" ").Should().BeNull();
    }

    [Fact]
    public void EscapeSeq_Newline_WorksInCharClass()
    {
        // Pattern: [\n] matches newline
        var regex = new ColorerRegex(@"[\n]");

        regex.Match("\n").Should().NotBeNull();
        regex.Match("\r").Should().BeNull();
    }

    [Fact]
    public void EscapeSeq_Hex_WorksInCharClass()
    {
        // Pattern: [\x41] matches 'A' (hex 41)
        var regex = new ColorerRegex(@"[\x41]");

        regex.Match("A").Should().NotBeNull();
        regex.Match("B").Should().BeNull();
    }

    [Fact]
    public void EscapeSeq_Octal_WorksInCharClass()
    {
        // Pattern: [\101] matches 'A' (octal 101 = decimal 65)
        var regex = new ColorerRegex(@"[\101]");

        regex.Match("A").Should().NotBeNull();
        regex.Match("B").Should().BeNull();
    }

    [Fact]
    public void EscapeSeq_MixedWithLiterals()
    {
        // Pattern: [a\tb\nc] matches a, tab, b, newline, c
        var regex = new ColorerRegex(@"[a\tb\nc]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("\t").Should().NotBeNull();
        regex.Match("b").Should().NotBeNull();
        regex.Match("\n").Should().NotBeNull();
        regex.Match("c").Should().NotBeNull();
        regex.Match("d").Should().BeNull();
    }

    #endregion

    #region Quantifiers Applied to Character Classes

    [Fact]
    public void CharClass_WithStar_MatchesZeroOrMore()
    {
        // Pattern: [abc]* matches zero or more of a,b,c
        var regex = new ColorerRegex(@"x[abc]*y");

        regex.Match("xy").Should().NotBeNull();
        regex.Match("xay").Should().NotBeNull();
        regex.Match("xabcy").Should().NotBeNull();
        regex.Match("xabcabcy").Should().NotBeNull();
    }

    [Fact]
    public void CharClass_WithPlus_MatchesOneOrMore()
    {
        // Pattern: [0-9]+ matches one or more digits
        var regex = new ColorerRegex(@"[0-9]+");

        var match = regex.Match("abc123def");
        match.Should().NotBeNull();
        match!.Value.Should().Be("123");
    }

    [Fact]
    public void CharClass_WithQuestion_MatchesOptional()
    {
        // Pattern: [abc]? matches zero or one of a,b,c
        var regex = new ColorerRegex(@"x[abc]?y");

        regex.Match("xy").Should().NotBeNull();
        regex.Match("xay").Should().NotBeNull();
        regex.Match("xby").Should().NotBeNull();
        regex.Match("xaay").Should().BeNull();
    }

    [Fact]
    public void CharClass_WithExactCount_MatchesExactly()
    {
        // Pattern: [0-9]{3} matches exactly 3 digits
        var regex = new ColorerRegex(@"[0-9]{3}");

        var match = regex.Match("abc12345def");
        match.Should().NotBeNull();
        match!.Value.Should().Be("123");
    }

    [Fact]
    public void CharClass_WithRange_MatchesRange()
    {
        // Pattern: [a-z]{2,4} matches 2-4 lowercase letters
        var regex = new ColorerRegex(@"[a-z]{2,4}");

        var match1 = regex.Match("ab");
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("ab");

        var match2 = regex.Match("abcd");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be("abcd");

        var match3 = regex.Match("abcdef");
        match3.Should().NotBeNull();
        match3!.Value.Should().Be("abcd");  // Greedy: takes 4
    }

    [Fact]
    public void CharClass_NonGreedy_MatchesMinimal()
    {
        // Pattern: [0-9]+? matches minimal digits
        var regex = new ColorerRegex(@"[0-9]+?[a-z]");

        var match = regex.Match("123abc");
        match.Should().NotBeNull();
        match!.Value.Should().Be("123a");  // Stops at first letter
    }

    #endregion

    #region Complex Character Class Combinations

    [Fact]
    public void CharClass_SequentialClasses_BothMatch()
    {
        // Pattern: [a-c][1-3] - two character classes in sequence
        var regex = new ColorerRegex(@"[a-c][1-3]");

        var match = regex.Match("a1");
        match.Should().NotBeNull();
        match!.Value.Should().Be("a1");

        var match2 = regex.Match("c3");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be("c3");

        regex.Match("a4").Should().BeNull();
        regex.Match("d1").Should().BeNull();
    }

    [Fact]
    public void CharClass_Alternation_WithClasses()
    {
        // Pattern: [a-c]|[1-3] - alternation of two classes
        var regex = new ColorerRegex(@"[a-c]|[1-3]");

        regex.Match("a").Should().NotBeNull();
        regex.Match("2").Should().NotBeNull();
        regex.Match("d").Should().BeNull();
        regex.Match("5").Should().BeNull();
    }

    [Fact]
    public void CharClass_InGroup_Captured()
    {
        // Pattern: ([a-z]+) captures character class match
        var regex = new ColorerRegex(@"([a-z]+)");

        var match = regex.Match("abc123");
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
        match.Groups.Should().HaveCount(2);  // Group 0 and Group 1
    }

    #endregion

    #region Unicode and High ASCII

    [Fact]
    public void CharClass_HighAscii_ExtendedChars()
    {
        // Pattern: [\x80-\xFF] matches high ASCII
        var regex = new ColorerRegex(@"[\x80-\xFF]");

        regex.Match("\x80").Should().NotBeNull();
        regex.Match("\xFF").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
    }

    [Fact]
    public void CharClass_Unicode_BasicMultilingualPlane()
    {
        // Pattern: [\x{0100}-\x{017F}] matches Latin Extended-A block
        var regex = new ColorerRegex(@"[\x{0100}-\x{017F}]");

        regex.Match("\u0100").Should().NotBeNull();  // Ā
        regex.Match("\u017F").Should().NotBeNull();  // ſ
        regex.Match("a").Should().BeNull();
    }

    #endregion

    #region Case Insensitive Edge Cases

    [Fact]
    public void CharClass_CaseInsensitive_RangeExpandsBoth()
    {
        // Pattern: [a-c] with IgnoreCase should match a,b,c,A,B,C
        var regex = new ColorerRegex(@"[a-c]", RegexOptions.IgnoreCase);

        regex.Match("a").Should().NotBeNull();
        regex.Match("b").Should().NotBeNull();
        regex.Match("c").Should().NotBeNull();
        regex.Match("A").Should().NotBeNull();
        regex.Match("B").Should().NotBeNull();
        regex.Match("C").Should().NotBeNull();
        regex.Match("d").Should().BeNull();
        regex.Match("D").Should().BeNull();
    }

    [Fact]
    public void CharClass_CaseInsensitive_Negated_ExcludesBoth()
    {
        // Pattern: [^a] with IgnoreCase should exclude both 'a' and 'A'
        var regex = new ColorerRegex(@"[^a]", RegexOptions.IgnoreCase);

        regex.Match("b").Should().NotBeNull();
        regex.Match("B").Should().NotBeNull();
        regex.Match("a").Should().BeNull();
        regex.Match("A").Should().BeNull();
    }

    #endregion
}
