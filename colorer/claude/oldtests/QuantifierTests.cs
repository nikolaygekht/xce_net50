using Far.Colorer.RegularExpressions;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Tests for quantifier functionality (*, +, ?, {n,m}).
/// </summary>
public class QuantifierTests
{
    #region Star (*) Tests

    [Fact]
    public void Star_MatchesZeroOccurrences()
    {
        var regex = new ColorerRegex("ab*c");

        Assert.True(regex.IsMatch("ac")); // Zero b's
        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("abbc"));
    }

    [Fact]
    public void Star_MatchesOneOccurrence()
    {
        var regex = new ColorerRegex("ab*c");

        Assert.True(regex.IsMatch("abc"));
    }

    [Fact]
    public void Star_MatchesMultipleOccurrences()
    {
        var regex = new ColorerRegex("ab*c");

        Assert.True(regex.IsMatch("abbc"));
        Assert.True(regex.IsMatch("abbbc"));
        Assert.True(regex.IsMatch("abbbbbbc"));
    }

    [Fact]
    public void Star_IsGreedy()
    {
        var regex = new ColorerRegex("a.*b");
        var match = regex.Match("axxxbyyybzzz");

        Assert.True(match.Success);
        // Should match from first 'a' to last 'b' (greedy)
        Assert.Equal("axxxbyyybzzz", match.Value);
    }

    [Fact]
    public void StarNonGreedy_MatchesMinimal()
    {
        var regex = new ColorerRegex("a.*?b");
        var match = regex.Match("axxxbyyybzzz");

        Assert.True(match.Success);
        // Should match from first 'a' to first 'b' (non-greedy)
        Assert.Equal("axxxb", match.Value);
    }

    #endregion

    #region Plus (+) Tests

    [Fact]
    public void Plus_DoesNotMatchZero()
    {
        var regex = new ColorerRegex("ab+c");

        Assert.False(regex.IsMatch("ac")); // Zero b's
        Assert.True(regex.IsMatch("abc"));
    }

    [Fact]
    public void Plus_MatchesOne()
    {
        var regex = new ColorerRegex("ab+c");

        Assert.True(regex.IsMatch("abc"));
    }

    [Fact]
    public void Plus_MatchesMultiple()
    {
        var regex = new ColorerRegex("ab+c");

        Assert.True(regex.IsMatch("abbc"));
        Assert.True(regex.IsMatch("abbbc"));
        Assert.True(regex.IsMatch("abbbbbbc"));
    }

    [Fact]
    public void Plus_IsGreedy()
    {
        var regex = new ColorerRegex("a.+b");
        var match = regex.Match("axxxbyyybzzz");

        Assert.True(match.Success);
        // Should match from 'a' to last 'b' (greedy)
        Assert.Equal("axxxbyyybzzz", match.Value);
    }

    [Fact]
    public void PlusNonGreedy_MatchesMinimal()
    {
        var regex = new ColorerRegex("a.+?b");
        var match = regex.Match("axxxbyyybzzz");

        Assert.True(match.Success);
        // Should match from 'a' to first 'b' (non-greedy)
        Assert.Equal("axxxb", match.Value);
    }

    #endregion

    #region Question (?) Tests

    [Fact]
    public void Question_MatchesZero()
    {
        var regex = new ColorerRegex("ab?c");

        Assert.True(regex.IsMatch("ac")); // Zero b's
        Assert.True(regex.IsMatch("abc"));
    }

    [Fact]
    public void Question_MatchesOne()
    {
        var regex = new ColorerRegex("ab?c");

        Assert.True(regex.IsMatch("abc"));
    }

    [Fact]
    public void Question_DoesNotMatchMultiple()
    {
        var regex = new ColorerRegex("ab?c");

        Assert.False(regex.IsMatch("abbc"));
    }

    [Fact]
    public void QuestionNonGreedy_MatchesZero()
    {
        var regex = new ColorerRegex("ab??c");

        Assert.True(regex.IsMatch("ac")); // Prefers zero
        Assert.True(regex.IsMatch("abc"));
    }

    #endregion

    #region Range {n,m} Tests

    [Fact]
    public void RangeExact_MatchesExactCount()
    {
        var regex = new ColorerRegex("ab{3}c");

        Assert.False(regex.IsMatch("abc"));
        Assert.False(regex.IsMatch("abbc"));
        Assert.True(regex.IsMatch("abbbc"));
        Assert.False(regex.IsMatch("abbbbc"));
    }

    [Fact]
    public void RangeMin_MatchesAtLeastN()
    {
        var regex = new ColorerRegex("ab{2,}c");

        Assert.False(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("abbc"));
        Assert.True(regex.IsMatch("abbbc"));
        Assert.True(regex.IsMatch("abbbbc"));
    }

    [Fact]
    public void RangeMinMax_MatchesBetweenNAndM()
    {
        var regex = new ColorerRegex("ab{2,4}c");

        Assert.False(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("abbc"));
        Assert.True(regex.IsMatch("abbbc"));
        Assert.True(regex.IsMatch("abbbbc"));
        Assert.False(regex.IsMatch("abbbbbc"));
    }

    [Fact]
    public void Range_IsGreedy()
    {
        var regex = new ColorerRegex("a.{2,4}b");
        var match = regex.Match("axxxxb");

        Assert.True(match.Success);
        // Should match 4 chars (greedy)
        Assert.Equal("axxxxb", match.Value);
    }

    [Fact]
    public void RangeNonGreedy_MatchesMinimal()
    {
        var regex = new ColorerRegex("a.{2,4}?b");
        var match = regex.Match("axxxxb");

        Assert.True(match.Success);
        // Should match 2 chars (non-greedy)
        Assert.Equal("axxb", match.Value);
    }

    [Fact]
    public void Range_WithZeroMin_MatchesZero()
    {
        var regex = new ColorerRegex("ab{0,2}c");

        Assert.True(regex.IsMatch("ac"));
        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("abbc"));
        Assert.False(regex.IsMatch("abbbc"));
    }

    #endregion

    #region Complex Quantifier Tests

    [Fact]
    public void MultipleQuantifiers_Work()
    {
        var regex = new ColorerRegex("a+b*c+");

        Assert.True(regex.IsMatch("ac"));
        Assert.True(regex.IsMatch("abc"));
        Assert.True(regex.IsMatch("aaabbbccc"));
        Assert.False(regex.IsMatch("bc"));
        Assert.False(regex.IsMatch("ab"));
    }

    [Fact]
    public void QuantifierOnGroup_Works()
    {
        var regex = new ColorerRegex("(ab)+");

        Assert.True(regex.IsMatch("ab"));
        Assert.True(regex.IsMatch("abab"));
        Assert.True(regex.IsMatch("ababab"));
        Assert.False(regex.IsMatch("a"));
        Assert.False(regex.IsMatch("aba"));
    }

    [Fact]
    public void QuantifierOnDot_Works()
    {
        var regex = new ColorerRegex(".+");

        Assert.True(regex.IsMatch("anything"));
        Assert.False(regex.IsMatch(""));
    }

    [Fact]
    public void NestedQuantifiers_Work()
    {
        var regex = new ColorerRegex("(a+)+b");

        Assert.True(regex.IsMatch("ab"));
        Assert.True(regex.IsMatch("aaab"));
        Assert.True(regex.IsMatch("aaaaaab"));
    }

    #endregion

    #region Error Cases

    [Fact]
    public void InvalidRange_MaxLessThanMin_ThrowsException()
    {
        Assert.Throws<RegexSyntaxException>(() => new ColorerRegex("a{5,2}"));
    }

    [Fact]
    public void UnterminatedRange_ThrowsException()
    {
        Assert.Throws<RegexSyntaxException>(() => new ColorerRegex("a{2,"));
    }

    [Fact]
    public void QuantifierWithoutAtom_ThrowsException()
    {
        Assert.Throws<RegexSyntaxException>(() => new ColorerRegex("*"));
        Assert.Throws<RegexSyntaxException>(() => new ColorerRegex("+"));
        Assert.Throws<RegexSyntaxException>(() => new ColorerRegex("?"));
    }

    #endregion
}
