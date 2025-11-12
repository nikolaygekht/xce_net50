using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;
using Xunit;
using System;

namespace Far.Colorer.Tests.RegularExpressions;

public unsafe class DebugCharClassTest
{
    [Fact]
    public void TestNegatedCharClass()
    {
        // Test: [^abc]+ should match "xyz"
        var regex = new ColorerRegex("[^abc]+", RegexOptions.None);
        var match = regex.Match("xyz");

        Console.WriteLine($"Match result: {match != null}");
        if (match != null)
        {
            Console.WriteLine($"Match value: '{match.Value}', Index: {match.Index}, Length: {match.Length}");
        }

        Assert.NotNull(match);
        Assert.Equal("xyz", match!.Value);
    }

    [Fact]
    public void TestSimpleCharClass()
    {
        // Test: [abc]+ should match "abcba" in "xyzabcba"
        var regex = new ColorerRegex("[abc]+", RegexOptions.None);
        var match = regex.Match("xyzabcba");

        Console.WriteLine($"Match result: {match != null}");
        if (match != null)
        {
            Console.WriteLine($"Match value: '{match.Value}', Index: {match.Index}, Length: {match.Length}");
        }

        Assert.NotNull(match);
        Assert.Equal("abcba", match!.Value);
    }
}
