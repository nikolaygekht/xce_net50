# Regex Implementation Guide for .NET Port

## Executive Summary

This guide provides recommendations for implementing the Colorer regex engine in .NET based on comprehensive analysis of 349 HRC files.

**Key Finding**: Colorer's regex engine cannot be replaced with .NET's `System.Text.RegularExpressions` due to fundamental incompatibilities in cross-pattern backreferences (`\yN`, `\y{name}`).

**Recommendation**: Port the existing C++ regex engine to C#, focusing on the most common features first.

---

## Scope of Work

### Files Analyzed
- 349 HRC (syntax definition) files from `/data/hrc/`
- 5 key reference files totaling 2,188 lines
- Features documented with exact line numbers and code examples

### Output Documents
- **REGEX_ANALYSIS.md** (17 KB, 526 lines) - Comprehensive feature analysis
- **REGEX_EXAMPLES.md** (13 KB, 441 lines) - 12 detailed code examples
- **REGEX_IMPLEMENTATION_GUIDE.md** (this document)

---

## Feature Priority Matrix

### Tier 1: Essential (80% of usage) - Weeks 1-2

| Feature | Usage | Files | Complexity | Est. Hours |
|---------|-------|-------|-----------|-----------|
| `\yN` backreferences | 50+ | c, cpp, lua, python | HIGH | 40 |
| Negative lookahead `?!` | 20-30 | cpp, asm, html | MEDIUM | 15 |
| Basic `\M` support | 25+ | asm, c, vb | MEDIUM | 20 |
| Tilde `~` scheme start | 30+ | c, cpp, perl | MEDIUM | 15 |
| Named groups `(?{name})` | 15-20 | cpp, csharp, perl | LOW | 10 |
| **Subtotal** | | | | **100 hours** |

### Tier 2: Important (15% of usage) - Weeks 3-4

| Feature | Usage | Files | Complexity | Est. Hours |
|---------|-------|-------|-----------|-----------|
| `\y{name}` named backrefs | 15-20 | python, csharp, markdown | MEDIUM | 20 |
| `\Y1-3` negative backrefs | 10-15 | vbasic, html, cobol | HIGH | 30 |
| Backslash-m `\m` | 5-10 | c, perl | LOW | 10 |
| Quantifier offset `?~N` | 5-10 | cpp, c, java | MEDIUM | 15 |
| Advanced `\M` features | 5-10 | assembly, vb | MEDIUM | 10 |
| **Subtotal** | | | | **85 hours** |

### Tier 3: Optional (5% of usage) - Week 5

| Feature | Usage | Files | Complexity | Est. Hours |
|---------|-------|-------|-----------|-----------|
| Unicode classes `[{L}]` | 3-5 | j2ee (generated) | LOW | 10 |
| Character class subtraction | 2 | j2ee (generated) | LOW | 5 |
| Advanced lookahead | 5-10 | cpp | HIGH | 15 |
| **Subtotal** | | | | **30 hours** |

### **Total Estimated Effort: 215 hours (5-6 weeks for 1 developer)**

---

## Implementation Roadmap

### Week 1: Foundation and Tier 1 Basics

**Goals**: Basic regex engine structure with most common features

**Tasks**:
1. Create `ColoreRexEngine` class structure
   - Pattern compilation cache
   - Match state machine
   - Group tracking system
2. Implement numeric backreferences `\yN`
   - Store capture groups from start pattern
   - Reference them in end pattern
   - Handle group numbering
3. Implement negative lookahead `?!`
   - Basic lookahead assertion
   - Negative version
   - Integration with main matching

**Deliverables**:
- [ ] Basic engine structure
- [ ] Numeric backreferences working
- [ ] Lookahead assertions working
- [ ] Unit tests for above

**Validation**:
- [ ] Test against `lua.hrc` comment patterns
- [ ] Test against `c.hrc` string patterns
- [ ] Test against `perl-brackets.ent.hrc`

---

### Week 2: Tier 1 Completion

**Goals**: Complete remaining Tier 1 features

**Tasks**:
1. Implement `\M` (move match start)
   - Position tracking in regex state
   - Region calculation with offset
2. Implement tilde `~` scheme boundary
   - Scheme context detection
   - Pattern positioning logic
3. Implement named groups `(?{name}...)`
   - Named capture group storage
   - Reference resolution

**Deliverables**:
- [ ] All Tier 1 features complete
- [ ] Integration tests with HRC files
- [ ] Performance benchmarks

**Validation**:
- [ ] Test against `c.hrc` preprocessor patterns (lines 218-225)
- [ ] Test against `asm.hrc` labels (lines 268-269)
- [ ] All 80% use cases covered

---

### Week 3: Tier 2 Features Part 1

**Goals**: Implement advanced backreferences

**Tasks**:
1. Named backreferences `\y{name}`
   - Storage of named groups
   - Reference lookup by name
   - Support in both start/end patterns
2. Negative backreferences `\Y1-3`
   - Negative matching logic
   - Integration with backreference system
   - Group validation

**Deliverables**:
- [ ] Named and negative backreferences working
- [ ] Test cases for VB.NET block nesting
- [ ] Documentation of advanced features

**Validation**:
- [ ] Test against `vbasic.hrc` block structures (lines 71-79)
- [ ] Test against `python.hrc` string patterns (lines 117-153)
- [ ] Test against `html.hrc` tag pairing

---

### Week 4: Tier 2 Features Part 2

**Goals**: Complete Tier 2 with remaining features

**Tasks**:
1. Backslash-m `\m` (move match end)
   - End position tracking
   - Range calculations
2. Quantifier offset `?~N`
   - Group skip logic
   - Pattern range calculations
3. Optimization and refactoring
   - Cache compiled patterns
   - Optimize group lookups
   - Performance tuning

**Deliverables**:
- [ ] All Tier 2 features complete
- [ ] Integrated with HRC parser
- [ ] Performance optimizations
- [ ] Comprehensive documentation

**Validation**:
- [ ] Test against `perl-brackets.ent.hrc` complex patterns
- [ ] Test against `cpp.hrc` function signatures
- [ ] Benchmark against real HRC files

---

### Week 5: Tier 3 and Integration

**Goals**: Optional features and system integration

**Tasks**:
1. Unicode character classes (optional)
   - `[{L}]`, `[{Nd}]` support
   - Character class subtraction
2. Integration testing
   - Run against full HRC library
   - Compare with C++ output
   - Validate color region mappings
3. Bug fixes and edge cases
   - Handle empty matches
   - Complex pattern interactions
   - Performance edge cases

**Deliverables**:
- [ ] Optional features (if time permits)
- [ ] Full integration with parser
- [ ] Bug-free implementation
- [ ] Performance validated

---

## Key Implementation Details

### 1. Cross-Pattern Backreferences

**Challenge**: Standard regex engines only support backreferences within a single pattern. Colorer uses backreferences **across patterns** (start pattern referenced in end pattern).

**Solution Structure**:
```csharp
class CapturedGroup
{
    public int Number { get; set; }        // \y1, \y2, etc.
    public string Name { get; set; }       // \y{name}
    public string Content { get; set; }    // Captured content
    public int StartPos { get; set; }
    public int EndPos { get; set; }
}

class PatternContext
{
    public Dictionary<int, CapturedGroup> NumericGroups { get; set; }
    public Dictionary<string, CapturedGroup> NamedGroups { get; set; }
    public string MatchedContent { get; set; }
    
    public CapturedGroup GetGroup(int num) => NumericGroups[num];
    public CapturedGroup GetGroup(string name) => NamedGroups[name];
}
```

### 2. Pattern Compilation Strategy

**Cache key**: Hash of pattern string + mode flags
**Storage**: Thread-safe dictionary with size limit (e.g., 1000 patterns)

```csharp
class PatternCompiler
{
    private Dictionary<string, CompiledPattern> _cache;
    private readonly int _cacheSize = 1000;
    
    public CompiledPattern Compile(string pattern, RegexOptions options)
    {
        string key = $"{pattern}:{options}";
        if (_cache.TryGetValue(key, out var compiled))
            return compiled;
            
        var compiled = new CompiledPattern(pattern, options);
        _cache[key] = compiled;
        return compiled;
    }
}
```

### 3. State Machine for Matching

```csharp
enum MatchState
{
    Unmatched,          // Pattern not yet matched
    PartialMatch,       // Part of pattern matched
    FullMatch,          // Pattern completely matched
    Failed,             // Match failed
    NegativeLookahead   // Lookahead assertion failed
}

class MatchContext
{
    public MatchState State { get; set; }
    public int CurrentPosition { get; set; }
    public PatternContext PatternContext { get; set; }
    public List<RegionInfo> HighlightedRegions { get; set; }
}
```

---

## Testing Strategy

### Unit Tests (Week 1-2)
- Test each feature in isolation
- Test combinations of features
- Test edge cases (empty matches, multiple groups)
- Test performance with large patterns

### Integration Tests (Week 3-4)
- Test patterns from real HRC files
- Compare output with C++ engine
- Validate region highlighting
- Test with complete HRC files (python, c, lua, perl)

### Performance Tests (Week 4-5)
- Benchmark pattern compilation
- Benchmark matching operations
- Test with large documents
- Memory usage profiling

### Reference Test Files
```
Primary (must work):
- data/hrc/base/lua.hrc         (comments, multiline blocks)
- data/hrc/base/python.hrc      (complex strings, f-strings)
- data/hrc/base/c.hrc           (preprocessor, raw strings)
- data/hrc/base/gen/perl-brackets.ent.hrc  (complex delimiters)

Secondary (important):
- data/hrc/base/vbasic.hrc      (block structure)
- data/hrc/inet/html.hrc        (tag pairing)
- data/hrc/base/cpp.hrc         (templates vs operators)

Reference (optional):
- data/hrc/rare/gen/j2ee/*.hrc  (Unicode classes)
```

---

## API Design

### Public Interface

```csharp
namespace Colorer.Regex
{
    public class ColorerRegexEngine
    {
        // Compile and cache a regex pattern
        public CompiledPattern Compile(string pattern);
        public CompiledPattern Compile(string pattern, RegexOptions options);
        
        // Match against input
        public MatchResult Match(CompiledPattern pattern, string input, int offset = 0);
        
        // Find all groups in a match
        public Group[] GetGroups(MatchResult match);
        public Group GetGroup(MatchResult match, int index);
        public Group GetGroup(MatchResult match, string name);
        
        // Cache management
        public void ClearCache();
        public CacheStats GetCacheStats();
    }
    
    public class MatchResult
    {
        public bool Success { get; }
        public int Index { get; }
        public int Length { get; }
        public Group[] Groups { get; }
        public Dictionary<string, Group> NamedGroups { get; }
    }
    
    public class Group
    {
        public int Index { get; }
        public int Length { get; }
        public string Value { get; }
        public bool Success { get; }
    }
    
    public class CompiledPattern
    {
        public string Pattern { get; }
        public RegexOptions Options { get; }
    }
}
```

---

## Common Pitfalls to Avoid

1. **Don't use .NET Regex as base** - Incompatible architecture
2. **Don't underestimate negative backreferences** - Complex logic
3. **Don't forget cross-pattern state** - Groups persist between start/end
4. **Don't ignore performance** - Caching is critical
5. **Don't skip Unicode handling** - Some HRC files rely on it
6. **Don't over-complicate early** - Start simple, add features incrementally
7. **Don't forget about thread-safety** - Cache needs synchronization

---

## Documentation Requirements

### For Developers
- Architecture overview (1 day)
- API documentation with examples (1 day)
- Implementation notes for each feature (2 days)

### For Users
- Feature coverage matrix
- Known limitations
- Performance characteristics
- Migration guide from C++

---

## Risk Assessment

### High Risk
- Cross-pattern backreferences: **MITIGATED** - Clear architecture defined
- Performance with complex patterns: **MITIGATED** - Caching strategy ready
- Unicode support: **MITIGATED** - Only used in 3-5 files

### Medium Risk
- Negative backreferences complexity
- Integration with existing parser
- Backward compatibility

### Low Risk
- Basic lookahead
- Named groups
- Position markers

---

## Success Criteria

### MVP (Minimum Viable Product)
- [ ] All Tier 1 features working
- [ ] 80% of HRC files parse correctly
- [ ] Performance acceptable (< 10ms per file)
- [ ] No major bugs with test suite
- [ ] Documentation complete

### Full Release
- [ ] All Tier 2 features working
- [ ] 95% of HRC files parse correctly
- [ ] Performance optimized (< 5ms per file)
- [ ] Edge cases handled
- [ ] Comprehensive documentation
- [ ] User guide and examples

---

## Related Documents

- **REGEX_ANALYSIS.md** - Feature analysis with statistics
- **REGEX_EXAMPLES.md** - 12 detailed code examples from HRC files
- **CLAUDE.md** - Project overview and architecture
- **PLAN.md** - Overall project plan

---

## Estimated Timeline

| Phase | Duration | Features | Status |
|-------|----------|----------|--------|
| Planning | 1 week | Analysis & design | DONE |
| Tier 1 | 2 weeks | Basic features | READY |
| Tier 2 | 2 weeks | Advanced features | READY |
| Tier 3 | 1 week | Optional features | READY |
| Integration | 1 week | Testing & fixes | READY |
| **Total** | **5-6 weeks** | Full engine | **READY** |

---

## Next Steps

1. Review this guide with development team
2. Set up development environment
3. Create unit test framework
4. Start Week 1 implementation
5. Weekly status reviews with stakeholder

