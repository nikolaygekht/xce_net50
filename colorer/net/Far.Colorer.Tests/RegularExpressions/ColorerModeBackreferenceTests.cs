using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Enums;
using Far.Colorer.RegularExpressions.Internal;
using Xunit;
using AwesomeAssertions;
using Xunit.Abstractions;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Tests for COLORERMODE backreferences - both cross-pattern (\y, \Y) and named backreferences.
/// These features are critical for HRC syntax highlighting files.
///
/// Coverage: Lines 183-184, 329-439, 468-493
/// </summary>
public class ColorerModeBackreferenceTests
{
    private readonly ITestOutputHelper _output;

    public ColorerModeBackreferenceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Cross-Pattern Backreferences (\y) - Lines 329-350, 183-184

    [Fact]
    public void CrossPatternBackreference_Basic_MatchesCapturedText()
    {
        // Arrange - Lines 329-350: ReBkTrace implementation
        // HRC Reference: asm.hrc - <block start="/(COMMENT) (.)/i" end="/\y2/"/>
        // This tests the pattern: start captures delimiter, end matches same delimiter

        var startPattern = new ColorerRegex(@"(COMMENT) (.)", RegexOptions.IgnoreCase);
        var endPattern = new ColorerRegex(@"\y2", RegexOptions.None);

        // Act - First pattern captures the delimiter
        var startMatch = startPattern.Match("COMMENT #");
        startMatch.Should().NotBeNull();
        startMatch!.Groups[2].Index.Should().Be(8);
        startMatch.GetGroupValue(2).Should().Be("#");

        // Set backreference for second pattern (lines 183-184)
        endPattern.SetBackReference("COMMENT #", startMatch);

        // Second pattern should match the same delimiter
        var endMatch = endPattern.Match("#");

        // Assert
        endMatch.Should().NotBeNull();
        endMatch!.Value.Should().Be("#");
    }

    [Fact]
    public void CrossPatternBackreference_MultiCharDelimiter()
    {
        // Arrange - Test with multi-character captured text
        var startPattern = new ColorerRegex(@"(BEGIN) (.*)", RegexOptions.None);
        var endPattern = new ColorerRegex(@"\y2", RegexOptions.None);

        // Act
        var startMatch = startPattern.Match("BEGIN END");
        startMatch.Should().NotBeNull();
        startMatch!.GetGroupValue(2).Should().Be("END");

        endPattern.SetBackReference("BEGIN END", startMatch);
        var endMatch = endPattern.Match("END");

        // Assert
        endMatch.Should().NotBeNull();
        endMatch!.Value.Should().Be("END");
    }

    [Fact]
    public void CrossPatternBackreference_NoBackTraceSet_NoMatch()
    {
        // Arrange - Lines 330-334: if (backStr == null || backTrace == null)
        var pattern = new ColorerRegex(@"\y2", RegexOptions.None);

        // Act - Try to match without setting backreference
        var match = pattern.Match("test");

        // Assert - Should fail because no backTrace is set
        match.Should().BeNull();
    }

    [Fact]
    public void CrossPatternBackreference_InvalidGroupNumber_NoMatch()
    {
        // Arrange - Lines 330-334: sv == -1 check
        var startPattern = new ColorerRegex(@"(a)", RegexOptions.None);
        var endPattern = new ColorerRegex(@"\y9", RegexOptions.None); // Group 9 doesn't exist

        // Act
        var startMatch = startPattern.Match("a");
        startMatch.Should().NotBeNull();

        endPattern.SetBackReference("a", startMatch);
        var endMatch = endPattern.Match("a");

        // Assert - Group 9 doesn't exist, should fail
        endMatch.Should().BeNull();
    }

    [Fact]
    public void CrossPatternBackreference_MismatchedText_NoMatch()
    {
        // Arrange - Lines 340-347: Character mismatch in loop
        var startPattern = new ColorerRegex(@"(COMMENT) (.)", RegexOptions.None);
        var endPattern = new ColorerRegex(@"\y2", RegexOptions.None);

        // Act
        var startMatch = startPattern.Match("COMMENT #");
        startMatch.Should().NotBeNull();
        startMatch!.GetGroupValue(2).Should().Be("#");

        endPattern.SetBackReference("COMMENT #", startMatch);
        var endMatch = endPattern.Match("$"); // Different character

        // Assert - Should not match because "$" != "#"
        endMatch.Should().BeNull();
    }

    [Fact]
    public void CrossPatternBackreference_AtEndOfString_NoMatch()
    {
        // Arrange - Lines 340: toParse >= end check
        var startPattern = new ColorerRegex(@"(COMMENT) (..)", RegexOptions.None); // 2 chars
        var endPattern = new ColorerRegex(@"\y2", RegexOptions.None);

        // Act
        var startMatch = startPattern.Match("COMMENT ##");
        startMatch.Should().NotBeNull();
        startMatch!.GetGroupValue(2).Should().Be("##");

        endPattern.SetBackReference("COMMENT ##", startMatch);
        var endMatch = endPattern.Match("#"); // Only 1 char, need 2

        // Assert - Not enough characters to match
        endMatch.Should().BeNull();
    }

    #endregion

    #region Cross-Pattern Backreferences Case-Insensitive (\Y) - Lines 352-438

    [Fact]
    public void CrossPatternBackreference_CaseInsensitive_Matches()
    {
        // Arrange - Lines 352-438: ReBkTraceN implementation
        var startPattern = new ColorerRegex(@"(BEGIN) (.*)", RegexOptions.None);
        var endPattern = new ColorerRegex(@"\Y2", RegexOptions.None);

        // Act
        var startMatch = startPattern.Match("BEGIN end");
        startMatch.Should().NotBeNull();
        startMatch!.GetGroupValue(2).Should().Be("end");

        endPattern.SetBackReference("BEGIN end", startMatch);
        var endMatch = endPattern.Match("END"); // Different case

        // Assert - \Y is case-insensitive
        endMatch.Should().NotBeNull();
        endMatch!.Value.Should().Be("END");
    }

    [Fact]
    public void CrossPatternBackreference_CaseInsensitive_NoBackTrace()
    {
        // Arrange - Lines 354-358: Null check for \Y
        var pattern = new ColorerRegex(@"\Y2", RegexOptions.None);

        // Act
        var match = pattern.Match("test");

        // Assert
        match.Should().BeNull();
    }

    #endregion

    #region Named Cross-Pattern Backreferences - Lines 381-438

    [Fact]
    public void CrossPatternBackreference_Named_Basic()
    {
        // Arrange - Lines 381-410: ReBkTraceName implementation
        // In C++, setBackRE is called before compilation to resolve group numbers
        var startPattern = new ColorerRegex(@"(?{delim}.)", RegexOptions.None);
        var endPattern = ColorerRegex.CreateWithBackRE(@"\y{delim}", startPattern, RegexOptions.None);

        // Act
        var startMatch = startPattern.Match("#");
        startMatch.Should().NotBeNull();
        startMatch!.GetGroupValue("delim").Should().Be("#");

        endPattern.SetBackReference("#", startMatch);
        var endMatch = endPattern.Match("#");

        // Assert
        endMatch.Should().NotBeNull();
        endMatch!.Value.Should().Be("#");
    }

    [Fact]
    public void CrossPatternBackreference_Named_CaseInsensitive()
    {
        // Arrange - Lines 411-438: ReBkTraceNName implementation
        var startPattern = new ColorerRegex(@"(?{word}\w+)", RegexOptions.None);
        var endPattern = ColorerRegex.CreateWithBackRE(@"\Y{word}", startPattern, RegexOptions.None);

        // Act
        var startMatch = startPattern.Match("Hello");
        startMatch.Should().NotBeNull();
        startMatch!.GetGroupValue("word").Should().Be("Hello");

        endPattern.SetBackReference("Hello", startMatch);
        var endMatch = endPattern.Match("HELLO"); // Different case

        // Assert - \Y{name} is case-insensitive
        endMatch.Should().NotBeNull();
        endMatch!.Value.Should().Be("HELLO");
    }

    [Fact]
    public void CrossPatternBackreference_Named_NotFound_ThrowsAtCompileTime()
    {
        // Arrange - With compile-time resolution, group name validation happens during compilation
        var startPattern = new ColorerRegex(@"(?{foo}a)", RegexOptions.None);

        // Act & Assert - Should throw when group "bar" doesn't exist in startPattern
        Action act = () => ColorerRegex.CreateWithBackRE(@"\y{bar}", startPattern, RegexOptions.None);
        act.Should().Throw<RegexSyntaxException>().WithMessage("*Named group 'bar' not found*");
    }

    #endregion

    #region Named Backreferences Within Same Pattern - Lines 468-493

    [Fact]
    public void NamedBackreference_SamePattern_Matches()
    {
        // Arrange - Lines 468-493: ReBkBrackName implementation
        // This tests backreferences to named groups within the same pattern
        // Correct syntax is \p{name}, not \k{name}
        var regex = new ColorerRegex(@"(?{word}\w+)\s+\p{word}", RegexOptions.None);

        // Act
        var match = regex.Match("hello hello");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("hello hello");
        match.GetGroupValue("word").Should().Be("hello");
    }

    [Fact]
    public void NamedBackreference_SamePattern_NoMatch()
    {
        // Arrange
        var regex = new ColorerRegex(@"(?{word}\w+)\s+\p{word}", RegexOptions.None);

        // Act
        var match = regex.Match("hello world");

        // Assert - Words don't match
        match.Should().BeNull();
    }

    [Fact]
    public void NamedBackreference_SamePattern_EmptyGroup()
    {
        // Arrange - Test with optional group that captures empty string
        var regex = new ColorerRegex(@"(?{opt}a?)\p{opt}", RegexOptions.None);

        // Act
        var match = regex.Match("");

        // Assert - Both capture and backreference are empty
        match.Should().NotBeNull();
        match!.Value.Should().Be("");
        match.GetGroupValue("opt").Should().Be("");
    }

    [Fact]
    public void NamedBackreference_SamePattern_GroupNotFound()
    {
        // Arrange - Lines 471-475: Group name not found
        // C++ behavior: return EError::ESYNTAX if getBracketNo returns -1
        // Should throw exception at compile time, not match time

        // Act & Assert - Should throw RegexSyntaxException
        Action act = () => new ColorerRegex(@"(?{word}\w+)\s+\p{other}", RegexOptions.None);
        act.Should().Throw<RegexSyntaxException>().WithMessage("Unknown group name: other");
    }

    [Fact]
    public void NamedBackreference_SamePattern_CaseSensitive()
    {
        // Arrange
        var regex = new ColorerRegex(@"(?{word}[A-Z]+)\s+\p{word}", RegexOptions.None);

        // Act
        var match1 = regex.Match("HELLO HELLO");
        var match2 = regex.Match("HELLO hello");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("HELLO HELLO");

        match2.Should().BeNull(); // Case mismatch
    }

    [Fact]
    public void NamedBackreference_SamePattern_MultipleGroups()
    {
        // Arrange - Multiple named groups with backreferences
        var regex = new ColorerRegex(@"(?{a}\w+)-(?{b}\w+)-\p{a}-\p{b}", RegexOptions.None);

        // Act
        var match = regex.Match("foo-bar-foo-bar");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("foo-bar-foo-bar");
        match.GetGroupValue("a").Should().Be("foo");
        match.GetGroupValue("b").Should().Be("bar");
    }

    #endregion
}
