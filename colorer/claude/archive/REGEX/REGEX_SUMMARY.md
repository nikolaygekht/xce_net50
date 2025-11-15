# Colorer Regex Engine Port - Executive Summary

## Quick Start

This directory contains comprehensive planning documents for porting the Colorer regex engine to .NET. Start here:

1. **Read this summary** (you are here)
2. **Review `REGEX_PORT_PLAN.md`** - Implementation roadmap
3. **Check `REGEX_TEST_PLAN.md`** - Testing strategy
4. **Reference `REGEX_ANALYSIS.md`** - Feature usage data

## What is Colorer Regex?

Colorer uses a custom regular expression engine with unique features not available in standard regex implementations:

- **Cross-pattern backreferences**: Capture groups from START pattern referenced in END pattern (`\yN`, `\y{name}`)
- **Position markers**: Adjust match boundaries (`\M`, `\m`, `~`)
- **Negative backreferences**: Match when backref does NOT match (`\YN`, `\Y{name}`)
- Standard features: lookahead, lookbehind, Unicode character classes

## Why Can't We Use .NET Regex?

`System.Text.RegularExpressions` cannot support cross-pattern backreferences because:
- Each regex pattern is compiled independently
- No way to share capture groups between patterns
- Fundamental architectural limitation

**Solution**: Port the C++ regex engine to C#, optimized with modern .NET features.

## Implementation Timeline

| Phase | Duration | Features | Tests |
|-------|----------|----------|-------|
| 1. Foundation | Week 1 | Basic matching, quantifiers, groups | 50+ |
| 2. Core Features | Week 2 | Cross-pattern backrefs, lookahead, position markers | 100+ |
| 3. Advanced | Week 3 | Named/negative backrefs, lookbehind | 150+ |
| 4. Optimization | Week 4-5 | Unicode classes, performance tuning | 200+ |
| 5. Integration | Week 5-6 | HRC validation, benchmarks | All passing |
| **Total** | **5-6 weeks** | **All features** | **95%+ coverage** |

## Feature Priority

### Tier 1 (80% usage) - Weeks 1-2
- `\yN` - Cross-pattern numeric backreferences
- `?!` - Negative lookahead
- `\M` - Move match start
- `~` - Scheme start marker
- `(?{name}...)` - Named groups

### Tier 2 (15% usage) - Week 3
- `\y{name}` - Cross-pattern named backreferences
- `\YN`, `\Y{name}` - Negative backreferences
- `\m` - Move match end
- `?#N`, `?~N` - Lookbehind

### Tier 3 (5% usage) - Week 4-5
- `[{L}]`, `[{Nd}]` - Unicode character classes
- Character class operations (union, intersection, subtraction)

## Performance Strategy

### Key Optimizations

1. **Span<char> everywhere**
   ```csharp
   public bool IsMatch(ReadOnlySpan<char> input)
   public ColorerMatch Match(ReadOnlySpan<char> input)
   ```

2. **Memory pooling**
   ```csharp
   ArrayPool<CaptureGroup>.Shared.Rent(size)
   ```

3. **Stack allocation for small buffers**
   ```csharp
   Span<CaptureGroup> groups = stackalloc CaptureGroup[16];
   ```

4. **Compiled pattern caching**
   ```csharp
   ConcurrentDictionary<string, RegexNode> Cache
   ```

### Performance Targets

- Simple match: < 100 ns (vs 50 ns C++)
- Complex match: < 500 ns (vs 250 ns C++)
- Cross-pattern: < 1 μs (vs 500 ns C++)
- Large file: < 100 ms (vs 50 ms C++)

**Goal**: Within 2x of C++ performance

## Testing Strategy

### Coverage

- **150+ unit tests** - All features, edge cases
- **50+ integration tests** - Real HRC patterns
- **Performance benchmarks** - Compare with C++
- **Target**: 95%+ code coverage

### Test Categories

1. **Basic matching** (20 tests) - Literals, dots, anchors
2. **Quantifiers** (25 tests) - `*`, `+`, `?`, `{n,m}`, non-greedy
3. **Groups** (20 tests) - Numeric, named, non-capturing
4. **Backreferences** (30 tests) - Standard, cross-pattern, negative
5. **Lookahead/behind** (20 tests) - All variants
6. **Metacharacters** (15 tests) - `~`, `\M`, `\m`
7. **Character classes** (25 tests) - Basic, Unicode, operations
8. **Error handling** (15 tests) - Malformed patterns, edge cases

### Integration Tests

Extract real patterns from HRC files:
- `lua.hrc` - Multiline comments with dynamic delimiters
- `c.hrc` - Raw strings, preprocessor
- `cpp.hrc` - Function signatures, templates
- `perl.hrc` - Substitution, heredoc
- `python.hrc` - Triple-quoted strings

## Architecture Overview

```
ColorerRegex                  # Public API
    ↓
RegexCompiler                 # Pattern → AST
    ↓
RegexNode (AST)              # Pattern representation
    ↓
RegexMatcher                 # Execution engine (Span-based)
    ↓
ColorerMatch                 # Result with captures
```

### Key Classes

```csharp
public sealed class ColorerRegex
{
    public ColorerRegex(string pattern, RegexOptions options = RegexOptions.None);
    public bool IsMatch(ReadOnlySpan<char> input);
    public ColorerMatch Match(ReadOnlySpan<char> input);
    public void SetBackReference(ColorerRegex regex, ColorerMatch match);
}

public sealed class ColorerMatch
{
    public bool Success { get; }
    public int Index { get; }
    public int Length { get; }
    public CaptureGroup[] Groups { get; }
    public ReadOnlySpan<char> GetGroupSpan(int groupNumber);
    public int EffectiveStart { get; } // For \M support
    public int EffectiveEnd { get; }   // For \m support
}
```

## Cross-Pattern Backreference Example

```csharp
// Lua multiline comment: --[===[ comment ]===]

// 1. Match START pattern
var start = new ColorerRegex(@"--\[(?{OpenBracket}=*)\[");
var startMatch = start.Match("--[===[");
// Captures: OpenBracket = "==="

// 2. Create END pattern and link to START
var end = new ColorerRegex(@"\](?{CloseBracket}\y1)\]");
end.SetBackReference(start, startMatch);

// 3. Match END - \y1 references START's group 1
var endMatch = end.Match("]===]");
// Matches because ]===] has same delimiter as captured in START

endMatch = end.Match("]==]");
// Fails because ]==] has different delimiter
```

## Project Structure

```
net/Far.Colorer/
└── RegularExpressions/
    ├── ColorerRegex.cs               # Main API
    ├── ColorerMatch.cs               # Match result
    ├── RegexCompiler.cs              # Pattern parser
    ├── RegexMatcher.cs               # Execution engine
    ├── RegexNode.cs                  # AST node
    ├── Enums/
    │   ├── RegexOperator.cs          # *, +, ?, etc.
    │   └── MetaSymbol.cs             # ~, \M, \m, etc.
    ├── Nodes/
    │   ├── LiteralNode.cs
    │   ├── QuantifierNode.cs
    │   ├── GroupNode.cs
    │   ├── BackreferenceNode.cs
    │   └── ... (10+ node types)
    └── Internal/
        ├── MatchState.cs             # Matching state
        ├── CaptureGroup.cs           # Group storage
        └── RegexCache.cs             # Pattern cache

net/Far.Colorer.Tests/
└── RegularExpressions/
    ├── Unit/
    │   ├── BasicMatchingTests.cs
    │   ├── BackreferenceTests.cs
    │   └── ... (10+ test classes)
    ├── Integration/
    │   └── HrcPatternTests.cs
    └── Performance/
        └── RegexBenchmarks.cs
```

## Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|------------|
| Cross-pattern complexity | HIGH | Extensive unit tests, validate with all HRC usage |
| Performance vs C++ | MEDIUM | Span<char>, ArrayPool, profiling, optimize hot paths |
| Unicode differences | LOW | Use .NET UnicodeCategory, test diverse input |
| API design changes | LOW | Keep internals internal, careful public API |

## Success Criteria

### Minimum Viable Product (MVP)
- [ ] All Tier 1 & 2 features working
- [ ] 95% of HRC patterns supported
- [ ] 150+ unit tests passing
- [ ] Performance < 3x slower than C++

### Full Release
- [ ] All features (Tier 1-3) working
- [ ] 99%+ HRC patterns supported
- [ ] 200+ unit tests passing
- [ ] 95%+ code coverage
- [ ] Performance < 2x slower than C++
- [ ] Complete documentation

## Documentation Index

| Document | Purpose | Size | Audience |
|----------|---------|------|----------|
| `REGEX_SUMMARY.md` | Overview and quick start | 2 KB | Everyone |
| `REGEX_PORT_PLAN.md` | Implementation roadmap | 35 KB | Developers |
| `REGEX_TEST_PLAN.md` | Testing strategy | 18 KB | QA, Developers |
| `REGEX_ANALYSIS.md` | Feature usage data | 17 KB | Architects |
| `REGEX_EXAMPLES.md` | Real-world patterns | 13 KB | Developers |
| `REGEX_IMPLEMENTATION_GUIDE.md` | Detailed guide | 13 KB | Lead Developer |
| `README_REGEX_ANALYSIS.md` | Documentation index | 5 KB | Everyone |

## Next Steps

### For Project Manager
1. Review timeline (5-6 weeks)
2. Allocate developer resources
3. Set up weekly checkpoints
4. Track against milestones

### For Tech Lead
1. Review architecture in `REGEX_PORT_PLAN.md`
2. Validate approach with team
3. Set up development environment
4. Create initial project structure

### For Developer
1. Read `REGEX_PORT_PLAN.md` - Understand architecture
2. Read `REGEX_TEST_PLAN.md` - Understand testing
3. Review `REGEX_EXAMPLES.md` - See real patterns
4. Start with Phase 1: Foundation

### For QA
1. Read `REGEX_TEST_PLAN.md`
2. Prepare test data from HRC files
3. Set up test infrastructure
4. Create test automation framework

## Quick Reference

### Most Common Patterns

```regex
\yN              # Cross-pattern numeric backref (50+ files)
?!               # Negative lookahead (30+ files)
~                # Scheme start (30+ files)
\M               # Move match start (25+ files)
(?{name}...)     # Named group (20+ files)
```

### Test Files to Validate Against

- `data/base/hrc/base/lua.hrc` (lines 118-124)
- `data/base/hrc/base/c.hrc` (lines 218-225)
- `data/base/hrc/base/cpp.hrc` (lines 91-94)
- `data/base/hrc/base/perl.hrc` (lines 156-158)
- `data/base/hrc/base/python.hrc` (lines 117-153)

### Performance Monitoring

```bash
# Run benchmarks
dotnet run --project Far.Colorer.Tests/Performance -c Release

# Profile memory
dotnet-trace collect --profile gc-collect -- dotnet run

# Measure coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Questions?

- **Architecture questions**: See `REGEX_PORT_PLAN.md`
- **Feature questions**: See `REGEX_ANALYSIS.md`
- **Testing questions**: See `REGEX_TEST_PLAN.md`
- **Implementation examples**: See `REGEX_EXAMPLES.md`

## Status

- [x] Analysis complete (349 HRC files analyzed)
- [x] Documentation complete (6 documents, 100+ pages)
- [x] Architecture designed
- [x] Test plan created
- [ ] Implementation started
- [ ] Tests written
- [ ] Features complete
- [ ] Performance validated
- [ ] Integration complete

**Ready to start implementation!**
