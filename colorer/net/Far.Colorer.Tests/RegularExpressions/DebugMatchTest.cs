using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;
using Xunit;
using System;

namespace Far.Colorer.Tests.RegularExpressions;

public unsafe class DebugMatchTest
{
    [Fact]
    public void TestBasicMatching()
    {
        // Test compiler
        var compiler = new CRegExpCompiler("abc", RegexOptions.None);
        var tree = compiler.Compile();

        Console.WriteLine($"Compiled tree: {(IntPtr)tree}");

        // Test matcher
        var matcher = new CRegExpMatcher(tree, false, false, false);
        bool result = matcher.Parse("xyzabcdef", 0, 9);

        Console.WriteLine($"Parse result: {result}");

        if (result)
        {
            matcher.GetMatches(out int start, out int end);
            Console.WriteLine($"Match: start={start}, end={end}");
        }

        Assert.True(result, "Should match 'abc' in 'xyzabcdef'");
    }
}
