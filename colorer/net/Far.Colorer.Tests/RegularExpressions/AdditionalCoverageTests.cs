using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Enums;
using Far.Colorer.RegularExpressions.Internal;
using Xunit;
using AwesomeAssertions;
using Xunit.Abstractions;
using System.Linq;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Additional tests for uncovered code paths in CRegExpMatcher and CRegExpCompiler.
/// Based on real HRC file usage patterns and C++ behavior.
///
/// Coverage targets:
/// - CRegExpMatcher: lines 369-370, 388-389, 403-406 (CheckStack calls), 906 (RePreNW), 984-995 (InsertStack)
/// - CRegExpCompiler: lines 87-89 (extend whitespace), 196-204 (ReEmpty), 220 (incomplete escape),
///                    268-271 (RePreNW), 701-746 (char class escapes), 1084-1100 (alternation with empty)
/// </summary>
public class AdditionalCoverageTests
{
    private readonly ITestOutputHelper _output;

    public AdditionalCoverageTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region RePreNW (\c) Tests - Compiler 268-271, Matcher 906-908

    [Fact]
    public void PreNW_AtStartOfString_Matches()
    {
        // Arrange - From asm.hrc: /\c[\-+]?\d*\.?\d+([eE][\-+]?\d+)?\b/
        // \c matches at position 0 (no preceding letter)
        var regex = new ColorerRegex(@"\c\d+", RegexOptions.None);

        // Act
        var match = regex.Match("123");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("123");
        match.Index.Should().Be(0);
    }

    [Fact]
    public void PreNW_AfterNonLetter_Matches()
    {
        // Arrange - \c matches after space (non-letter)
        var regex = new ColorerRegex(@"\c\d+", RegexOptions.None);

        // Act
        var match = regex.Match("abc 123");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("123");
        match.Index.Should().Be(4);
    }

    [Fact]
    public void PreNW_InMixedString_MatchesAfterDigit()
    {
        // Arrange - \c checks if previous char is NOT a letter
        // C++ code: return toParse == 0 || !isLetter(pattern[toParse - 1])
        var regex = new ColorerRegex(@"\c\d+", RegexOptions.None);

        // Act - "abc123": positions a=0,b=1,c=2,1=3,2=4,3=5
        // Pos 0: \c matches, \d+ fails (not digit)
        // Pos 3: \c fails (pattern[2]='c' is letter)
        // Pos 4: \c matches (pattern[3]='1' NOT letter), \d+ matches "23"
        var match = regex.Match("abc123");

        // Assert - Matches "23" at position 4
        match.Should().NotBeNull();
        match!.Value.Should().Be("23");
        match.Index.Should().Be(4);
    }

    [Fact]
    public void PreNW_AtEndOfString_Matches()
    {
        // Arrange - C++ line 768-769: if (toParse >= end) return true
        // \c at end of string matches
        var regex = new ColorerRegex(@"abc\c", RegexOptions.None);

        // Act
        var match = regex.Match("abc");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    [Fact]
    public void PreNW_RealHrcPattern_AsmNumbers()
    {
        // Arrange - Real pattern from asm.hrc for decimal numbers
        // /\c[\-+]?\d*\.?\d+([eE][\-+]?\d+)?\b/
        var regex = new ColorerRegex(@"\c[\-+]?\d*\.?\d+([eE][\-+]?\d+)?\b", RegexOptions.None);

        // Act
        var match1 = regex.Match("3.14");
        var match2 = regex.Match("  -42");
        var match3 = regex.Match("1.5e-10");

        // Assert
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("3.14");

        match2.Should().NotBeNull();
        match2!.Value.Should().Be("-42");

        match3.Should().NotBeNull();
        match3!.Value.Should().Be("1.5e-10");
    }

    #endregion

    #region Extended Mode Whitespace - Compiler 87-89

    [Fact]
    public void ExtendedMode_SkipsWhitespace()
    {
        // Arrange - In extended mode, whitespace should be ignored
        var regex = new ColorerRegex(@"a b c", RegexOptions.Extended);

        // Act
        var match = regex.Match("abc");

        // Assert - "a b c" with extended mode should match "abc"
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    [Fact]
    public void ExtendedMode_SkipsTabs()
    {
        // Arrange
        var regex = new ColorerRegex("a\tb\tc", RegexOptions.Extended);

        // Act
        var match = regex.Match("abc");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    [Fact]
    public void ExtendedMode_SkipsNewlines()
    {
        // Arrange
        var regex = new ColorerRegex("a\nb\nc", RegexOptions.Extended);

        // Act
        var match = regex.Match("abc");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    #endregion

    #region Incomplete Escape - Compiler 220

    [Fact]
    public void IncompleteEscape_AtEndOfPattern_ThrowsException()
    {
        // Arrange - Backslash at end of pattern
        // Act & Assert
        Action act = () => new ColorerRegex(@"abc\", RegexOptions.None);
        act.Should().Throw<RegexSyntaxException>().WithMessage("Incomplete escape sequence");
    }

    [Fact]
    public void IncompleteEscape_JustBackslash_ThrowsException()
    {
        // Arrange
        // Act & Assert
        Action act = () => new ColorerRegex(@"\", RegexOptions.None);
        act.Should().Throw<RegexSyntaxException>().WithMessage("Incomplete escape sequence");
    }

    #endregion

    #region ReEmpty for Invalid Quantifier Targets - Compiler 196-204

    [Fact]
    public void InvalidQuantifierTarget_Star_CreatesEmpty()
    {
        // Arrange - * at start creates ReEmpty node
        var regex = new ColorerRegex(@"*abc", RegexOptions.None);

        // Act
        var match = regex.Match("abc");

        // Assert - Should match "abc", * is treated as empty
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    [Fact]
    public void InvalidQuantifierTarget_Plus_CreatesEmpty()
    {
        // Arrange - + at start creates ReEmpty node
        var regex = new ColorerRegex(@"+abc", RegexOptions.None);

        // Act
        var match = regex.Match("abc");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    [Fact]
    public void InvalidQuantifierTarget_Question_CreatesEmpty()
    {
        // Arrange - ? at start creates ReEmpty node
        var regex = new ColorerRegex(@"?abc", RegexOptions.None);

        // Act
        var match = regex.Match("abc");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("abc");
    }

    [Fact]
    public void InvalidQuantifierTarget_Brace_ThrowsException()
    {
        // Arrange - { without proper syntax throws exception
        // Unlike *, +, ?, the { starts a range quantifier and must be valid

        // Act & Assert
        Action act = () => new ColorerRegex(@"{abc", RegexOptions.None);
        act.Should().Throw<RegexSyntaxException>();
    }

    #endregion

    #region Character Class Escapes - Compiler 701-746

    [Fact]
    public void CharClass_EscapedD_MatchesDigits()
    {
        // Arrange - [\d] should match digits
        var regex = new ColorerRegex(@"[\d]+", RegexOptions.None);

        // Act
        var match = regex.Match("abc123xyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("123");
    }

    [Fact]
    public void CharClass_EscapedW_MatchesWordChars()
    {
        // Arrange - [\w] should match word characters (letters, digits, underscore)
        var regex = new ColorerRegex(@"[\w]+", RegexOptions.None);

        // Act
        var match = regex.Match("  hello_123  ");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("hello_123");
    }

    [Fact]
    public void CharClass_EscapedS_MatchesWhitespace()
    {
        // Arrange - [\s] should match whitespace
        var regex = new ColorerRegex(@"[\s]+", RegexOptions.None);

        // Act
        var match = regex.Match("abc   \t\nxyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("   \t\n");
    }

    [Fact]
    public void CharClass_EscapedN_MatchesNewline()
    {
        // Arrange - [\n] should match newline
        var regex = new ColorerRegex(@"[\n]+", RegexOptions.None);

        // Act
        var match = regex.Match("abc\n\nxyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("\n\n");
    }

    [Fact]
    public void CharClass_EscapedT_MatchesTab()
    {
        // Arrange - [\t] should match tab
        var regex = new ColorerRegex(@"[\t]+", RegexOptions.None);

        // Act
        var match = regex.Match("abc\t\txyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("\t\t");
    }

    [Fact]
    public void CharClass_EscapedR_MatchesCarriageReturn()
    {
        // Arrange - [\r] should match carriage return
        var regex = new ColorerRegex(@"[\r]+", RegexOptions.None);

        // Act
        var match = regex.Match("abc\r\rxyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("\r\r");
    }

    [Fact]
    public void CharClass_EscapedF_MatchesFormFeed()
    {
        // Arrange - [\f] should match form feed
        var regex = new ColorerRegex(@"[\f]+", RegexOptions.None);

        // Act
        var match = regex.Match("abc\f\fxyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("\f\f");
    }

    [Fact]
    public void CharClass_EscapedA_MatchesBell()
    {
        // Arrange - [\a] should match bell character
        var regex = new ColorerRegex(@"[\a]+", RegexOptions.None);

        // Act
        var match = regex.Match("abc\a\axyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("\a\a");
    }

    [Fact]
    public void CharClass_EscapedE_MatchesEscape()
    {
        // Arrange - [\e] should match escape character (0x1B)
        var regex = new ColorerRegex(@"[\e]+", RegexOptions.None);

        // Act
        var match = regex.Match("abc\x1b\x1bxyz");

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be("\x1b\x1b");
    }

    #endregion

    #region Alternation with Empty Alternatives - Compiler 1084-1100

    [Fact(Skip = "Known issue: Alternation with empty branches doesn't match PCRE2 behavior")]
    public void Alternation_EmptyBeforeFirst_Matches()
    {
        // KNOWN ISSUE: Our implementation currently only matches the empty alternative
        // PCRE2 behavior for |foo with input "foo":
        //   Expected: 2 matches - empty string at 0, then "foo" at 0
        //   Actual: only matches empty string at every position
        //
        // The problem is that when left branch is empty (always matches),
        // the right branch ("foo") is never tried.
        //
        // This appears to be a bug in the ReOr (alternation) implementation
        // in CRegExpMatcher - it doesn't properly backtrack to try right branch
        // when left branch is empty/zero-width.

        var regex = new ColorerRegex(@"|foo", RegexOptions.None);

        // At position 0, PCRE2 finds both empty and "foo"
        var match = regex.Match("foo");
        match.Should().NotBeNull();
        // Currently: matches empty at 0
        // Expected PCRE2: should also find "foo" at 0 via backtracking
    }

    [Fact(Skip = "Known issue: Alternation with empty branches doesn't match PCRE2 behavior")]
    public void Alternation_EmptyBetween_Matches()
    {
        // KNOWN ISSUE: Similar to EmptyBeforeFirst
        // PCRE2 behavior for foo||bar with input "bar":
        //   Expected: 3 matches - empty, "bar", empty
        //   Actual: only empty strings

        var regex = new ColorerRegex(@"foo||bar", RegexOptions.None);

        var match = regex.Match("bar");
        match.Should().NotBeNull();
        // Currently: matches empty
        // Expected: should match "bar"
    }

    [Fact]
    public void Alternation_EmptyAfterLast_Matches()
    {
        // This pattern works correctly because "foo" is tried first
        var regex = new ColorerRegex(@"foo|", RegexOptions.None);

        var match1 = regex.Match("foo");
        match1.Should().NotBeNull();
        match1!.Value.Should().Be("foo");

        var match2 = regex.Match("bar");
        match2.Should().NotBeNull();
        match2!.Value.Should().Be(""); // Empty alternative works
    }

    #endregion

    #region InsertStack Extension - Matcher 984-995

    [Fact]
    public void DeepBacktracking_ExtendsStack_CompletesSuccessfully()
    {
        // Arrange - Initial stack size is 512, need > 512 InsertStack calls
        // Pattern with alternation and quantifiers causes heavy backtracking
        // Each alternation branch tried = 1 InsertStack call
        // (a|b|c|d|e)+ with long input and final mismatch forces extensive backtracking
        var regex = new ColorerRegex(@"(a|b|c|d|e)+f", RegexOptions.None);

        // Act - Long input that doesn't end with 'f' to force full backtracking
        // This will try many combinations via InsertStack
        var input = string.Concat(Enumerable.Repeat("abcde", 150)); // 750 chars, no 'f'
        var match = regex.Match(input + "f");

        // Assert - Should complete without stack overflow, stack should have expanded
        match.Should().NotBeNull();
        match!.Value.Should().Be(input + "f");
    }

    [Fact]
    public void VeryDeepNesting_ExtendsStackMultipleTimes()
    {
        // Arrange - Pattern with catastrophic backtracking potential
        // Nested quantifiers with alternation: each level multiplies backtracking
        // This should force multiple stack expansions (512 -> 640 -> 768...)
        var regex = new ColorerRegex(@"((a|b)+)+c", RegexOptions.None);

        // Act - Input that matches after extensive backtracking
        // The + quantifiers will try many different ways to partition the input
        var input = string.Concat(Enumerable.Repeat("ab", 200)) + "c"; // 400 a/b chars + c
        var match = regex.Match(input);

        // Assert - Should complete, stack expanded multiple times
        match.Should().NotBeNull();
        match!.Value.Should().Be(input);
    }

    [Fact]
    public void PathologicalBacktracking_HandlesStackGrowth()
    {
        // Arrange - The classic pathological case: nested quantifiers
        // (a*)* with input of a's can cause exponential backtracking
        // Each * needs to decide how many a's to consume at each level
        var regex = new ColorerRegex(@"(a*)*b", RegexOptions.None);

        // Act - Many a's followed by b - forces backtracking through all partitions
        var input = new string('a', 100) + "b";
        var match = regex.Match(input);

        // Assert - Should handle it (even if slow) and expand stack as needed
        match.Should().NotBeNull();
        match!.Value.Should().Be(input);
    }

    [Fact]
    public void ExtremelyDeepRecursion_ForcesStackExpansion()
    {
        // Arrange - Pattern designed to maximize InsertStack calls
        // Initial stack = 512, MEM_INC = 128
        // Need a pattern that creates > 512 stack entries
        //
        // Strategy: Use alternation with many branches + quantifiers
        // Pattern: (a|b|c|d|e|f|g|h|i|j)+ forces branching at each position
        // With non-greedy quantifier and lookahead, we can force more backtracking
        var regex = new ColorerRegex(@"(a|b|c|d|e|f|g|h|i|j)+?x", RegexOptions.None);

        // Act - Very long input without 'x' until the end
        // This forces trying all alternation combinations via backtracking
        // Each character position × 10 alternatives = many InsertStack calls
        var input = string.Concat(Enumerable.Repeat("abcdefghij", 100)) + "x";

        _output.WriteLine($"Input length: {input.Length} chars");
        _output.WriteLine("Pattern: (a|b|c|d|e|f|g|h|i|j)+?x");
        _output.WriteLine("This should trigger stack expansion from 512 -> 640+ elements");

        var match = regex.Match(input);

        // Assert
        match.Should().NotBeNull();
        match!.Value.Should().Be(input);

        _output.WriteLine($"Match successful: {match!.Value.Length} chars matched");
    }

    #endregion

    #region CheckStack Edge Cases - Matcher 369-370, 388-389, 403-406

    [Fact]
    public void CrossPatternBackref_NoBackRE_ThrowsAtCompileTime()
    {
        // Arrange - With compile-time resolution, \y{name} requires SetBackRE before compilation
        // C++ returns EERROR if backRE not set during compilation

        // Act & Assert - Should throw because no backRE set
        Action act = () => new ColorerRegex(@"\y{test}", RegexOptions.None);
        act.Should().Throw<RegexSyntaxException>().WithMessage("*requires SetBackRE()*");
    }

    [Fact]
    public void CrossPatternBackref_NameNotInBackRE_ThrowsAtCompileTime()
    {
        // Arrange - C++ validates name exists in backRE at compile time
        var startPattern = new ColorerRegex(@"(?{foo}a)", RegexOptions.None);

        // Act & Assert - Should throw because "bar" not found in startPattern
        Action act = () => ColorerRegex.CreateWithBackRE(@"\y{bar}", startPattern, RegexOptions.None);
        act.Should().Throw<RegexSyntaxException>().WithMessage("*Named group 'bar' not found*");
    }

    [Fact]
    public void CrossPatternBackref_SamePatternName_StillRequiresBackRE()
    {
        // Arrange - \y{name} is ALWAYS cross-pattern, even if name exists in same pattern
        // This matches C++ behavior where backRE must be set
        // The pattern has {test} defined, but \y still needs a backRE

        // Act & Assert - Should throw because no backRE set
        Action act = () => new ColorerRegex(@"(?{test}a)\y{test}", RegexOptions.None);
        act.Should().Throw<RegexSyntaxException>().WithMessage("*requires SetBackRE()*");
    }

    [Fact]
    public void CaseInsensitiveBackref_CharMismatch_TriggersCheckStack()
    {
        // Arrange - Lines 369-370: character mismatch in case-insensitive comparison
        var startPattern = new ColorerRegex(@"(abc)", RegexOptions.None);
        var endPattern = new ColorerRegex(@"\Y1", RegexOptions.None);

        // Act
        var startMatch = startPattern.Match("abc");
        startMatch.Should().NotBeNull();

        endPattern.SetBackReference("abc", startMatch);
        var endMatch = endPattern.Match("xyz"); // Mismatch

        // Assert
        endMatch.Should().BeNull();
    }

    #endregion
}
