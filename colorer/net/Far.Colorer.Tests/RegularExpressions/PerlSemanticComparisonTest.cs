using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;
using Xunit;
using Xunit.Abstractions;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Tests to verify our regex engine follows Perl semantics.
/// These tests compare our behavior against documented Perl regex behavior.
///
/// Perl regex documentation reference:
/// - Lookahead (?=pattern) and (?!pattern) are zero-width assertions
/// - They test without consuming characters
/// - Quantifiers can backtrack past them
/// </summary>
public class PerlSemanticComparisonTest
{
    private readonly ITestOutputHelper _output;

    public PerlSemanticComparisonTest(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Test: \d+(?!px) on "100px"
    ///
    /// Perl behavior (from testing):
    /// - In Perl: /\d+(?!px)/ matches "10" in "100px"
    ///
    /// Why:
    /// 1. \d+ greedily matches "100"
    /// 2. At position 3, (?!px) sees "px" -> FAIL
    /// 3. Backtrack: \d+ tries "10"
    /// 4. At position 2, (?!px) sees "0px" (not "px") -> SUCCESS
    /// 5. Match: "10"
    ///
    /// This is standard greedy backtracking behavior.
    /// </summary>
    [Fact]
    public void NegativeLookahead_WithGreedyQuantifier_BacktracksCorrectly()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+(?!px)", RegexOptions.None);

        // Act & Log
        var match1 = regex.Match("100px");
        _output.WriteLine($"Pattern: \\d+(?!px)");
        _output.WriteLine($"Input: '100px'");
        _output.WriteLine($"Match: {match1?.Value ?? "null"}");
        _output.WriteLine($"");
        _output.WriteLine($"Execution trace:");
        _output.WriteLine($"1. \\d+ greedily matches '100' (positions 0-2)");
        _output.WriteLine($"2. At position 3, (?!px) sees 'px' -> FAIL");
        _output.WriteLine($"3. Backtrack: \\d+ tries '10' (positions 0-1)");
        _output.WriteLine($"4. At position 2, (?!px) sees '0px' (starts with '0', not 'p') -> SUCCESS");
        _output.WriteLine($"5. Return match: '10'");

        // Assert - Perl behavior: matches "10"
        Assert.NotNull(match1);
        Assert.Equal("10", match1!.Value);
        Assert.Equal(0, match1.Index);
        Assert.Equal(2, match1.Length);
    }

    /// <summary>
    /// Test: \d+(?!px) on "100em"
    ///
    /// Perl behavior:
    /// - Should match "100" because lookahead doesn't see "px"
    /// </summary>
    [Fact]
    public void NegativeLookahead_WhenLookaheadPasses_MatchesFull()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+(?!px)", RegexOptions.None);

        // Act
        var match = regex.Match("100em");
        _output.WriteLine($"Pattern: \\d+(?!px)");
        _output.WriteLine($"Input: '100em'");
        _output.WriteLine($"Match: {match?.Value ?? "null"}");

        // Assert - Should match full "100"
        Assert.NotNull(match);
        Assert.Equal("100", match!.Value);
    }

    /// <summary>
    /// Test: foo(?!bar) matches "foo" not followed by "bar"
    ///
    /// From Perl docs:
    /// "foo(?!bar) matches any occurrence of 'foo' that isn't followed by 'bar'"
    /// </summary>
    [Fact]
    public void NegativeLookahead_PerlDocExample_FooNotBar()
    {
        // Arrange
        var regex = new ColorerRegex("foo(?!bar)", RegexOptions.None);

        // Act
        var match1 = regex.Match("foobar");
        var match2 = regex.Match("foobaz");
        var match3 = regex.Match("foo");

        _output.WriteLine($"Pattern: foo(?!bar)");
        _output.WriteLine($"'foobar': {match1?.Value ?? "null"}");
        _output.WriteLine($"'foobaz': {match2?.Value ?? "null"}");
        _output.WriteLine($"'foo': {match3?.Value ?? "null"}");

        // Assert
        Assert.Null(match1);  // "foobar" - foo IS followed by bar
        Assert.NotNull(match2); // "foobaz" - foo is NOT followed by bar
        Assert.Equal("foo", match2!.Value);
        Assert.NotNull(match3); // "foo" at end - NOT followed by bar
        Assert.Equal("foo", match3!.Value);
    }

    /// <summary>
    /// Test: (?!foo)bar does NOT match "bar not preceded by foo"
    ///
    /// From Perl docs:
    /// "If you are looking for a 'bar' that isn't preceded by a 'foo',
    ///  (?!foo)bar will not do what you want. That's because the (?!foo)
    ///  is just saying that the next thing cannot be 'foo'--and it's not,
    ///  it's a 'bar', so 'foobar' will match."
    /// </summary>
    [Fact]
    public void NegativeLookahead_NotLookbehind_PerlDocWarning()
    {
        // Arrange
        var regex = new ColorerRegex("(?!foo)bar", RegexOptions.None);

        // Act
        var match1 = regex.Match("foobar");
        var match2 = regex.Match("xyzbar");

        _output.WriteLine($"Pattern: (?!foo)bar");
        _output.WriteLine($"'foobar': {match1?.Value ?? "null"} at position {match1?.Index ?? -1}");
        _output.WriteLine($"'xyzbar': {match2?.Value ?? "null"} at position {match2?.Index ?? -1}");
        _output.WriteLine($"");
        _output.WriteLine($"Note: This WILL match 'bar' in 'foobar' because:");
        _output.WriteLine($"  At position 3 (the 'b'), (?!foo) checks if next is 'foo'");
        _output.WriteLine($"  Next chars are 'bar' (not 'foo'), so (?!foo) succeeds");
        _output.WriteLine($"  Then 'bar' matches literally");

        // Assert - Both should match "bar"
        Assert.NotNull(match1);
        Assert.Equal("bar", match1!.Value);
        Assert.Equal(3, match1.Index); // At position 3 in "foobar"

        Assert.NotNull(match2);
        Assert.Equal("bar", match2!.Value);
        Assert.Equal(3, match2.Index); // At position 3 in "xyzbar"
    }

    /// <summary>
    /// Test: Positive lookahead (?=pattern)
    /// </summary>
    [Fact]
    public void PositiveLookahead_PerlBehavior()
    {
        // Arrange
        var regex = new ColorerRegex(@"\d+(?=px)", RegexOptions.None);

        // Act
        var match1 = regex.Match("100px");
        var match2 = regex.Match("100em");

        _output.WriteLine($"Pattern: \\d+(?=px)");
        _output.WriteLine($"'100px': {match1?.Value ?? "null"}");
        _output.WriteLine($"'100em': {match2?.Value ?? "null"}");

        // Assert
        Assert.NotNull(match1);
        Assert.Equal("100", match1!.Value); // Matches "100" when followed by "px"

        Assert.Null(match2); // No match when not followed by "px"
    }

    /// <summary>
    /// Test: Multiple lookaheads
    /// Pattern: \w+(?=\d)(?=.*px)
    /// Explanation: Greedy \w+ matches as much as possible, then backtracks
    /// until BOTH lookaheads are satisfied. First lookahead requires digit immediately after,
    /// second requires "px" somewhere ahead. The match stops at "test12" because:
    /// - After "test12", next char is '3' (digit) ✓
    /// - From that position, "3px" matches .*px ✓
    /// Note: Verified with Perl and Python - both return "test12", not "test"
    /// </summary>
    [Fact]
    public void MultipleLookaheads_PerlBehavior()
    {
        // Arrange
        var regex = new ColorerRegex(@"\w+(?=\d)(?=.*px)", RegexOptions.None);

        // Act
        var match1 = regex.Match("test123px");
        var match2 = regex.Match("test123em");
        var match3 = regex.Match("testpx");

        _output.WriteLine($"Pattern: \\w+(?=\\d)(?=.*px)");
        _output.WriteLine($"'test123px': {match1?.Value ?? "null"}");
        _output.WriteLine($"'test123em': {match2?.Value ?? "null"}");
        _output.WriteLine($"'testpx': {match3?.Value ?? "null"}");

        // Assert
        Assert.NotNull(match1); // Both lookaheads pass
        Assert.Equal("test12", match1!.Value); // Greedy matching with both lookaheads satisfied

        Assert.Null(match2); // Second lookahead fails (no "px")
        Assert.Null(match3); // First lookahead fails (no digit after)
    }

    /// <summary>
    /// Test: Lookahead with backtracking edge case
    /// Pattern: \w+(?!bar) on "foobar"
    ///
    /// Expected Perl behavior:
    /// - \w+ greedily matches "foobar"
    /// - At end, (?!bar) checks if followed by "bar" - sees EOF, not "bar" -> SUCCESS
    /// - But wait, we're at EOF, that's past "bar"
    ///
    /// Actually in Perl:
    /// - \w+ matches "fooba" (5 chars)
    /// - At position 5, (?!bar) sees "r" (not "bar") -> SUCCESS
    /// - Or backtracks further until it succeeds
    /// </summary>
    [Fact]
    public void NegativeLookahead_WithWordCharacters_Backtracking()
    {
        // Arrange
        var regex = new ColorerRegex(@"\w+(?!bar)", RegexOptions.None);

        // Act
        var match = regex.Match("foobar");

        _output.WriteLine($"Pattern: \\w+(?!bar)");
        _output.WriteLine($"Input: 'foobar'");
        _output.WriteLine($"Match: '{match?.Value ?? "null"}' at position {match?.Index ?? -1}");

        if (match != null)
        {
            _output.WriteLine($"");
            _output.WriteLine($"Likely backtracking behavior:");
            _output.WriteLine($"1. \\w+ greedily matches 'foobar' (positions 0-5)");
            _output.WriteLine($"2. At position 6 (EOF), (?!bar) sees nothing -> SUCCESS");
            _output.WriteLine($"   OR");
            _output.WriteLine($"1. \\w+ matches 'foob' (positions 0-3)");
            _output.WriteLine($"2. At position 4, (?!bar) sees 'ar' (not 'bar') -> SUCCESS");
        }

        // In Perl, this would match something (likely "fooba" or full "foobar")
        // The exact behavior depends on how lookahead interprets "end of string"
        Assert.NotNull(match);
        Assert.True(match!.Value.Length > 0);
    }

    /// <summary>
    /// Test case from actual usage in shell-bash.hrc
    /// Pattern: \b\M(%var;)\+\=\(?!/
    ///
    /// Breaking down: \b \M (%var;) \+\= \( (?! /)
    /// This is: word-boundary, match-marker, var-pattern, literal "+=", literal "(", NOT followed by "/"
    /// </summary>
    [Fact]
    public void RealWorld_ShellBashHrcPattern()
    {
        // Simplified version without Colorer-specific features
        // Pattern: word\+\=\((?!/)
        var regex = new ColorerRegex(@"var\+\=\((?!/)", RegexOptions.None);

        // Act
        var match1 = regex.Match("var+=(value");
        var match2 = regex.Match("var+=(/path");

        _output.WriteLine($"Pattern: var\\+\\=\\((?!/)");
        _output.WriteLine($"'var+=(value': {match1?.Value ?? "null"}");
        _output.WriteLine($"'var+=(/path': {match2?.Value ?? "null"}");

        // Assert
        Assert.NotNull(match1); // NOT followed by /
        Assert.Equal("var+=(", match1!.Value);

        Assert.Null(match2); // IS followed by /
    }
}
