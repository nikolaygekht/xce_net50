using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Enums;
using Far.Colorer.RegularExpressions.Internal;
using Xunit;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Tests for numeric backreferences (\1, \2, etc.) with named capture groups.
/// This ensures that (?{name}...) groups work with standard backreference syntax.
/// </summary>
public class BackreferenceNamedGroupTests
{
    [Fact]
    public void NamedGroup_NumericBackreference_Matches()
    {
        var regex = new ColorerRegex(@"(?{tag}\w+):\1");
        var match = regex.Match("test:test");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("test:test", match.Value);
        Assert.Equal("test", match.GetGroupValue("tag"));
    }

    [Fact]
    public void NamedGroup_NumericBackreference_NoMatch()
    {
        var regex = new ColorerRegex(@"(?{tag}\w+):\1");
        var match = regex.Match("test:other");

        Assert.Null(match);
    }

    [Fact]
    public void MultipleNamedGroups_Backreference_Correct()
    {
        var regex = new ColorerRegex(@"(?{a}\w+)-(?{b}\w+):\1:\2");
        var match = regex.Match("foo-bar:foo:bar");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("foo", match.GetGroupValue("a"));
        Assert.Equal("bar", match.GetGroupValue("b"));
    }

    [Fact]
    public void MixedNamedAndRegular_Backreferences_Work()
    {
        var regex = new ColorerRegex(@"(?{named}\w+)(\d+):\1:\2");
        var match = regex.Match("test123:test:123");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("test", match.GetGroupValue("named"));
        Assert.Equal("123", match.GetGroupValue(2));
    }

    [Fact]
    public void NamedGroup_CaseInsensitive_Backreference()
    {
        var regex = new ColorerRegex(@"(?{word}\w+):\1", RegexOptions.IgnoreCase);
        var match = regex.Match("TEST:test");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("TEST", match.GetGroupValue("word"));
    }

    [Fact]
    public void NamedGroup_EmptyBackreference_Matches()
    {
        var regex = new ColorerRegex(@"(?{opt}a?):\1");
        var match = regex.Match(":");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal(":", match.Value);
    }

    [Fact]
    public void NamedGroup_RepeatedBackreference()
    {
        var regex = new ColorerRegex(@"(?{tag}\w+):\1:\1");
        var match = regex.Match("foo:foo:foo");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("foo", match.GetGroupValue("tag"));
    }

    [Fact]
    public void NamedGroup_Backreference_PartialMatch()
    {
        var regex = new ColorerRegex(@"(?{word}\w+)-\1");
        var match = regex.Match("hello-hello world");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("hello-hello", match.Value);
        Assert.Equal("hello", match.GetGroupValue("word"));
    }

    [Fact]
    public void NamedGroup_NestedBackreference()
    {
        var regex = new ColorerRegex(@"(?{outer}(?{inner}\w+)-\2):\1");
        var match = regex.Match("test-test:test-test");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("test-test", match.GetGroupValue("outer"));
        Assert.Equal("test", match.GetGroupValue("inner"));
    }

    [Fact]
    public void RegularGroup_WithNamedGroup_BothBackreference()
    {
        // Mix regular and named groups
        var regex = new ColorerRegex(@"(\w+)-(?{named}\d+)-\1-\2");
        var match = regex.Match("foo-123-foo-123");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("foo", match.GetGroupValue(1));
        Assert.Equal("123", match.GetGroupValue("named"));
    }
}
