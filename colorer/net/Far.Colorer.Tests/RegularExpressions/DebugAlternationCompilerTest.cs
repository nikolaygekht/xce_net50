using Far.Colorer.RegularExpressions.Internal;
using Far.Colorer.RegularExpressions.Enums;
using Xunit;
using Xunit.Abstractions;
using System;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Debug test to understand what the compiler produces for alternation patterns
/// </summary>
public unsafe class DebugAlternationCompilerTest
{
    private readonly ITestOutputHelper _output;

    public DebugAlternationCompilerTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DebugSimpleAlternation()
    {
        // Arrange
        var compiler = new CRegExpCompiler("cat|dog", RegexOptions.None);

        // Act
        var tree = compiler.Compile();

        // Assert & Debug
        _output.WriteLine("Pattern: cat|dog");
        _output.WriteLine("");
        PrintTree(tree, 0);

        Assert.True(tree != null);
    }

    [Fact]
    public void DebugAlternationInGroup()
    {
        // Arrange
        var compiler = new CRegExpCompiler("(cat|dog)", RegexOptions.None);

        // Act
        var tree = compiler.Compile();

        // Assert & Debug
        _output.WriteLine("Pattern: (cat|dog)");
        _output.WriteLine("");
        PrintTree(tree, 0);

        Assert.True(tree != null);
    }

    private void PrintTree(SRegInfo* node, int depth)
    {
        if (node == null)
            return;

        // Safety check for depth
        if (depth > 50)
        {
            _output.WriteLine(new string(' ', depth * 2) + "(max depth reached)");
            return;
        }

        string indent = new string(' ', depth * 2);

        // Safety check: verify node pointer is valid before accessing
        try
        {
            var op = node->op; // Test access
            _output.WriteLine($"{indent}Node: op={op}, param0={node->param0}, param1={node->param1}");

            if (node->op == EOps.ReSymb)
            {
                _output.WriteLine($"{indent}  symbol: '{node->symbol}'");
            }

            if (node->op == EOps.ReEnum || node->op == EOps.ReNEnum)
            {
                _output.WriteLine($"{indent}  CharClass: {(node->charclass != null ? "present" : "null")}");
            }

            // Print param (child) - with safety check
            if (node->param != null)
            {
                _output.WriteLine($"{indent}  param (child):");
                try
                {
                    PrintTree(node->param, depth + 2);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"{indent}    (invalid param pointer: {ex.Message})");
                }
            }
            else if (node->op > EOps.ReBlockOps && node->op < EOps.ReSymbolOps)
            {
                _output.WriteLine($"{indent}  param: NULL (expected for block op)");
            }

            // Print next (sibling) - with safety check
            if (node->next != null)
            {
                _output.WriteLine($"{indent}  next (sibling):");
                try
                {
                    PrintTree(node->next, depth + 1);
                }
                catch (Exception ex)
                {
                    _output.WriteLine($"{indent}  (invalid next pointer: {ex.Message})");
                }
            }
        }
        catch (Exception ex)
        {
            _output.WriteLine($"{indent}(invalid node pointer: {ex.Message})");
        }
    }
}
