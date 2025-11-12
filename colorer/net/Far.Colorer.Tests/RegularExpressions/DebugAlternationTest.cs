using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;
using Xunit;
using System;

namespace Far.Colorer.Tests.RegularExpressions;

public class DebugAlternationTest
{
    [Fact]
    public void TestSimpleAlternation()
    {
        // Test: cat|dog
        var regex = new ColorerRegex("cat|dog", RegexOptions.None);

        var match1 = regex.Match("I have a cat");
        Console.WriteLine($"Match 'I have a cat': {match1 != null}, Value: '{match1?.Value}', Index: {match1?.Index}");

        var match2 = regex.Match("I have a dog");
        Console.WriteLine($"Match 'I have a dog': {match2 != null}, Value: '{match2?.Value}', Index: {match2?.Index}");

        Assert.NotNull(match1);
        Assert.Equal("cat", match1!.Value);

        Assert.NotNull(match2);
        Assert.Equal("dog", match2!.Value);
    }

    [Fact]
    public void TestAlternationInGroup()
    {
        // Test: (cat|dog)s?
        var regex = new ColorerRegex("(cat|dog)s?", RegexOptions.None);

        var match = regex.Match("I have cats and dogs");
        Console.WriteLine($"Match: {match != null}, Value: '{match?.Value}', Index: {match?.Index}");

        Assert.NotNull(match);
        Assert.Equal("cats", match!.Value);
    }
}
