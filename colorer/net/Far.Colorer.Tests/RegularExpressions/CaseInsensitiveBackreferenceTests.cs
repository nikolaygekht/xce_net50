using Far.Colorer.RegularExpressions.Enums;
using Far.Colorer.RegularExpressions.Internal;
using Xunit;
using AwesomeAssertions;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Tests for case-insensitive backreferences.
/// Verifies that backreferences respect the IgnoreCase flag.
/// </summary>
public unsafe class CaseInsensitiveBackreferenceTests
{
    #region Case-Sensitive Backreferences (baseline)

    [Fact]
    public void Backreference_CaseSensitive_ExactMatch_Succeeds()
    {
        // Pattern: (abc)\1 without IgnoreCase
        var regex = new ColorerRegex(@"(abc)\1", RegexOptions.None);
        var match = regex.Match("abcabc");

        match.Should().NotBeNull();
        match!.Value.Should().Be("abcabc");
    }

    [Fact]
    public void Backreference_CaseSensitive_DifferentCase_Fails()
    {
        // Pattern: (abc)\1 without IgnoreCase
        var regex = new ColorerRegex(@"(abc)\1", RegexOptions.None);
        var match = regex.Match("abcABC");

        match.Should().BeNull();
    }

    #endregion

    #region Case-Insensitive Backreferences (the bug we're fixing)

    [Fact]
    public void Backreference_IgnoreCase_SameCase_Succeeds()
    {
        // Pattern: (abc)\1 with IgnoreCase - same case
        var regex = new ColorerRegex(@"(abc)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("abcabc");

        match.Should().NotBeNull();
        match!.Value.Should().Be("abcabc");
    }

    [Fact]
    public void Backreference_IgnoreCase_DifferentCase_ShouldSucceed()
    {
        // Pattern: (abc)\1 with IgnoreCase - different case
        // BUG: This currently fails but should succeed
        var regex = new ColorerRegex(@"(abc)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("abcABC");

        match.Should().NotBeNull();
        match!.Value.Should().Be("abcABC");
    }

    [Fact]
    public void Backreference_IgnoreCase_MixedCase_ShouldSucceed()
    {
        // Pattern: (abc)\1 with IgnoreCase - mixed case
        var regex = new ColorerRegex(@"(abc)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("abcAbC");

        match.Should().NotBeNull();
        match!.Value.Should().Be("abcAbC");
    }

    [Fact]
    public void Backreference_IgnoreCase_AllUppercase_ShouldSucceed()
    {
        // Pattern: (ABC)\1 with IgnoreCase - lowercase input
        var regex = new ColorerRegex(@"(ABC)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("ABCabc");

        match.Should().NotBeNull();
        match!.Value.Should().Be("ABCabc");
    }

    #endregion

    #region Multiple Backreferences

    [Fact]
    public void MultipleBackreferences_IgnoreCase_DifferentCases_ShouldSucceed()
    {
        // Pattern: (foo)(bar)\1\2 with IgnoreCase
        var regex = new ColorerRegex(@"(foo)(bar)\1\2", RegexOptions.IgnoreCase);
        var match = regex.Match("foobarFOOBAR");

        match.Should().NotBeNull();
        match!.Value.Should().Be("foobarFOOBAR");
    }

    [Fact]
    public void MultipleBackreferences_IgnoreCase_MixedCases_ShouldSucceed()
    {
        // Pattern: (a)(b)\1\2 with IgnoreCase
        var regex = new ColorerRegex(@"(a)(b)\1\2", RegexOptions.IgnoreCase);
        var match = regex.Match("abAB");

        match.Should().NotBeNull();
        match!.Value.Should().Be("abAB");
    }

    #endregion

    #region Backreference with Alternation

    [Fact]
    public void Backreference_WithAlternation_IgnoreCase_ShouldSucceed()
    {
        // Pattern: (foo|bar)\1 with IgnoreCase
        var regex = new ColorerRegex(@"(foo|bar)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("fooFOO");

        match.Should().NotBeNull();
        match!.Value.Should().Be("fooFOO");
    }

    [Fact]
    public void Backreference_WithAlternation_SecondOption_IgnoreCase_ShouldSucceed()
    {
        // Pattern: (foo|bar)\1 with IgnoreCase - matching 'bar'
        var regex = new ColorerRegex(@"(foo|bar)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("barBAR");

        match.Should().NotBeNull();
        match!.Value.Should().Be("barBAR");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Backreference_IgnoreCase_Numbers_Match()
    {
        // Pattern: (123)\1 - numbers have no case
        var regex = new ColorerRegex(@"(123)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("123123");

        match.Should().NotBeNull();
        match!.Value.Should().Be("123123");
    }

    [Fact]
    public void Backreference_IgnoreCase_SpecialChars_Match()
    {
        // Pattern: (!@#)\1 - special chars have no case
        var regex = new ColorerRegex(@"(!@#)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("!@#!@#");

        match.Should().NotBeNull();
        match!.Value.Should().Be("!@#!@#");
    }

    [Fact]
    public void Backreference_IgnoreCase_MixedAlphanumeric_ShouldSucceed()
    {
        // Pattern: (test123)\1 with IgnoreCase
        var regex = new ColorerRegex(@"(test123)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("test123TEST123");

        match.Should().NotBeNull();
        match!.Value.Should().Be("test123TEST123");
    }

    #endregion

    #region Wrong Content Should Still Fail

    [Fact]
    public void Backreference_IgnoreCase_WrongContent_Fails()
    {
        // Pattern: (abc)\1 - different letters should still fail
        var regex = new ColorerRegex(@"(abc)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("abcdef");

        match.Should().BeNull();
    }

    [Fact]
    public void Backreference_IgnoreCase_PartialMatch_Fails()
    {
        // Pattern: (abc)\1 - only partial second occurrence
        var regex = new ColorerRegex(@"(abc)\1", RegexOptions.IgnoreCase);
        var match = regex.Match("abcAB");

        match.Should().BeNull();
    }

    #endregion
}
