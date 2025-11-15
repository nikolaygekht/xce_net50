using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Enums;
using Far.Colorer.RegularExpressions.Internal;
using Xunit;
using AwesomeAssertions;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Integration tests for ColorerRegex - testing compiler + matcher together.
/// Tests the complete regex pipeline from pattern compilation to matching.
/// </summary>
public unsafe class ColorerRegexTests
{
    #region Basic Matching Tests

    [Fact]
    public void Match_SimpleLiteral_MatchesCorrectly()
    {
        // Arrange
        var regex = new ColorerRegex("abc");

        // Act
        var match = regex.Match("xyzabcdef");

        // Assert
        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Index.Should().Be(3);
        match.Length.Should().Be(3);
        match.Value.Should().Be("abc");
    }

    [Fact]
    public void Match_NoMatch_ReturnsNull()
    {
        // Arrange
        var regex = new ColorerRegex("xyz");

        // Act
        var match = regex.Match("abcdef");

        // Assert
        match.Should().BeNull();
    }

    [Fact]
    public void Match_EmptyPattern_MatchesAtStart()
    {
        // Arrange
        var regex = new ColorerRegex("");

        // Act
        var match = regex.Match("abc");

        // Assert
        match.Should().NotBeNull();
        match!.Success.Should().Be(true);
        match.Index.Should().Be(0);
        match.Length.Should().Be(0);
    }

    #endregion

    #region Metacharacter Tests

    [Fact]
    public void Match_Dot_MatchesAnyCharacter()
    {
        // Arrange
        var regex = new ColorerRegex("a.c");

        // Act
        var match = regex.Match("abc");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    [Fact]
    public void Match_StartOfLine_MatchesAtBeginning()
    {
        // Arrange
        var regex = new ColorerRegex("^abc");

        // Act
        var match1 = regex.Match("abcdef");
        var match2 = regex.Match("xyzabc");

        // Assert
        match1.Should().NotBeNull();
        match2.Should().BeNull();
    }

    [Fact]
    public void Match_EndOfLine_MatchesAtEnd()
    {
        // Arrange
        var regex = new ColorerRegex("abc$");

        // Act
        var match1 = regex.Match("xyzabc");
        var match2 = regex.Match("abcdef");

        // Assert
        match1.Should().NotBeNull();
        match2.Should().BeNull();
    }

    #endregion

    #region Escape Sequence Tests

    [Fact]
    public void Match_DigitEscape_MatchesDigits()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+");

        // Act
        var match = regex.Match("abc123def");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("123");
        match.Index.Should().Be(3);
    }

    [Fact]
    public void Match_WordEscape_MatchesWordCharacters()
    {
        // Arrange
        var regex = new ColorerRegex(@"\w+");

        // Act
        var match = regex.Match("  hello  ");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("hello");
    }

    [Fact]
    public void Match_WhitespaceEscape_MatchesSpaces()
    {
        // Arrange
        var regex = new ColorerRegex(@"\s+");

        // Act
        var match = regex.Match("abc   def");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("   ");
    }

    [Fact]
    public void Match_WordBoundary_MatchesAtBoundary()
    {
        // Arrange
        var regex = new ColorerRegex(@"\bword\b");

        // Act
        var match1 = regex.Match("a word here");
        var match2 = regex.Match("awordb");

        // Assert
        match1.Should().NotBeNull();
        match2.Should().BeNull();
    }

    #endregion

    #region Quantifier Tests

    [Fact]
    public void Match_Star_MatchesZeroOrMore()
    {
        // Arrange
        var regex = new ColorerRegex("a*b");

        // Act
        var match1 = regex.Match("b");
        var match2 = regex.Match("aaab");
        var match3 = regex.Match("ab");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("b");

        match2.Should().NotBeNull();
        match2!.Value.Should().Be("aaab");

        match3.Should().NotBeNull();
        match3!.Value.Should().Be("ab");
    }

    [Fact]
    public void Match_Plus_MatchesOneOrMore()
    {
        // Arrange
        var regex = new ColorerRegex("a+");

        // Act
        var match1 = regex.Match("aaa");
        var match2 = regex.Match("b");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("aaa");

        match2.Should().BeNull();
    }

    [Fact]
    public void Match_Question_MatchesZeroOrOne()
    {
        // Arrange
        var regex = new ColorerRegex("ab?c");

        // Act
        var match1 = regex.Match("abc");
        var match2 = regex.Match("ac");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("abc");

        match2.Should().NotBeNull();
        match2!.Value.Should().Be("ac");
    }

    [Fact]
    public void Match_RangeQuantifier_MatchesExactCount()
    {
        // Arrange
        var regex = new ColorerRegex("a{3}");

        // Act
        var match1 = regex.Match("aaa");
        var match2 = regex.Match("aa");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("aaa");

        match2.Should().BeNull();
    }

    [Fact]
    public void Match_RangeQuantifierMinMax_MatchesInRange()
    {
        // Arrange
        var regex = new ColorerRegex("a{2,4}");

        // Act
        var match1 = regex.Match("aa");
        var match2 = regex.Match("aaaa");
        var match3 = regex.Match("a");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("aa");

        match2.Should().NotBeNull();
        match2!.Value.Should().Be("aaaa");

        match3.Should().BeNull();
    }

    [Fact]
    public void Match_NonGreedyQuantifier_MatchesMinimal()
    {
        // Arrange
        var regex = new ColorerRegex("a+?b");

        // Act
        var match = regex.Match("aaab");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("aaab");
    }

    #endregion

    #region Capture Group Tests

    [Fact]
    public void Match_CaptureGroup_CapturesCorrectly()
    {
        // Arrange
        var regex = new ColorerRegex("(\\d+)");

        // Act
        var match = regex.Match("abc123def");

        // Assert
        match.Should().NotBeNull();
        match!.Groups.Count.Should().BeGreaterThan(1);
        match.GetGroupValue(0).Should().Be("123");
        match.GetGroupValue(1).Should().Be("123");
    }

    [Fact]
    public void Match_MultipleCaptureGroups_CapturesAll()
    {
        // Arrange
        var regex = new ColorerRegex("(\\w+):(\\d+)");

        // Act
        var match = regex.Match("port:8080");

        // Assert
        match.Should().NotBeNull();
        match!.GetGroupValue(0).Should().Be("port:8080");
        match.GetGroupValue(1).Should().Be("port");
        match.GetGroupValue(2).Should().Be("8080");
    }

    [Fact]
    public void Match_NonCapturingGroup_DoesNotCapture()
    {
        // Arrange
        var regex = new ColorerRegex("(?:\\d+)");

        // Act
        var match = regex.Match("123");

        // Assert
        match.Should().NotBeNull();
        // Should only have group 0 (full match), no group 1
        match!.Groups.Count.Should().Be(1);
    }

    [Fact]
    public void Match_NestedCaptureGroups_CapturesCorrectly()
    {
        // Arrange
        var regex = new ColorerRegex("((\\w+):(\\d+))");

        // Act
        var match = regex.Match("host:9000");

        // Assert
        match.Should().NotBeNull();
        match!.GetGroupValue(0).Should().Be("host:9000");
        match.GetGroupValue(1).Should().Be("host:9000");
        match.GetGroupValue(2).Should().Be("host");
        match.GetGroupValue(3).Should().Be("9000");
    }

    #endregion

    #region Character Class Tests

    [Fact]
    public void Match_CharacterClass_MatchesCharacters()
    {
        // Arrange
        var regex = new ColorerRegex("[abc]+");

        // Act
        var match = regex.Match("xyzabcba");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("abcba");
    }

    [Fact]
    public void Match_NegatedCharacterClass_MatchesExcluded()
    {
        // Arrange
        var regex = new ColorerRegex("[^abc]+");

        // Act
        var match = regex.Match("xyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("xyz");
    }

    [Fact]
    public void Match_CharacterRange_MatchesRange()
    {
        // Arrange
        var regex = new ColorerRegex("[a-z]+");

        // Act
        var match = regex.Match("ABC123abc");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    #endregion

    #region Alternation Tests

    [Fact]
    public void Match_Alternation_MatchesFirstOption()
    {
        // Arrange
        var regex = new ColorerRegex("cat|dog");

        // Act
        var match1 = regex.Match("I have a cat");
        var match2 = regex.Match("I have a dog");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("cat");

        match2.Should().NotBeNull();
        match2!.Value.Should().Be("dog");
    }

    [Fact]
    public void Match_AlternationInGroup_WorksCorrectly()
    {
        // Arrange
        var regex = new ColorerRegex("(cat|dog)s?");

        // Act
        var match = regex.Match("I have cats and dogs");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("cats");
    }

    #endregion

    #region Options Tests

    [Fact]
    public void Match_IgnoreCase_MatchesCaseInsensitive()
    {
        // Arrange
        var regex = new ColorerRegex("abc", RegexOptions.IgnoreCase);

        // Act
        var match1 = regex.Match("ABC");
        var match2 = regex.Match("AbC");
        var match3 = regex.Match("abc");

        // Assert
        match1.Should().NotBeNull();
        match2.Should().NotBeNull();
        match3.Should().NotBeNull();
    }

    [Fact]
    public void Match_Multiline_ChangesAnchorBehavior()
    {
        // Arrange
        var regex = new ColorerRegex("^line", RegexOptions.Multiline);

        // Act
        var match = regex.Match("first\nline2");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("line");
    }

    [Fact]
    public void Match_Singleline_DotMatchesNewline()
    {
        // Arrange
        var regex = new ColorerRegex("a.b", RegexOptions.Singleline);

        // Act
        var match = regex.Match("a\nb");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("a\nb");
    }

    #endregion

    #region Static Helper Tests

    [Fact]
    public void StaticMatch_MatchesCorrectly()
    {
        // Act
        var match = ColorerRegex.Match("abc123", @"\d+");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("123");
    }

    [Fact]
    public void StaticIsMatch_ReturnsCorrectResult()
    {
        // Act
        bool matches1 = ColorerRegex.IsMatch("abc123", @"\d+");
        bool matches2 = ColorerRegex.IsMatch("abcdef", @"\d+");

        // Assert
        matches1.Should().Be(true);
        matches2.Should().Be(false);
    }

    [Fact]
    public void StaticMatches_FindsAllMatches()
    {
        // Act
        var matches = ColorerRegex.Matches("a1 b2 c3", @"\d").ToList();

        // Assert
        matches.Count.Should().Be(3);
        matches[0].Value.Should().Be("1");
        matches[1].Value.Should().Be("2");
        matches[2].Value.Should().Be("3");
    }

    #endregion

    #region Matches Iterator Tests

    [Fact]
    public void Matches_FindsMultipleMatches()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+");

        // Act
        var matches = regex.Matches("a1 b22 c333").ToList();

        // Assert
        matches.Count.Should().Be(3);
        matches[0].Value.Should().Be("1");
        matches[1].Value.Should().Be("22");
        matches[2].Value.Should().Be("333");
    }

    [Fact]
    public void Matches_EmptyString_ReturnsNoMatches()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+");

        // Act
        var matches = regex.Matches("").ToList();

        // Assert
        matches.Count.Should().Be(0);
    }

    #endregion

    #region Complex Pattern Tests

    [Fact]
    public void Match_EmailPattern_MatchesEmail()
    {
        // Arrange
        var regex = new ColorerRegex(@"\w+@\w+\.\w+");

        // Act
        var match = regex.Match("Contact: user@example.com for info");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Match_HexColorPattern_MatchesHexColor()
    {
        // Arrange
        var regex = new ColorerRegex("#[0-9a-fA-F]{6}");

        // Act
        var match = regex.Match("Color: #FF5733 is nice");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("#FF5733");
    }

    [Fact]
    public void Match_IPv4Pattern_MatchesIPAddress()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");

        // Act
        var match = regex.Match("Server: 192.168.1.1");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("192.168.1.1");
    }

    #endregion

    #region Lookahead Tests

    [Fact]
    public void Match_PositiveLookahead_MatchesCorrectly()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+(?=px)");

        // Act
        var match1 = regex.Match("100px");
        var match2 = regex.Match("100em");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("100");

        match2.Should().BeNull();
    }

    [Fact]
    public void Match_NegativeLookahead_MatchesCorrectly1()
    {
        // Arrange
        var regex = new ColorerRegex(@"[a-z]+(?![A-Z])");

        // Act
        var match1 = regex.Match("helloW");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("hell");
    }

    [Fact]
    public void Match_NegativeLookahead_MatchesCorrectly()
    {
        // Arrange
        // Pattern: \d+(?!px) follows Perl semantics with greedy backtracking
        // On "100px": \d+ matches "100", lookahead sees "px" -> FAIL
        //             Backtrack: \d+ tries "10", lookahead sees "0px" -> SUCCESS
        // Result: matches "10" (correct Perl behavior)
        var regex = new ColorerRegex(@"\d+(?!px)");

        // Act
        var match1 = regex.Match("100em");
        var match2 = regex.Match("100px");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("100"); // No "px" after digits, matches full number

        // Perl semantic: Matches "10" due to backtracking (see PerlSemanticComparisonTest)
        match2.Should().NotBeNull();
        match2!.Value.Should().Be("10");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Match_StartIndex_MatchesFromPosition()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+");

        // Act
        var match = regex.Match("a1b2c3", 3);

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("2");
        match.Index.Should().Be(3);
    }

    [Fact]
    public void Match_VeryLongInput_HandlesCorrectly()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+");
        var input = new string('a', 10000) + "12345" + new string('b', 10000);

        // Act
        var match = regex.Match(input);

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("12345");
        match.Index.Should().Be(10000);
    }

    #endregion
}
