# Colorer Regex Engine - .NET Port Implementation Plan

## Overview

This document provides a comprehensive plan for porting the Colorer regular expression engine from C++ to .NET 8/9, with a focus on:
- **Maximum performance** using `Span<char>` and modern .NET features
- **Complete feature compatibility** with existing HRC files
- **Comprehensive testing** against real-world syntax patterns
- **Incremental delivery** with testable milestones

## Context

Based on comprehensive analysis of 349 HRC files (see `REGEX_ANALYSIS.md`, `REGEX_EXAMPLES.md`, `REGEX_IMPLEMENTATION_GUIDE.md`):
- **80% of patterns** use Tier 1 features (cross-pattern backreferences, lookahead, position markers)
- **15% of patterns** use Tier 2 features (named/negative backreferences, advanced positioning)
- **5% of patterns** use Tier 3 features (Unicode character classes, class operations)

**Critical Finding**: .NET's `System.Text.RegularExpressions` cannot be used due to cross-pattern backreferences where capture groups from START regex are referenced in END regex - a unique Colorer feature.

## Solution Structure

```
net/
├── Far.Colorer/                          # Main library
│   └── RegularExpressions/               # NEW: Regex engine
│       ├── ColorerRegex.cs               # Main regex class
│       ├── ColorerMatch.cs               # Match result
│       ├── ColorerMatchCollection.cs     # Match collection
│       ├── RegexNode.cs                  # AST node
│       ├── RegexCompiler.cs              # Pattern -> AST
│       ├── RegexMatcher.cs               # Execution engine (Span-based)
│       ├── Enums/
│       │   ├── RegexOperator.cs          # EOps equivalent
│       │   ├── MetaSymbol.cs             # EMetaSymbols equivalent
│       │   └── RegexOptions.cs           # Flags (IgnoreCase, etc.)
│       ├── CharacterClasses/
│       │   ├── CharacterClass.cs         # Base class
│       │   ├── UnicodeCharacterClass.cs  # [{L}], [{Nd}], etc.
│       │   └── CharacterClassParser.cs   # Parse [...] patterns
│       ├── Nodes/
│       │   ├── IRegexNode.cs             # Node interface
│       │   ├── LiteralNode.cs            # Literal characters
│       │   ├── QuantifierNode.cs         # *, +, ?, {n,m}
│       │   ├── GroupNode.cs              # (...), (?{name}...)
│       │   ├── BackreferenceNode.cs      # \yN, \y{name}, \YN
│       │   ├── LookaheadNode.cs          # ?=, ?!
│       │   ├── LookbehindNode.cs         # ?#N, ?~N
│       │   └── MetacharacterNode.cs      # ~, \M, \m, ^, $
│       └── Internal/
│           ├── MatchState.cs             # Matching state machine
│           ├── CaptureGroup.cs           # Group storage
│           └── RegexCache.cs             # Compiled pattern cache
└── Far.Colorer.Tests/                    # Test project
    └── RegularExpressions/               # NEW: Regex tests
        ├── BasicTests.cs                 # Basic functionality
        ├── BackreferenceTests.cs         # \yN, \y{name}, \YN
        ├── LookaheadTests.cs             # ?=, ?!
        ├── MetacharacterTests.cs         # ~, \M, \m
        ├── CharacterClassTests.cs        # [{L}], [{Nd}]
        ├── PerformanceTests.cs           # Benchmarks
        └── Integration/
            ├── HrcPatternTests.cs        # Real HRC patterns
            └── TestData/                 # Sample HRC excerpts
                ├── lua_patterns.txt
                ├── c_patterns.txt
                └── perl_patterns.txt
```

## Architecture Design

### 1. Core Classes

#### ColorerRegex
```csharp
public sealed class ColorerRegex
{
    private readonly string _pattern;
    private readonly RegexOptions _options;
    private readonly RegexNode _compiledTree;

    // Cross-pattern backreference support
    private ColorerRegex? _backReferenceRegex;
    private ColorerMatch? _backReferenceMatch;

    public ColorerRegex(string pattern, RegexOptions options = RegexOptions.None)
    {
        _pattern = pattern;
        _options = options;
        _compiledTree = RegexCompiler.Compile(pattern, options);
    }

    // Primary match API - Span-based for performance
    public bool IsMatch(ReadOnlySpan<char> input)
    public ColorerMatch Match(ReadOnlySpan<char> input)
    public ColorerMatch Match(ReadOnlySpan<char> input, int startPos, int endPos)

    // Cross-pattern backreference API
    public void SetBackReference(ColorerRegex regex, ColorerMatch match)
    public (ColorerRegex? Regex, ColorerMatch? Match) GetBackReference()

    // Named group management
    public int GetGroupNumber(string name)
    public string? GetGroupName(int number)
}
```

#### ColorerMatch
```csharp
public sealed class ColorerMatch
{
    // Group captures
    public CaptureGroup[] Groups { get; }
    public Dictionary<string, CaptureGroup> NamedGroups { get; }

    // Match position
    public int Index { get; }
    public int Length { get; }
    public bool Success { get; }

    // Efficient access without allocations
    public ReadOnlySpan<char> GetGroupSpan(int groupNumber)
    public ReadOnlySpan<char> GetGroupSpan(string groupName)

    // Position markers (for \M, \m support)
    public int EffectiveStart { get; internal set; }
    public int EffectiveEnd { get; internal set; }
}
```

#### RegexMatcher (Execution Engine)
```csharp
internal sealed class RegexMatcher
{
    // Stack-based (non-recursive) matching for safety
    private readonly Stack<MatchState> _stateStack;
    private readonly ArrayPool<CaptureGroup> _groupPool;

    // Primary matching logic - fully Span-based
    public bool TryMatch(
        ReadOnlySpan<char> input,
        int startPos,
        int endPos,
        RegexNode compiledTree,
        RegexOptions options,
        ColorerMatch? backrefMatch,  // For \yN support
        out ColorerMatch? result)
    {
        // Span-based matching implementation
        // Uses stackalloc for small allocations
        // ArrayPool for larger temporary buffers
        // No string allocations during matching
    }

    // Fast path optimizations
    private bool QuickReject(ReadOnlySpan<char> input, RegexNode tree)
    {
        // First character optimization
        // Fixed string prefix detection
        // Minimum length checks
    }
}
```

### 2. Performance Strategy

#### Span<char> Usage
```csharp
// Example: Efficient character class matching
private bool MatchCharacterClass(
    ReadOnlySpan<char> input,
    ref int position,
    CharacterClass charClass)
{
    if (position >= input.Length)
        return false;

    char ch = input[position];
    if (charClass.Contains(ch))
    {
        position++;
        return true;
    }
    return false;
}

// Example: Backreference matching without allocations
private bool MatchBackreference(
    ReadOnlySpan<char> input,
    ref int position,
    ReadOnlySpan<char> capturedText,
    bool ignoreCase)
{
    if (position + capturedText.Length > input.Length)
        return false;

    var slice = input.Slice(position, capturedText.Length);

    bool match = ignoreCase
        ? slice.Equals(capturedText, StringComparison.OrdinalIgnoreCase)
        : slice.SequenceEqual(capturedText);

    if (match)
        position += capturedText.Length;

    return match;
}
```

#### Memory Pooling
```csharp
// Use ArrayPool for temporary allocations
private static readonly ArrayPool<CaptureGroup> GroupPool =
    ArrayPool<CaptureGroup>.Shared;

// Example usage in matcher
public ColorerMatch Match(ReadOnlySpan<char> input)
{
    const int MaxStackGroups = 16;

    // Small allocation on stack
    Span<CaptureGroup> groups = stackalloc CaptureGroup[MaxStackGroups];

    int requiredGroups = _compiledTree.GroupCount;
    CaptureGroup[]? rentedGroups = null;

    try
    {
        if (requiredGroups > MaxStackGroups)
        {
            // Large allocation from pool
            rentedGroups = GroupPool.Rent(requiredGroups);
            groups = rentedGroups.AsSpan(0, requiredGroups);
        }

        // Perform matching with pooled memory
        bool success = ExecuteMatch(input, groups);

        // Create result with copied data
        return success ? new ColorerMatch(groups.ToArray()) : ColorerMatch.Empty;
    }
    finally
    {
        if (rentedGroups != null)
            GroupPool.Return(rentedGroups);
    }
}
```

#### Compiled Pattern Caching
```csharp
internal sealed class RegexCache
{
    // Cache compiled patterns to avoid re-compilation
    private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), RegexNode>
        Cache = new();

    private const int MaxCacheSize = 256;

    public static RegexNode GetOrCompile(string pattern, RegexOptions options)
    {
        var key = (pattern, options);

        return Cache.GetOrAdd(key, k =>
        {
            // Evict old entries if cache is full
            if (Cache.Count > MaxCacheSize)
                EvictOldest();

            return RegexCompiler.Compile(k.Pattern, k.Options);
        });
    }
}
```

### 3. Cross-Pattern Backreference Architecture

This is the most critical feature that makes Colorer unique:

```csharp
// Example from lua.hrc (lines 118-124):
// START: /\[(?{OpenBracket}=*)\[/
// END:   /\](?{CloseBracket}\y1)\]/

// Implementation:
public class CrossPatternBackreferenceExample
{
    public void MatchLuaComment(string text)
    {
        // 1. Compile and match START pattern
        var startRegex = new ColorerRegex(@"\[(?{OpenBracket}=*)\[");
        var startMatch = startRegex.Match(text);

        if (!startMatch.Success)
            return;

        // 2. Create END pattern and link to START match
        var endRegex = new ColorerRegex(@"\](?{CloseBracket}\y1)\]");
        endRegex.SetBackReference(startRegex, startMatch);

        // 3. Match END pattern - \y1 references group 1 from startMatch
        int searchStart = startMatch.Index + startMatch.Length;
        var endMatch = endRegex.Match(text.AsSpan(searchStart));

        // The \y1 in endRegex will match exactly what was captured
        // in group 1 of startMatch (the = characters)
    }
}

// Internal implementation in RegexMatcher:
private bool MatchCrossPatternBackreference(
    ReadOnlySpan<char> input,
    ref int position,
    int groupNumber)
{
    // Get the backreference match (set via SetBackReference)
    if (_backReferenceMatch == null)
        throw new InvalidOperationException(
            "Cross-pattern backreference \\y without SetBackReference");

    // Get the captured text from the OTHER pattern's match
    var capturedGroup = _backReferenceMatch.Groups[groupNumber];
    if (!capturedGroup.Success)
        return false;

    var capturedText = _backReferenceMatch.Input
        .AsSpan(capturedGroup.Index, capturedGroup.Length);

    // Match against current input
    return MatchBackreference(input, ref position, capturedText, _ignoreCase);
}
```

### 4. Special Feature Implementations

#### Tilde (~) - Scheme Start Marker
```csharp
// Matches the position where the current scheme started
private bool MatchSchemeStart(ReadOnlySpan<char> input, int position, int schemeStartPos)
{
    // ~ only matches at the exact scheme start position
    return position == schemeStartPos;
}

// Usage in public API:
public ColorerMatch Match(
    ReadOnlySpan<char> input,
    int startPos = 0,
    int endPos = -1,
    int schemeStartPos = 0)  // NEW parameter
{
    // Pass schemeStartPos to matcher for ~ support
}
```

#### \M and \m - Position Markers
```csharp
// \M moves the effective start of the match
// \m moves the effective end of the match

private class MatchState
{
    public int Position;
    public int EffectiveStart;  // Modified by \M
    public int EffectiveEnd;    // Modified by \m
    public CaptureGroup[] Groups;
}

private bool MatchSetStart(MatchState state)
{
    // \M: Set the start of group 0 to current position
    state.EffectiveStart = state.Position;
    return true;
}

private bool MatchSetEnd(MatchState state)
{
    // \m: Set the end of group 0 to current position
    state.EffectiveEnd = state.Position;
    return true;
}
```

#### Negative Backreferences (\YN, \Y{name})
```csharp
// Matches if the backreference does NOT match at current position
private bool MatchNegativeBackreference(
    ReadOnlySpan<char> input,
    ref int position,
    int groupNumber)
{
    if (_backReferenceMatch == null)
        return false;

    var capturedGroup = _backReferenceMatch.Groups[groupNumber];
    if (!capturedGroup.Success)
        return true; // No capture = negative match succeeds

    var capturedText = _backReferenceMatch.Input
        .AsSpan(capturedGroup.Index, capturedGroup.Length);

    // Check if it DOESN'T match
    if (position + capturedText.Length > input.Length)
        return true;

    var slice = input.Slice(position, capturedText.Length);
    bool matches = slice.SequenceEqual(capturedText);

    // Negative: succeed if it doesn't match, don't advance position
    return !matches;
}
```

## Implementation Phases

### Phase 1: Foundation (Week 1) - 40 hours

**Goal**: Basic regex engine structure with core matching

**Tasks**:
1. Create project structure and base classes
   - `ColorerRegex`, `ColorerMatch`, `RegexNode`
   - Enums: `RegexOperator`, `MetaSymbol`, `RegexOptions`
2. Implement `RegexCompiler`
   - Parse pattern string to AST
   - Handle basic operators: literals, `.`, `^`, `$`
   - Handle quantifiers: `*`, `+`, `?`, `{n,m}`
3. Implement basic `RegexMatcher`
   - Span-based matching loop
   - Simple backtracking
   - Group capture (numeric only)
4. Write foundation tests
   - Pattern compilation
   - Basic matching
   - Simple quantifiers

**Deliverables**:
- [ ] Project structure created
- [ ] Basic regex working for simple patterns
- [ ] 50+ unit tests passing
- [ ] Documentation of public API

**Validation**:
```csharp
// Should work by end of Phase 1:
var regex = new ColorerRegex(@"ab+c");
Assert.True(regex.IsMatch("abbc"));

var regex2 = new ColorerRegex(@"(\w+)");
var match = regex2.Match("hello");
Assert.Equal("hello", match.Groups[1].Value);
```

### Phase 2: Tier 1 Features (Week 2) - 60 hours

**Goal**: Implement 80% of required features

**Tasks**:
1. Cross-pattern backreferences `\yN`
   - `SetBackReference` API
   - Reference resolution in matcher
   - Integration tests with HRC patterns
2. Named groups `(?{name}...)`
   - Name-to-number mapping
   - Named backreferences `\p{name}`
3. Negative lookahead `?!`
   - Lookahead execution without consuming input
   - Negative assertion logic
4. Position markers `~`, `\M`
   - Scheme start tracking
   - Effective match start adjustment
5. Character classes `[...]`, `[^...]`
   - Range parsing
   - Unicode support

**Deliverables**:
- [ ] Cross-pattern backreferences working
- [ ] Named groups working
- [ ] Lookahead working
- [ ] Position markers working
- [ ] 100+ unit tests passing
- [ ] Integration tests with lua.hrc, c.hrc patterns

**Validation**:
```csharp
// Lua comment example should work:
var start = new ColorerRegex(@"\[(?{OpenBracket}=*)\[");
var startMatch = start.Match("[===[");
Assert.Equal("==", startMatch.GetGroupSpan("OpenBracket").ToString());

var end = new ColorerRegex(@"\](?{CloseBracket}\y1)\]");
end.SetBackReference(start, startMatch);
var endMatch = end.Match("]===]");
Assert.True(endMatch.Success);
```

### Phase 3: Tier 2 Features (Week 3) - 50 hours

**Goal**: Implement 15% advanced features

**Tasks**:
1. Named backreferences `\y{name}`, `\Y{name}`
   - Name resolution across patterns
   - Error handling for missing names
2. Negative backreferences `\YN`
   - Negative matching logic
   - VB.NET block structure support
3. Position end marker `\m`
   - Effective match end adjustment
4. Lookbehind `?#N`, `?~N`
   - Fixed-length lookbehind
   - Negative lookbehind

**Deliverables**:
- [ ] All Tier 2 features working
- [ ] 150+ unit tests passing
- [ ] Integration tests with vbasic.hrc, python.hrc
- [ ] Performance benchmarks

**Validation**:
```csharp
// VB.NET block nesting should work:
var start = new ColorerRegex(@"(?{BlockType}If|While|For)");
var startMatch = start.Match("If");

var end = new ColorerRegex(@"End\s+\y{BlockType}");
end.SetBackReference(start, startMatch);
Assert.True(end.IsMatch("End If"));
Assert.False(end.IsMatch("End While"));
```

### Phase 4: Tier 3 & Optimization (Week 4-5) - 50 hours

**Goal**: Unicode features and performance optimization

**Tasks**:
1. Unicode character classes `[{L}]`, `[{Nd}]`, etc.
   - Parse `[{...}]` syntax
   - Map to .NET `UnicodeCategory`
   - Character class operations: union, intersection, subtraction
2. Performance optimization
   - First-character optimization
   - Boyer-Moore for literal prefixes
   - Compiled pattern caching
   - Profile and optimize hot paths
3. Edge cases and error handling
   - Malformed patterns
   - Stack overflow protection
   - Timeout support

**Deliverables**:
- [ ] All features complete
- [ ] 200+ unit tests passing
- [ ] All HRC integration tests passing
- [ ] Performance within 2x of C++ version
- [ ] Complete API documentation

### Phase 5: Integration & Testing (Week 5-6) - 40 hours

**Goal**: Comprehensive testing and integration

**Tasks**:
1. HRC integration tests
   - Extract patterns from all major HRC files
   - Create test cases for each pattern type
   - Validate against expected results
2. Performance testing
   - Benchmark against C++ version
   - Profile memory usage
   - Optimize critical paths
3. Documentation
   - API reference
   - Usage examples
   - Migration guide from C++ regex
4. Code review and polish
   - Address analyzer warnings
   - Ensure consistent style
   - Final test coverage review

**Deliverables**:
- [ ] 95%+ code coverage
- [ ] All HRC patterns working
- [ ] Performance benchmarks documented
- [ ] Complete documentation
- [ ] Ready for integration with parser

## Testing Strategy

### Unit Tests (150+ tests)

```csharp
// Far.Colorer.Tests/RegularExpressions/

// Basic functionality
[Fact] public void Literal_Matches_ExactString()
[Fact] public void Dot_Matches_AnyCharacter()
[Fact] public void Quantifier_Star_Matches_ZeroOrMore()
[Fact] public void Quantifier_Plus_Matches_OneOrMore()

// Backreferences
[Fact] public void NumericBackreference_MatchesCapturedGroup()
[Fact] public void CrossPatternBackreference_MatchesFromOtherRegex()
[Fact] public void NamedBackreference_MatchesByName()
[Fact] public void NegativeBackreference_FailsWhenMatches()

// Lookahead
[Fact] public void PositiveLookahead_MatchesWithoutConsuming()
[Fact] public void NegativeLookahead_MatchesWhenPatternFails()

// Position markers
[Fact] public void Tilde_MatchesSchemeStart()
[Fact] public void BackslashM_MovesMatchStart()
[Fact] public void Backslashm_MovesMatchEnd()

// Character classes
[Fact] public void CharacterClass_MatchesRange()
[Fact] public void NegatedCharacterClass_MatchesOutsideRange()
[Fact] public void UnicodeCharacterClass_MatchesCategory()

// Edge cases
[Fact] public void EmptyPattern_MatchesEmptyString()
[Fact] public void MalformedPattern_ThrowsException()
[Fact] public void VeryLongInput_DoesNotStackOverflow()
```

### Integration Tests (50+ tests)

```csharp
// Real HRC patterns
[Theory]
[InlineData("lua.hrc", "line 118", @"\[(?{OpenBracket}=*)\[", "[===[")]
[InlineData("c.hrc", "line 218", @"^\s*(?{PoundSymbol}#)\s*define", "#define FOO")]
[InlineData("perl.hrc", "line 156", @"s([^\w\s])", "s/foo/bar/")]
public void HrcPattern_MatchesExpectedInput(
    string file, string line, string pattern, string input)
{
    var regex = new ColorerRegex(pattern);
    Assert.True(regex.IsMatch(input));
}

// Cross-pattern tests
[Fact]
public void LuaMultilineComment_MatchesWithCorrectDelimiter()
{
    var start = new ColorerRegex(@"\[(?{OpenBracket}=*)\[");
    var startMatch = start.Match("--[===[");

    var end = new ColorerRegex(@"\](?{CloseBracket}\y1)\]");
    end.SetBackReference(start, startMatch);

    Assert.True(end.IsMatch("]===]"));
    Assert.False(end.IsMatch("]==]"));
    Assert.False(end.IsMatch("]====]"));
}
```

### Performance Tests

```csharp
[Benchmark]
public void Benchmark_SimplePattern()
{
    var regex = new ColorerRegex(@"ab+c");
    var input = "xyzabbbbcdefg";
    for (int i = 0; i < 10000; i++)
        regex.IsMatch(input);
}

[Benchmark]
public void Benchmark_Backreference()
{
    var regex = new ColorerRegex(@"(\w+)=\1");
    var input = "foo=foo bar=bar";
    for (int i = 0; i < 10000; i++)
        regex.IsMatch(input);
}

[Benchmark]
public void Benchmark_CrossPatternBackreference()
{
    var start = new ColorerRegex(@"\[(?{OpenBracket}=*)\[");
    var startMatch = start.Match("[===[");

    var end = new ColorerRegex(@"\](?{CloseBracket}\y1)\]");
    end.SetBackReference(start, startMatch);

    for (int i = 0; i < 10000; i++)
        end.IsMatch("]===]");
}

// Target: < 2x slower than C++ version
```

## Risk Mitigation

### High Risk: Cross-Pattern Backreferences

**Risk**: Complex feature, easy to get wrong
**Mitigation**:
- Start with simple numeric backreferences
- Extensive unit tests
- Validate against all HRC usage
- Clear documentation of limitations

### Medium Risk: Performance

**Risk**: C# may be slower than C++
**Mitigation**:
- Use Span<char> throughout
- ArrayPool for allocations
- Profile early and often
- Optimize hot paths with unsafe code if needed

### Medium Risk: Unicode Handling

**Risk**: Differences between ICU and .NET
**Mitigation**:
- Use .NET's built-in UnicodeCategory
- Test with diverse Unicode input
- Document any differences

### Low Risk: API Design

**Risk**: May need changes later
**Mitigation**:
- Keep internal APIs internal
- Mark public APIs carefully
- Follow .NET naming conventions

## Success Criteria

### MVP (End of Phase 3)
- [ ] All Tier 1 & 2 features working
- [ ] 95% of HRC patterns supported
- [ ] 150+ unit tests passing
- [ ] Integration tests with major HRC files pass
- [ ] Basic performance acceptable (< 3x slower than C++)

### Full Release (End of Phase 5)
- [ ] All features working including Tier 3
- [ ] 99%+ of HRC patterns supported
- [ ] 200+ unit tests passing
- [ ] All HRC integration tests pass
- [ ] Performance < 2x slower than C++
- [ ] 95%+ code coverage
- [ ] Complete documentation
- [ ] Zero critical bugs

## Next Steps

1. **Review this plan** - Validate approach and estimates
2. **Create project structure** - Set up folders and files
3. **Start Phase 1** - Begin with foundation classes
4. **Set up test infrastructure** - xUnit, test data, benchmarks
5. **Daily validation** - Test against HRC patterns from day one

## References

- `CLAUDE.md` - Project overview and context
- `PLAN.md` - Overall port plan
- `REGEX_ANALYSIS.md` - Feature usage analysis
- `REGEX_EXAMPLES.md` - Real-world pattern examples
- `REGEX_IMPLEMENTATION_GUIDE.md` - Detailed implementation guide
- `native/src/colorer/cregexp/` - C++ implementation reference
