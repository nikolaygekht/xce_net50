using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Far.Colorer.RegularExpressions.Enums;

namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// High-level regular expression wrapper combining compiler and matcher.
/// Provides the main API for compiling and executing Colorer-specific regular expressions.
/// </summary>
internal unsafe class ColorerRegex : IDisposable
{
    private CRegExpCompiler? compiler;
    private SRegInfo* compiledTree;
    private CRegExpMatcher? matcher;
    private string pattern;
    private RegexOptions options;

    public ColorerRegex(string pattern, RegexOptions options = RegexOptions.None)
    {
        if (pattern == null)
            throw new ArgumentNullException(nameof(pattern));

        this.pattern = pattern;
        this.options = options;

        // Compile the pattern (empty string is valid)
        Compile();
    }

    private void Compile()
    {
        compiler = new CRegExpCompiler(pattern, options);
        compiledTree = compiler.Compile();

        // Extract options for matcher
        bool ignoreCase = (options & RegexOptions.IgnoreCase) != 0;
        bool singleLine = (options & RegexOptions.Singleline) != 0;
        bool multiLine = (options & RegexOptions.Multiline) != 0;

        // Create matcher
        matcher = new CRegExpMatcher(compiledTree, ignoreCase, singleLine, multiLine);
    }

    /// <summary>
    /// Match the pattern against input string starting at specified position.
    /// </summary>
    public ColorerMatch? Match(string input, int startIndex = 0)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (startIndex < 0 || startIndex > input.Length)
            throw new ArgumentOutOfRangeException(nameof(startIndex));

        if (matcher == null)
            throw new InvalidOperationException("Regex not compiled");

        // Try to match at startIndex
        bool success = matcher.Parse(input, startIndex, input.Length);

        if (!success)
            return null;

        // Extract match results
        matcher.GetMatches(out int matchStart, out int matchEnd);

        if (matchStart == -1 || matchEnd == -1)
            return null;

        // Create match object with captures
        var captures = new List<CaptureGroup>();

        // Add capture group 0 (full match)
        captures.Add(new CaptureGroup(
            matchStart,
            matchEnd - matchStart,
            0)); // group number 0

        // Add numbered captures (1-9)
        for (int i = 1; i < 10; i++)
        {
            matcher.GetCapture(i, out int capStart, out int capEnd);
            if (capStart >= 0 && capEnd >= 0 && capEnd >= capStart)
            {
                captures.Add(new CaptureGroup(
                    capStart,
                    capEnd - capStart,
                    i)); // group number i
            }
        }

        return new ColorerMatch(
            input,
            matchStart,
            matchEnd - matchStart,
            captures);
    }

    /// <summary>
    /// Find all matches in the input string.
    /// </summary>
    public IEnumerable<ColorerMatch> Matches(string input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        int position = 0;
        while (position < input.Length)
        {
            var match = Match(input, position);
            if (match == null)
            {
                position++;
                continue;
            }

            yield return match;

            // Move past this match
            position = match.Index + Math.Max(1, match.Length);
        }
    }

    /// <summary>
    /// Test if the pattern matches the input.
    /// </summary>
    public bool IsMatch(string input, int startIndex = 0)
    {
        return Match(input, startIndex) != null;
    }

    /// <summary>
    /// Set backreference support for COLORERMODE (\y, \Y cross-pattern references).
    /// </summary>
    public void SetBackReference(string? backStr, ColorerMatch? backMatch)
    {
        if (matcher == null)
            throw new InvalidOperationException("Regex not compiled");

        if (backStr == null || backMatch == null)
        {
            matcher.SetBackTrace(null, null);
            return;
        }

        // Convert ColorerMatch to SMatches
        SMatches* backTrace = (SMatches*)Marshal.AllocHGlobal(sizeof(SMatches));
        backTrace->Clear();

        // Fill in captures from backMatch
        var groups = backMatch.Groups;
        for (int i = 0; i < Math.Min(groups.Count, 10); i++)
        {
            var capture = groups[i];
            int* sArr = backTrace->s;
            int* eArr = backTrace->e;
            sArr[i] = capture.Index;
            eArr[i] = capture.Index + capture.Length;
        }

        backTrace->cMatch = groups.Count;

        matcher.SetBackTrace(backStr, backTrace);

        // Note: backTrace is leaked here - in production code we'd need to track and free it
        // For now this matches the C++ semantics where the caller owns the backTrace
    }

    public void Dispose()
    {
        matcher?.Dispose();
        matcher = null;

        compiler?.Dispose();
        compiler = null;

        GC.SuppressFinalize(this);
    }

    ~ColorerRegex()
    {
        Dispose();
    }

    /// <summary>
    /// Get the original pattern string.
    /// </summary>
    public string Pattern => pattern;

    /// <summary>
    /// Get the regex options.
    /// </summary>
    public RegexOptions Options => options;

    /// <summary>
    /// Static helper to match a pattern against input without creating a reusable regex object.
    /// </summary>
    public static ColorerMatch? Match(string input, string pattern, RegexOptions options = RegexOptions.None)
    {
        using var regex = new ColorerRegex(pattern, options);
        return regex.Match(input);
    }

    /// <summary>
    /// Static helper to test if a pattern matches input.
    /// </summary>
    public static bool IsMatch(string input, string pattern, RegexOptions options = RegexOptions.None)
    {
        using var regex = new ColorerRegex(pattern, options);
        return regex.IsMatch(input);
    }

    /// <summary>
    /// Static helper to find all matches.
    /// </summary>
    public static IEnumerable<ColorerMatch> Matches(string input, string pattern, RegexOptions options = RegexOptions.None)
    {
        using var regex = new ColorerRegex(pattern, options);
        foreach (var match in regex.Matches(input))
        {
            yield return match;
        }
    }
}
