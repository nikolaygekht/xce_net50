using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;
using Xunit;
using AwesomeAssertions;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// PCRE2 compliance test suite - tests derived from PCRE2 testdata/testinput1
/// Uses verbatim strings (@"...") to preserve PCRE2 test data exactly as-is.
///
/// Test categories covered:
/// - Basic pattern matching
/// - Case-insensitive matching
/// - Escape sequences (\t, \n, \r, \f, \a, \e, \x, octal)
/// - Quantifiers (*, +, ?, {n}, {n,m}, non-greedy)
/// - Anchors (^, $)
/// - Character classes ([abc], [^abc], [a-z])
/// - Capturing groups
/// - Backreferences (\1, \2, etc.)
/// - Alternation (|)
/// - Lookahead/lookbehind
///
/// Test file source: PCRE2 /testdata/testinput1
/// </summary>
public unsafe class PCRE2ComplianceTests
{
    #region Test 1: Basic Literal Matching (testinput1 lines 11-18)

    [Fact]
    public void PCRE2_Test001_BasicLiteralMatch()
    {
        // Pattern from PCRE2: /the quick brown fox/
        var regex = new ColorerRegex(@"the quick brown fox");

        // PCRE2 test: "the quick brown fox" should match
        var match1 = regex.Match(@"the quick brown fox");
        match1.Should().NotBeNull();
        match1!.Value.Should().Be(@"the quick brown fox");

        // PCRE2 test: "What do you know about the quick brown fox?" should match
        var match2 = regex.Match(@"What do you know about the quick brown fox?");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be(@"the quick brown fox");
    }

    [Fact]
    public void PCRE2_Test002_BasicLiteralNoMatch()
    {
        // Pattern from PCRE2: /the quick brown fox/ (case-sensitive)
        var regex = new ColorerRegex(@"the quick brown fox");

        // PCRE2 test: "\= Expect no match" - "The quick brown FOX" should NOT match
        var match1 = regex.Match(@"The quick brown FOX");
        match1.Should().BeNull();

        // PCRE2 test: "What do you know about THE QUICK BROWN FOX?" should NOT match
        var match2 = regex.Match(@"What do you know about THE QUICK BROWN FOX?");
        match2.Should().BeNull();
    }

    #endregion

    #region Test 2: Case-Insensitive Matching (testinput1 lines 20-24)

    [Fact]
    public void PCRE2_Test003_CaseInsensitiveMatch()
    {
        // Pattern from PCRE2: /The quick brown fox/i
        var regex = new ColorerRegex(@"The quick brown fox", RegexOptions.IgnoreCase);

        // All these should match with /i flag
        var match1 = regex.Match(@"the quick brown fox");
        match1.Should().NotBeNull();
        match1!.Value.Should().Be(@"the quick brown fox");

        var match2 = regex.Match(@"The quick brown FOX");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be(@"The quick brown FOX");

        var match3 = regex.Match(@"What do you know about the quick brown fox?");
        match3.Should().NotBeNull();
        match3!.Value.Should().Be(@"the quick brown fox");

        var match4 = regex.Match(@"What do you know about THE QUICK BROWN FOX?");
        match4.Should().NotBeNull();
        match4!.Value.Should().Be(@"THE QUICK BROWN FOX");
    }

    #endregion

    #region Test 3: Escape Sequences (testinput1 lines 28-30)

    [Fact]
    public void PCRE2_Test004_EscapeSequences()
    {
        // Pattern from PCRE2: /abcd\t\n\r\f\a\e\071\x3b\$\\\?caxyz/
        // This tests: tab, newline, return, formfeed, bell, escape, octal, hex, escaped special chars
        var regex = new ColorerRegex(@"abcd\t\n\r\f\a\e\071\x3b\$\\\?caxyz");

        // Build expected input string:
        // abcd + tab + newline + return + formfeed + bell + escape + '9'(octal 071) + ';'(hex 3b) + $ + \ + ? + caxyz
        // NOTE: \x1b + "9" not "\x1b9" (which would be single char U+01B9)
        string input = "abcd\t\n\r\f\a\x1b" + "9;$\\?caxyz";

        var match = regex.Match(input);
        match.Should().NotBeNull();
        match!.Value.Should().Be(input);
    }

    #endregion

    #region Test 4: Complex Quantifiers (testinput1 lines 32-61)

    [Fact]
    public void PCRE2_Test005_ComplexQuantifiers()
    {
        // Pattern from PCRE2: /a*abc?xyz+pqr{3}ab{2,}xy{4,5}pq{0,6}AB{0,}zz/
        // This tests: *, ?, +, {n}, {n,}, {n,m}
        var regex = new ColorerRegex(@"a*abc?xyz+pqr{3}ab{2,}xy{4,5}pq{0,6}AB{0,}zz");

        // Valid matches from PCRE2 testinput1
        var validInputs = new[]
        {
            @"abxyzpqrrrabbxyyyypqAzz",
            @"aabxyzpqrrrabbxyyyypqAzz",
            @"aaabxyzpqrrrabbxyyyypqAzz",
            @"aaaabxyzpqrrrabbxyyyypqAzz",
            @"abcxyzpqrrrabbxyyyypqAzz",
            @"aabcxyzpqrrrabbxyyyypqAzz",
            @"aaabcxyzpqrrrabbxyyyypAzz",
            @"aaabcxyzpqrrrabbxyyyypqAzz",
            @"aaabcxyzpqrrrabbxyyyypqqAzz",
            @"aaabcxyzpqrrrabbxyyyypqqqAzz",
            @"aaabcxyzpqrrrabbxyyyypqqqqAzz",
            @"aaabcxyzpqrrrabbxyyyypqqqqqAzz",
            @"aaabcxyzpqrrrabbxyyyypqqqqqqAzz",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
        }
    }

    [Fact]
    public void PCRE2_Test006_ComplexQuantifiers_NoMatch()
    {
        // Pattern from PCRE2: /a*abc?xyz+pqr{3}ab{2,}xy{4,5}pq{0,6}AB{0,}zz/
        var regex = new ColorerRegex(@"a*abc?xyz+pqr{3}ab{2,}xy{4,5}pq{0,6}AB{0,}zz");

        // Invalid matches from PCRE2 testinput1 (should NOT match)
        var invalidInputs = new[]
        {
            @"abxyzpqrrabbxyyyypqAzz",  // pqrr (only 2 r's, need 3)
            @"abxyzpqrrrrabbxyyyypqAzz",  // pqrrrr (4 r's, but need exactly 3)
            @"abxyzpqrrrabxyyyypqAzz",  // ab (only 1 b, need 2+)
            @"aaabcxyzpqrrrabbxyyyypqqqqqqqAzz",  // pqqqqqqq (7 q's, max is 6)
        };

        foreach (var input in invalidInputs)
        {
            var match = regex.Match(input);
            match.Should().BeNull($"Pattern should NOT match '{input}'");
        }
    }

    #endregion

    #region Test 5: Anchors - Start of Line (testinput1 lines 63-70)

    [Fact]
    public void PCRE2_Test007_AnchorStartOfLine()
    {
        // Pattern from PCRE2: /^(abc){1,2}zz/
        var regex = new ColorerRegex(@"^(abc){1,2}zz");

        // Should match at start
        var match1 = regex.Match(@"abczz");
        match1.Should().NotBeNull();
        match1!.Value.Should().Be(@"abczz");

        var match2 = regex.Match(@"abcabczz");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be(@"abcabczz");
    }

    [Fact]
    public void PCRE2_Test008_AnchorStartOfLine_NoMatch()
    {
        // Pattern from PCRE2: /^(abc){1,2}zz/
        var regex = new ColorerRegex(@"^(abc){1,2}zz");

        // Should NOT match - doesn't start with pattern
        var match1 = regex.Match(@"zz");
        match1.Should().BeNull();

        var match2 = regex.Match(@"abcabcabczz");  // 3 repetitions, max is 2
        match2.Should().BeNull();

        var match3 = regex.Match(@">>abczz");  // Not at start
        match3.Should().BeNull();
    }

    #endregion

    #region Test 6: Alternation with Non-Greedy Quantifiers (testinput1 lines 72-82)

    [Fact]
    public void PCRE2_Test009_AlternationNonGreedy()
    {
        // Pattern from PCRE2: /^(b+?|a){1,2}?c/
        var regex = new ColorerRegex(@"^(b+?|a){1,2}?c");

        var validInputs = new[]
        {
            @"bc",
            @"bbc",
            @"bbbc",
            @"bac",
            @"bbac",
            @"aac",
            @"abbbbbbbbbbbc",
            @"bbbbbbbbbbbac",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
        }
    }

    [Fact]
    public void PCRE2_Test010_AlternationNonGreedy_NoMatch()
    {
        // Pattern from PCRE2: /^(b+?|a){1,2}?c/
        var regex = new ColorerRegex(@"^(b+?|a){1,2}?c");

        // Should NOT match
        var invalidInputs = new[]
        {
            @"aaac",  // aaa is 3 repetitions, max is 2
            @"abbbbbbbbbbbac",  // Can't match this pattern
        };

        foreach (var input in invalidInputs)
        {
            var match = regex.Match(input);
            match.Should().BeNull($"Pattern should NOT match '{input}'");
        }
    }

    #endregion

    #region Test 7: Alternation with Greedy Quantifiers (testinput1 lines 84-96)

    [Fact]
    public void PCRE2_Test011_AlternationGreedy()
    {
        // Pattern from PCRE2: /^(b+|a){1,2}c/
        var regex = new ColorerRegex(@"^(b+|a){1,2}c");

        var validInputs = new[]
        {
            @"bc",
            @"bbc",
            @"bbbc",
            @"bac",
            @"bbac",
            @"aac",
            @"abbbbbbbbbbbc",
            @"bbbbbbbbbbbac",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
        }
    }

    #endregion

    #region Test 8: Bracket Expressions (testinput1 lines 132-152)

    [Fact]
    public void PCRE2_Test012_BracketWithClosingBracket()
    {
        // Pattern from PCRE2: /^[ab\]cde]/
        // Character class containing: a, b, ], c, d, e
        var regex = new ColorerRegex(@"^[ab\]cde]");

        var validInputs = new[]
        {
            @"athing",
            @"bthing",
            @"]thing",
            @"cthing",
            @"dthing",
            @"ething",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
            match!.Value.Should().HaveLength(1);
        }
    }

    [Fact]
    public void PCRE2_Test013_BracketWithClosingBracket_NoMatch()
    {
        // Pattern from PCRE2: /^[ab\]cde]/
        var regex = new ColorerRegex(@"^[ab\]cde]");

        var invalidInputs = new[]
        {
            @"fthing",
            @"[thing",
            @"\thing",
        };

        foreach (var input in invalidInputs)
        {
            var match = regex.Match(input);
            match.Should().BeNull($"Pattern should NOT match '{input}'");
        }
    }

    [Fact]
    public void PCRE2_Test014_BracketStartWithClosing()
    {
        // Pattern from PCRE2: /^[]cde]/
        // Character class containing: ], c, d, e
        var regex = new ColorerRegex(@"^[]cde]");

        var validInputs = new[]
        {
            @"]thing",
            @"cthing",
            @"dthing",
            @"ething",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
        }

        // Should NOT match
        var noMatch = regex.Match(@"athing");
        noMatch.Should().BeNull();
    }

    [Fact]
    public void PCRE2_Test015_NegatedBracketWithClosing()
    {
        // Pattern from PCRE2: /^[^ab\]cde]/
        // Negated character class: NOT (a, b, ], c, d, e)
        var regex = new ColorerRegex(@"^[^ab\]cde]");

        var validInputs = new[]
        {
            @"fthing",
            @"[thing",
            @"\thing",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
        }

        // Should NOT match
        var invalidInputs = new[]
        {
            @"athing",
            @"bthing",
            @"]thing",
            @"cthing",
            @"dthing",
            @"ething",
        };

        foreach (var input in invalidInputs)
        {
            var match = regex.Match(input);
            match.Should().BeNull($"Pattern should NOT match '{input}'");
        }
    }

    #endregion

    #region Test 9: Digit Character Class (testinput1 lines 169-184)

    [Fact]
    public void PCRE2_Test016_DigitsWithAnchor()
    {
        // Pattern from PCRE2: /^[0-9]+$/
        var regex = new ColorerRegex(@"^[0-9]+$");

        var validInputs = new[]
        {
            @"0",
            @"1",
            @"2",
            @"3",
            @"4",
            @"5",
            @"6",
            @"7",
            @"8",
            @"9",
            @"10",
            @"100",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
            match!.Value.Should().Be(input);
        }

        // Should NOT match
        var noMatch = regex.Match(@"abc");
        noMatch.Should().BeNull();
    }

    #endregion

    #region Test 10: Dot and Quantifiers (testinput1 lines 186-189)

    [Fact]
    public void PCRE2_Test017_DotWithQuantifier()
    {
        // Pattern from PCRE2: /^.*nter/
        var regex = new ColorerRegex(@"^.*nter");

        var validInputs = new[]
        {
            @"enter",
            @"inter",
            @"uponter",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
            match!.Value.Should().Be(input);
        }
    }

    #endregion

    #region Test 11: Quantified Pattern with End Anchor (testinput1 lines 191-201)

    [Fact]
    public void PCRE2_Test018_QuantifiedWithEndAnchor()
    {
        // Pattern from PCRE2: /^xxx[0-9]+$/
        var regex = new ColorerRegex(@"^xxx[0-9]+$");

        var match1 = regex.Match(@"xxx0");
        match1.Should().NotBeNull();
        match1!.Value.Should().Be(@"xxx0");

        var match2 = regex.Match(@"xxx1234");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be(@"xxx1234");

        // Should NOT match
        var match3 = regex.Match(@"xxx");
        match3.Should().BeNull();
    }

    [Fact]
    public void PCRE2_Test019_DotPlusDigitsEndAnchor()
    {
        // Pattern from PCRE2: /^.+[0-9][0-9][0-9]$/
        var regex = new ColorerRegex(@"^.+[0-9][0-9][0-9]$");

        var validInputs = new[]
        {
            @"x123",
            @"xx123",
            @"123456",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
            match!.Value.Should().Be(input);
        }

        // Edge case: "x1234" should match (greedy .+ takes 'x1', then '234')
        var edgeMatch = regex.Match(@"x1234");
        edgeMatch.Should().NotBeNull();
        edgeMatch!.Value.Should().Be(@"x1234");
    }

    #endregion

    #region Test 12: Backreferences (testinput1 lines 420-423)

    [Fact]
    public void PCRE2_Test020_SimpleBackreference()
    {
        // Pattern from PCRE2: /^(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)(k)\11*(\3\4)\1(?#)2$/
        // Tests: multiple captures and backreferences
        // \11* is tricky: could be \11 (backreference 11) or \1 followed by 1*
        // In PCRE2, with 11 groups, \11 is backreference to group 11 (k)
        // Simplified version for testing: /(a)\1/
        var regex = new ColorerRegex(@"(a)\1");

        var match = regex.Match(@"aa");
        match.Should().NotBeNull();
        match!.Value.Should().Be(@"aa");
        match.GetGroupValue(1).Should().Be(@"a");
    }

    [Fact]
    public void PCRE2_Test021_BackreferenceWithQuantifier()
    {
        // Pattern: (abc)\1 - backreference to captured group
        var regex = new ColorerRegex(@"(abc)\1");

        var match = regex.Match(@"abcabc");
        match.Should().NotBeNull();
        match!.Value.Should().Be(@"abcabc");
        match.GetGroupValue(1).Should().Be(@"abc");

        // Should NOT match if backreference doesn't match
        var noMatch = regex.Match(@"abcdef");
        noMatch.Should().BeNull();
    }

    [Fact]
    public void PCRE2_Test022_MultipleBackreferences()
    {
        // Pattern: (a)(b)\2\1 - references in reverse order
        var regex = new ColorerRegex(@"(a)(b)\2\1");

        var match = regex.Match(@"abba");
        match.Should().NotBeNull();
        match!.Value.Should().Be(@"abba");
        match.GetGroupValue(1).Should().Be(@"a");
        match.GetGroupValue(2).Should().Be(@"b");
    }

    #endregion

    #region Test 13: Lookahead Assertions (testinput1 lines 1059-1062)

    [Fact]
    public void PCRE2_Test023_PositiveLookahead()
    {
        // Pattern from PCRE2: /^(?=ab(de))(abd)(e)/
        // Positive lookahead: (?=...) asserts pattern exists ahead
        var regex = new ColorerRegex(@"^(?=ab(de))(abd)(e)");

        var match = regex.Match(@"abde");
        match.Should().NotBeNull();
        match!.Value.Should().Be(@"abde");
    }

    [Fact]
    public void PCRE2_Test024_NegativeLookahead()
    {
        // Pattern from PCRE2: /^(?!(ab)de|x)(abd)(f)/
        // Negative lookahead: (?!...) asserts pattern does NOT exist ahead
        var regex = new ColorerRegex(@"^(?!(ab)de|x)(abd)(f)");

        var match = regex.Match(@"abdf");
        match.Should().NotBeNull();
        match!.Value.Should().Be(@"abdf");

        // Should NOT match "abde" because lookahead fails
        var noMatch = regex.Match(@"abde");
        noMatch.Should().BeNull();
    }

    #endregion

    #region Test 14: Character Class Ranges (testinput1 lines 203-230)

    [Fact]
    public void PCRE2_Test025_CharacterRange()
    {
        // Pattern from PCRE2: /^[0-9a-f]+$/i (hex digits)
        var regex = new ColorerRegex(@"^[0-9a-f]+$", RegexOptions.IgnoreCase);

        var validInputs = new[]
        {
            @"0",
            @"a",
            @"f",
            @"A",
            @"F",
            @"0123456789",
            @"abcdef",
            @"ABCDEF",
            @"DeadBeef",
        };

        foreach (var input in validInputs)
        {
            var match = regex.Match(input);
            match.Should().NotBeNull($"Pattern should match '{input}'");
            match!.Value.Should().Be(input);
        }

        // Should NOT match
        var invalidInputs = new[] { @"g", @"z", @"xyz" };
        foreach (var input in invalidInputs)
        {
            var match = regex.Match(input);
            match.Should().BeNull($"Pattern should NOT match '{input}'");
        }
    }

    #endregion

    #region Test 15: Nested Quantifiers and Groups (testinput1 lines 98-112)

    [Fact]
    public void PCRE2_Test026_NestedGroups()
    {
        // Pattern from PCRE2: /^(?:a(b(c)))(?:d(e(f)))(?:h(i(j)))(?:k(l(m)))$/
        // Multiple nested non-capturing and capturing groups
        var regex = new ColorerRegex(@"^(?:a(b(c)))(?:d(e(f)))(?:h(i(j)))(?:k(l(m)))$");

        var match = regex.Match(@"abcdefhijklm");
        match.Should().NotBeNull();
        match!.Value.Should().Be(@"abcdefhijklm");

        // Verify capture groups
        match.GetGroupValue(1).Should().Be(@"bc");
        match.GetGroupValue(2).Should().Be(@"c");
        match.GetGroupValue(3).Should().Be(@"ef");
        match.GetGroupValue(4).Should().Be(@"f");
    }

    #endregion

    #region Test 16: Non-Greedy vs Greedy Quantifiers (testinput1 lines 274-289)

    [Fact]
    public void PCRE2_Test027_GreedyVsNonGreedy()
    {
        // Pattern: /.+foo/ (greedy) vs /.+?foo/ (non-greedy)
        var greedyRegex = new ColorerRegex(@".+foo");
        var nonGreedyRegex = new ColorerRegex(@".+?foo");

        string input = @"xfooyfoo";

        // Greedy should match maximum: "xfooyfoo"
        var greedyMatch = greedyRegex.Match(input);
        greedyMatch.Should().NotBeNull();
        greedyMatch!.Value.Should().Be(@"xfooyfoo");

        // Non-greedy should match minimum: "xfoo"
        var nonGreedyMatch = nonGreedyRegex.Match(input);
        nonGreedyMatch.Should().NotBeNull();
        nonGreedyMatch!.Value.Should().Be(@"xfoo");
    }

    #endregion

    #region Test 17: Word Boundaries (testinput1 lines 379-395)

    [Fact]
    public void PCRE2_Test028_WordBoundary()
    {
        // Pattern from PCRE2: /\bfoo\b/
        var regex = new ColorerRegex(@"\bfoo\b");

        // Should match "foo" as separate word
        var match1 = regex.Match(@"foo");
        match1.Should().NotBeNull();

        var match2 = regex.Match(@"foo bar");
        match2.Should().NotBeNull();

        var match3 = regex.Match(@"(foo)");
        match3.Should().NotBeNull();

        // Should NOT match "foo" as part of word
        var noMatch1 = regex.Match(@"foobar");
        noMatch1.Should().BeNull();

        var noMatch2 = regex.Match(@"barfoo");
        noMatch2.Should().BeNull();
    }

    [Fact]
    public void PCRE2_Test029_NonWordBoundary()
    {
        // Pattern: /\Bfoo\B/ - NOT at word boundary
        var regex = new ColorerRegex(@"\Bfoo\B");

        // Should match "foo" inside word
        var match = regex.Match(@"barfoobar");
        match.Should().NotBeNull();

        // Should NOT match at word boundaries
        var noMatch1 = regex.Match(@"foo");
        noMatch1.Should().BeNull();

        var noMatch2 = regex.Match(@"foobar");
        noMatch2.Should().BeNull();
    }

    #endregion

    #region Test 18: Empty Alternation (testinput1 lines 245-249)

    [Fact]
    public void PCRE2_Test030_EmptyAlternation()
    {
        // Pattern from PCRE2: /(a)b|/
        // Empty alternative always matches (matches empty string)
        var regex = new ColorerRegex(@"(a)b|");

        // Should match "ab"
        var match1 = regex.Match(@"ab");
        match1.Should().NotBeNull();

        // Should also match empty string at start of any input
        var match2 = regex.Match(@"xyz");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be(string.Empty);
    }

    #endregion
}
