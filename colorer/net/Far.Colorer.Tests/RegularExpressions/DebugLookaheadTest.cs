using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;
using Xunit;
using System;

namespace Far.Colorer.Tests.RegularExpressions;

public class DebugLookaheadTest
{
    [Fact]
    public void TestNegativeLookahead()
    {
        // Test: \d+(?!px) follows Perl semantics
        // "100em" - matches "100" (lookahead doesn't see "px")
        // "100px" - matches "10" (greedy backtracking: "100" fails, "10" succeeds)
        var regex = new ColorerRegex(@"\d+(?!px)", RegexOptions.None);

        var match1 = regex.Match("100em");
        Console.WriteLine($"Match '100em': {match1 != null}, Value: '{match1?.Value}'");

        var match2 = regex.Match("100px");
        Console.WriteLine($"Match '100px': {match2 != null}, Value: '{match2?.Value}'");
        Console.WriteLine("Perl semantic: '100px' matches '10' due to backtracking");

        Assert.NotNull(match1);
        Assert.Equal("100", match1!.Value);

        // Perl behavior: matches "10" (correct!)
        Assert.NotNull(match2);
        Assert.Equal("10", match2!.Value);
    }

    [Fact]
    public void TestPositiveLookahead()
    {
        // Test: \d+(?=px) should match "100" in "100px" but not in "100em"
        var regex = new ColorerRegex(@"\d+(?=px)", RegexOptions.None);

        var match1 = regex.Match("100px");
        Console.WriteLine($"Match '100px': {match1 != null}, Value: '{match1?.Value}'");

        var match2 = regex.Match("100em");
        Console.WriteLine($"Match '100em': {match2 != null}, Value: '{match2?.Value}'");

        Assert.NotNull(match1);
        Assert.Equal("100", match1!.Value);
        Assert.Null(match2);
    }
}
