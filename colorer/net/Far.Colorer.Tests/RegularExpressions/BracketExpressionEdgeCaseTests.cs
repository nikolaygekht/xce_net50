using Far.Colorer.RegularExpressions.Internal;
using Xunit;
using AwesomeAssertions;
using System;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Edge case tests for bracket expressions with ] character
/// Validates proper handling of ] in various positions
/// </summary>
public unsafe class BracketExpressionEdgeCaseTests
{
    #region Test Case 1: []abc] - ] at start of class

    [Fact]
    public void BracketStartWithClosing_MatchesCorrectly()
    {
        // Pattern: []abc] means character class containing: ], a, b, c
        var regex = new ColorerRegex(@"[]abc]");

        // Should match ]
        var match1 = regex.Match("]");
        match1.Should().NotBeNull("Should match ']'");
        match1!.Value.Should().Be("]");

        // Should match a
        var match2 = regex.Match("a");
        match2.Should().NotBeNull("Should match 'a'");
        match2!.Value.Should().Be("a");

        // Should match b
        var match3 = regex.Match("b");
        match3.Should().NotBeNull("Should match 'b'");
        match3!.Value.Should().Be("b");

        // Should match c
        var match4 = regex.Match("c");
        match4.Should().NotBeNull("Should match 'c'");
        match4!.Value.Should().Be("c");

        // Should NOT match d
        var match5 = regex.Match("d");
        match5.Should().BeNull("Should NOT match 'd'");
    }

    [Fact]
    public void BracketStartWithClosing_InString()
    {
        // Pattern: []abc]+ should match sequences of ], a, b, c
        var regex = new ColorerRegex(@"[]abc]+");

        var match = regex.Match("test]abc]end");
        match.Should().NotBeNull();
        match!.Value.Should().Be("]abc]");
    }

    #endregion

    #region Test Case 2: []aaa - Unclosed bracket (should be error)

    [Fact]
    public void UnterminatedBracket_ThrowsException()
    {
        // Pattern: []aaa has no closing ] after the content
        // The ] after [ is part of the class, so we need another ] to close it
        Action act = () => { var regex = new ColorerRegex(@"[]aaa"); };

        // This should throw because the character class is never closed
        act.Should().Throw<Exception>("Unterminated character class should throw exception");
    }

    [Fact]
    public void UnterminatedBracket_Simple_ThrowsException()
    {
        // Pattern: [abc (no closing ])
        Action act = () => { var regex = new ColorerRegex(@"[abc"); };

        act.Should().Throw<Exception>("Unterminated character class should throw exception");
    }

    [Fact]
    public void UnterminatedBracket_WithNegation_ThrowsException()
    {
        // Pattern: [^abc (no closing ])
        Action act = () => { var regex = new ColorerRegex(@"[^abc"); };

        act.Should().Throw<Exception>("Unterminated negated character class should throw exception");
    }

    #endregion

    #region Test Case 3: [a\]bc] - Escaped ] in middle

    [Fact]
    public void EscapedClosingInMiddle_MatchesCorrectly()
    {
        // Pattern: [a\]bc] means character class containing: a, ], b, c
        var regex = new ColorerRegex(@"[a\]bc]");

        // Should match a
        var match1 = regex.Match("a");
        match1.Should().NotBeNull("Should match 'a'");

        // Should match ]
        var match2 = regex.Match("]");
        match2.Should().NotBeNull("Should match ']'");

        // Should match b
        var match3 = regex.Match("b");
        match3.Should().NotBeNull("Should match 'b'");

        // Should match c
        var match4 = regex.Match("c");
        match4.Should().NotBeNull("Should match 'c'");

        // Should NOT match d
        var match5 = regex.Match("d");
        match5.Should().BeNull("Should NOT match 'd'");
    }

    [Fact]
    public void EscapedClosing_MultiplePlaces()
    {
        // Pattern: [\]a\]b\]] means character class containing: ], a, ], b, ]
        // First ] is literal (at start), then \] twice, then \] at end
        var regex = new ColorerRegex(@"[\]a\]b\]]");

        // Should match ]
        var match1 = regex.Match("]");
        match1.Should().NotBeNull("Should match ']'");

        // Should match a
        var match2 = regex.Match("a");
        match2.Should().NotBeNull("Should match 'a'");

        // Should match b
        var match3 = regex.Match("b");
        match3.Should().NotBeNull("Should match 'b'");

        // Should NOT match c
        var match4 = regex.Match("c");
        match4.Should().BeNull("Should NOT match 'c'");
    }

    #endregion

    #region Test Case 4: [ab][cd] - Multiple bracket expressions

    [Fact]
    public void MultipleBracketExpressions_Sequential()
    {
        // Pattern: [ab][cd] means [ab] followed by [cd]
        var regex = new ColorerRegex(@"[ab][cd]");

        // Should match "ac"
        var match1 = regex.Match("ac");
        match1.Should().NotBeNull("Should match 'ac'");
        match1!.Value.Should().Be("ac");

        // Should match "ad"
        var match2 = regex.Match("ad");
        match2.Should().NotBeNull("Should match 'ad'");
        match2!.Value.Should().Be("ad");

        // Should match "bc"
        var match3 = regex.Match("bc");
        match3.Should().NotBeNull("Should match 'bc'");
        match3!.Value.Should().Be("bc");

        // Should match "bd"
        var match4 = regex.Match("bd");
        match4.Should().NotBeNull("Should match 'bd'");
        match4!.Value.Should().Be("bd");

        // Should NOT match "ab"
        var match5 = regex.Match("ab");
        match5.Should().BeNull("Should NOT match 'ab'");

        // Should NOT match "cd"
        var match6 = regex.Match("cd");
        match6.Should().BeNull("Should NOT match 'cd'");
    }

    [Fact]
    public void MultipleBracketExpressions_WithClosingBracket()
    {
        // Pattern: []ab][cd] means []ab] followed by [cd]
        // First class: ], a, b
        // Second class: c, d
        var regex = new ColorerRegex(@"[]ab][cd]");

        // Should match "]c"
        var match1 = regex.Match("]c");
        match1.Should().NotBeNull("Should match ']c'");
        match1!.Value.Should().Be("]c");

        // Should match "ad"
        var match2 = regex.Match("ad");
        match2.Should().NotBeNull("Should match 'ad'");
        match2!.Value.Should().Be("ad");

        // Should match "bc"
        var match3 = regex.Match("bc");
        match3.Should().NotBeNull("Should match 'bc'");
        match3!.Value.Should().Be("bc");

        // Should NOT match "ab" (a is in first class, b is in first class, but need second class char)
        var match4 = regex.Match("ab");
        match4.Should().BeNull("Should NOT match 'ab'");
    }

    [Fact]
    public void MultipleBracketExpressions_Complex()
    {
        // Pattern: [a-c][0-2][x-z] - three sequential character classes
        var regex = new ColorerRegex(@"[a-c][0-2][x-z]");

        // Should match "a0x"
        var match1 = regex.Match("a0x");
        match1.Should().NotBeNull("Should match 'a0x'");
        match1!.Value.Should().Be("a0x");

        // Should match "c2z"
        var match2 = regex.Match("c2z");
        match2.Should().NotBeNull("Should match 'c2z'");
        match2!.Value.Should().Be("c2z");

        // Should match "b1y"
        var match3 = regex.Match("b1y");
        match3.Should().NotBeNull("Should match 'b1y'");
        match3!.Value.Should().Be("b1y");

        // Should NOT match "a3x" (3 not in [0-2])
        var match4 = regex.Match("a3x");
        match4.Should().BeNull("Should NOT match 'a3x'");
    }

    #endregion

    #region Test Case 5: [^]abc] - Negated class starting with ]

    [Fact]
    public void NegatedBracketStartWithClosing_MatchesCorrectly()
    {
        // Pattern: [^]abc] means NOT (], a, b, c)
        var regex = new ColorerRegex(@"[^]abc]");

        // Should match d
        var match1 = regex.Match("d");
        match1.Should().NotBeNull("Should match 'd'");

        // Should match x
        var match2 = regex.Match("x");
        match2.Should().NotBeNull("Should match 'x'");

        // Should NOT match ]
        var match3 = regex.Match("]");
        match3.Should().BeNull("Should NOT match ']'");

        // Should NOT match a
        var match4 = regex.Match("a");
        match4.Should().BeNull("Should NOT match 'a'");

        // Should NOT match b
        var match5 = regex.Match("b");
        match5.Should().BeNull("Should NOT match 'b'");

        // Should NOT match c
        var match6 = regex.Match("c");
        match6.Should().BeNull("Should NOT match 'c'");
    }

    #endregion

    #region Additional Edge Cases

    [Fact]
    public void EmptyBracket_OnlyClosing_MatchesSingleChar()
    {
        // Pattern: []] means character class containing only ]
        var regex = new ColorerRegex(@"[]]+");

        var match = regex.Match("test]]]end");
        match.Should().NotBeNull();
        match!.Value.Should().Be("]]]");
    }

    [Fact]
    public void BracketWithDash_AsRange()
    {
        // Pattern: []-a] means range from ] (ASCII 93) to a (ASCII 97)
        // This includes: ] (93), ^ (94), _ (95), ` (96), a (97)
        var regex = new ColorerRegex(@"[]-a]");

        var match1 = regex.Match("]");
        match1.Should().NotBeNull("Should match ']'");

        var match2 = regex.Match("^");
        match2.Should().NotBeNull("Should match '^'");

        var match3 = regex.Match("_");
        match3.Should().NotBeNull("Should match '_'");

        var match4 = regex.Match("`");
        match4.Should().NotBeNull("Should match '`'");

        var match5 = regex.Match("a");
        match5.Should().NotBeNull("Should match 'a'");

        // Should NOT match - (before range)
        var match6 = regex.Match("-");
        match6.Should().BeNull("Should NOT match '-'");

        // Should NOT match b (after range)
        var match7 = regex.Match("b");
        match7.Should().BeNull("Should NOT match 'b'");
    }

    [Fact]
    public void BracketWithDash_Literal()
    {
        // Pattern: []a-] means ], a, - (dash is literal at end)
        var regex = new ColorerRegex(@"[]a-]");

        var match1 = regex.Match("]");
        match1.Should().NotBeNull("Should match ']'");

        var match2 = regex.Match("a");
        match2.Should().NotBeNull("Should match 'a'");

        var match3 = regex.Match("-");
        match3.Should().NotBeNull("Should match '-' (literal at end)");

        var match4 = regex.Match("b");
        match4.Should().BeNull("Should NOT match 'b'");
    }

    [Fact]
    public void BracketWithRange_AfterClosing()
    {
        // Pattern: []a-z] means ], a, b, c, ..., z (] followed by range)
        var regex = new ColorerRegex(@"[]a-z]");

        var match1 = regex.Match("]");
        match1.Should().NotBeNull("Should match ']'");

        var match2 = regex.Match("m");
        match2.Should().NotBeNull("Should match 'm'");

        var match3 = regex.Match("0");
        match3.Should().BeNull("Should NOT match '0'");
    }

    #endregion
}
