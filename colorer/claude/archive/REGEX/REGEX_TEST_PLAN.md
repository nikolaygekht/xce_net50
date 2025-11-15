# Colorer Regex Engine - Test Plan

## Overview

This document outlines the comprehensive testing strategy for the Colorer regex engine port. The goal is to ensure 95%+ code coverage and validate all features against real-world HRC patterns.

## Test Structure

### Test Projects

```
Far.Colorer.Tests/
└── RegularExpressions/
    ├── Unit/                          # Unit tests (150+ tests)
    │   ├── BasicMatchingTests.cs      # Literals, dots, anchors
    │   ├── QuantifierTests.cs         # *, +, ?, {n,m}
    │   ├── GroupTests.cs              # (...), (?{name}...)
    │   ├── BackreferenceTests.cs      # \yN, \y{name}, \YN, \Y{name}
    │   ├── LookaheadTests.cs          # ?=, ?!
    │   ├── LookbehindTests.cs         # ?#N, ?~N
    │   ├── MetacharacterTests.cs      # ~, \M, \m, ^, $, \b
    │   ├── CharacterClassTests.cs     # [...], [^...], [{L}]
    │   ├── UnicodeTests.cs            # Unicode categories, \x{...}
    │   └── ErrorHandlingTests.cs      # Malformed patterns, edge cases
    │
    ├── Integration/                   # Integration tests (50+ tests)
    │   ├── HrcPatternTests.cs         # Real patterns from HRC files
    │   ├── CrossPatternTests.cs       # START/END pattern pairs
    │   ├── ComplexPatternTests.cs     # Multi-feature combinations
    │   └── RegressionTests.cs         # Bug fixes and edge cases
    │
    ├── Performance/                   # Performance benchmarks
    │   ├── MatchingBenchmarks.cs      # Matching speed
    │   ├── CompilationBenchmarks.cs   # Pattern compilation
    │   └── MemoryBenchmarks.cs        # Memory usage
    │
    └── TestData/                      # Test data files
        ├── HrcPatterns/               # Extracted HRC patterns
        │   ├── lua_patterns.json
        │   ├── c_patterns.json
        │   ├── cpp_patterns.json
        │   ├── perl_patterns.json
        │   ├── python_patterns.json
        │   └── ...
        └── SampleCode/                # Sample code files
            ├── sample.lua
            ├── sample.c
            ├── sample.cpp
            └── ...
```

## Unit Test Plan (150+ tests)

### 1. Basic Matching (20 tests)

```csharp
[Fact] public void Literal_SingleChar_Matches()
[Fact] public void Literal_MultiChar_Matches()
[Fact] public void Literal_NoMatch_ReturnsFalse()
[Fact] public void Dot_MatchesSingleChar()
[Fact] public void Dot_DoesNotMatchNewline()
[Fact] public void Dot_MatchesNewlineWithSingleLineOption()
[Fact] public void Anchor_Caret_MatchesStartOfLine()
[Fact] public void Anchor_Dollar_MatchesEndOfLine()
[Fact] public void Anchor_CaretDollar_MatchesFullLine()
[Fact] public void WordBoundary_MatchesAtWordEdge()
[Fact] public void NonWordBoundary_MatchesInsideWord()
[Fact] public void EmptyPattern_MatchesEmptyString()
[Fact] public void EmptyPattern_MatchesAtAnyPosition()
[Fact] public void CaseInsensitive_MatchesUpperLower()
[Fact] public void CaseSensitive_DoesNotMatchDifferentCase()
[Fact] public void MultiLine_CaretMatchesLineStarts()
[Fact] public void MultiLine_DollarMatchesLineEnds()
[Fact] public void ExtendedMode_IgnoresWhitespace()
[Fact] public void ExtendedMode_AllowsComments()
[Fact] public void EscapedSpecialChar_MatchesLiteral()
```

### 2. Quantifiers (25 tests)

```csharp
// Star (*)
[Fact] public void Star_MatchesZeroOccurrences()
[Fact] public void Star_MatchesOneOccurrence()
[Fact] public void Star_MatchesMultipleOccurrences()
[Fact] public void Star_IsGreedy()
[Fact] public void StarNonGreedy_MatchesMinimal()

// Plus (+)
[Fact] public void Plus_DoesNotMatchZero()
[Fact] public void Plus_MatchesOne()
[Fact] public void Plus_MatchesMultiple()
[Fact] public void Plus_IsGreedy()
[Fact] public void PlusNonGreedy_MatchesMinimal()

// Question (?)
[Fact] public void Question_MatchesZero()
[Fact] public void Question_MatchesOne()
[Fact] public void Question_DoesNotMatchMultiple()
[Fact] public void QuestionNonGreedy_MatchesZero()

// Range {n,m}
[Fact] public void RangeExact_MatchesExactCount()
[Fact] public void RangeMin_MatchesAtLeastN()
[Fact] public void RangeMinMax_MatchesBetweenNAndM()
[Fact] public void Range_IsGreedy()
[Fact] public void RangeNonGreedy_MatchesMinimal()
[Fact] public void Range_WithZeroMin_MatchesZero()
[Fact] public void Range_WithLargeMax_Works()
[Fact] public void Range_InvalidRange_ThrowsException()
[Fact] public void Range_NegativeNumbers_ThrowsException()
[Fact] public void Range_MaxLessThanMin_ThrowsException()
[Fact] public void Range_VeryLargeNumbers_Works()
```

### 3. Groups and Captures (20 tests)

```csharp
// Numeric groups
[Fact] public void Group_CapturesMatch()
[Fact] public void Group_Nested_CapturesAll()
[Fact] public void Group_Multiple_CapturesInOrder()
[Fact] public void Group_Empty_CapturesEmptyString()
[Fact] public void Group_NotMatched_IsNotSuccess()
[Fact] public void Group_Optional_CanBeEmpty()

// Named groups
[Fact] public void NamedGroup_CapturesWithName()
[Fact] public void NamedGroup_AccessByName()
[Fact] public void NamedGroup_AccessByNumber()
[Fact] public void NamedGroup_DuplicateName_ThrowsException()
[Fact] public void NamedGroup_InvalidName_ThrowsException()
[Fact] public void NamedGroup_EmptyName_IsNonCapturing()

// Non-capturing groups
[Fact] public void NonCapturingGroup_DoesNotCapture()
[Fact] public void NonCapturingGroup_StillGroups()

// Group numbering
[Fact] public void GroupNumber_StartsAtOne()
[Fact] public void GroupNumber_Sequential()
[Fact] public void GroupNumber_SkipsNonCapturing()
[Fact] public void GetGroupName_ReturnsCorrectName()
[Fact] public void GetGroupNumber_ReturnsCorrectNumber()
[Fact] public void GetGroupNumber_InvalidName_ReturnsNegative()
```

### 4. Backreferences (30 tests)

```csharp
// Standard backreferences
[Fact] public void Backreference_MatchesCapturedText()
[Fact] public void Backreference_Multiple_MatchesRespectiveGroups()
[Fact] public void Backreference_CaseInsensitive_IgnoresCase()
[Fact] public void Backreference_Empty_MatchesEmpty()
[Fact] public void Backreference_NotCaptured_Fails()
[Fact] public void Backreference_InvalidNumber_ThrowsException()

// Named backreferences (\p{name})
[Fact] public void NamedBackreference_MatchesCapturedText()
[Fact] public void NamedBackreference_InvalidName_ThrowsException()

// Cross-pattern numeric backreferences (\yN)
[Fact] public void CrossPatternBackref_MatchesOtherPattern()
[Fact] public void CrossPatternBackref_WithoutSetBackRef_ThrowsException()
[Fact] public void CrossPatternBackref_MultipleGroups_MatchesCorrect()
[Fact] public void CrossPatternBackref_EmptyGroup_MatchesEmpty()
[Fact] public void CrossPatternBackref_NotCaptured_Fails()
[Fact] public void CrossPatternBackref_CaseInsensitive_Works()

// Cross-pattern named backreferences (\y{name})
[Fact] public void CrossPatternNamedBackref_MatchesByName()
[Fact] public void CrossPatternNamedBackref_InvalidName_ThrowsException()
[Fact] public void CrossPatternNamedBackref_NotCaptured_Fails()

// Negative backreferences (\YN)
[Fact] public void NegativeBackref_FailsWhenMatches()
[Fact] public void NegativeBackref_SucceedsWhenDoesNotMatch()
[Fact] public void NegativeBackref_EmptyGroup_Succeeds()
[Fact] public void NegativeBackref_NotCaptured_Succeeds()
[Fact] public void NegativeBackref_DoesNotAdvancePosition()

// Negative named backreferences (\Y{name})
[Fact] public void NegativeNamedBackref_FailsWhenMatches()
[Fact] public void NegativeNamedBackref_SucceedsWhenDoesNotMatch()
[Fact] public void NegativeNamedBackref_InvalidName_ThrowsException()

// Complex scenarios
[Fact] public void Backreference_WithQuantifier_Works()
[Fact] public void Backreference_InLookahead_Works()
[Fact] public void Backreference_Nested_Works()
[Fact] public void CrossPattern_ChainedBackrefs_Works()
[Fact] public void CrossPattern_UpdatedMatch_UsesNewValues()
```

### 5. Lookahead and Lookbehind (20 tests)

```csharp
// Positive lookahead (?=)
[Fact] public void PositiveLookahead_MatchesWithoutConsuming()
[Fact] public void PositiveLookahead_Fails_DoesNotMatch()
[Fact] public void PositiveLookahead_Complex_Works()
[Fact] public void PositiveLookahead_WithBackreference_Works()

// Negative lookahead (?!)
[Fact] public void NegativeLookahead_SucceedsWhenPatternFails()
[Fact] public void NegativeLookahead_FailsWhenPatternMatches()
[Fact] public void NegativeLookahead_DoesNotConsumeInput()
[Fact] public void NegativeLookahead_Multiple_Works()

// Positive lookbehind (?#N)
[Fact] public void PositiveLookbehind_MatchesPreviousN()
[Fact] public void PositiveLookbehind_FailsWhenNoMatch()
[Fact] public void PositiveLookbehind_AtStartOfInput_Fails()
[Fact] public void PositiveLookbehind_VariableLength_Works()

// Negative lookbehind (?~N)
[Fact] public void NegativeLookbehind_SucceedsWhenNoMatch()
[Fact] public void NegativeLookbehind_FailsWhenMatches()
[Fact] public void NegativeLookbehind_AtStartOfInput_Succeeds()

// Combined
[Fact] public void Lookahead_Multiple_AllMustMatch()
[Fact] public void Lookahead_AndLookbehind_Works()
[Fact] public void Lookahead_Nested_Works()
[Fact] public void Lookahead_WithGroups_DoesNotCapture()
[Fact] public void Lookbehind_LengthTooLong_Fails()
```

### 6. Metacharacters (15 tests)

```csharp
// Tilde (~) - scheme start
[Fact] public void Tilde_MatchesSchemeStart()
[Fact] public void Tilde_DoesNotMatchOtherPositions()
[Fact] public void Tilde_WithOffset_UsesCorrectStart()

// \M - set match start
[Fact] public void BackslashM_MovesEffectiveStart()
[Fact] public void BackslashM_Multiple_UsesLast()
[Fact] public void BackslashM_AffectsGroup0()
[Fact] public void BackslashM_DoesNotAffectOtherGroups()

// \m - set match end
[Fact] public void Backslashm_MovesEffectiveEnd()
[Fact] public void Backslashm_Multiple_UsesLast()
[Fact] public void Backslashm_AffectsGroup0()
[Fact] public void Backslashm_DoesNotAffectOtherGroups()

// Combined
[Fact] public void BackslashM_AndBackslashm_Together_Work()
[Fact] public void PositionMarkers_WithBackref_Work()
[Fact] public void Tilde_WithBackslashM_Works()
[Fact] public void AllPositionMarkers_Complex_Work()
```

### 7. Character Classes (25 tests)

```csharp
// Basic character classes
[Fact] public void CharClass_SingleChar_Matches()
[Fact] public void CharClass_MultipleChars_MatchesAny()
[Fact] public void CharClass_Range_MatchesInRange()
[Fact] public void CharClass_MultipleRanges_Work()
[Fact] public void CharClass_MixedCharsAndRanges_Work()
[Fact] public void CharClass_Empty_ThrowsException()

// Negated classes
[Fact] public void NegatedCharClass_MatchesOutsideSet()
[Fact] public void NegatedCharClass_DoesNotMatchInside()

// Predefined classes
[Fact] public void Digit_MatchesDigits()
[Fact] public void NonDigit_MatchesNonDigits()
[Fact] public void Word_MatchesWordChars()
[Fact] public void NonWord_MatchesNonWordChars()
[Fact] public void Whitespace_MatchesWhitespace()
[Fact] public void NonWhitespace_MatchesNonWhitespace()

// Unicode character classes
[Fact] public void UnicodeClass_Letter_MatchesLetters()
[Fact] public void UnicodeClass_DecimalDigit_MatchesDigits()
[Fact] public void UnicodeClass_Punctuation_MatchesPunctuation()
[Fact] public void UnicodeClass_ALL_MatchesAll()
[Fact] public void UnicodeClass_ASSIGNED_MatchesAssigned()
[Fact] public void UnicodeClass_UNASSIGNED_MatchesUnassigned()

// Character class operations
[Fact] public void CharClass_Union_CombinesSets()
[Fact] public void CharClass_Subtraction_RemovesChars()
[Fact] public void CharClass_Intersection_FindsCommon()
[Fact] public void CharClass_ComplexOperation_Works()
[Fact] public void CharClass_NestedOperations_Work()
```

### 8. Error Handling (15 tests)

```csharp
[Fact] public void MalformedPattern_UnmatchedParen_ThrowsException()
[Fact] public void MalformedPattern_UnmatchedBracket_ThrowsException()
[Fact] public void MalformedPattern_InvalidQuantifier_ThrowsException()
[Fact] public void MalformedPattern_InvalidRange_ThrowsException()
[Fact] public void MalformedPattern_InvalidBackreference_ThrowsException()
[Fact] public void MalformedPattern_InvalidEscape_ThrowsException()
[Fact] public void MalformedPattern_InvalidUnicode_ThrowsException()
[Fact] public void MalformedPattern_InvalidCharClass_ThrowsException()
[Fact] public void MalformedPattern_EmptyGroup_Works()
[Fact] public void VeryLongPattern_Works()
[Fact] public void VeryLongInput_DoesNotStackOverflow()
[Fact] public void VeryDeepNesting_DoesNotStackOverflow()
[Fact] public void RepeatedMatching_DoesNotLeak()
[Fact] public void ConcurrentMatching_IsThreadSafe()
[Fact] public void NullInput_ThrowsArgumentNullException()
```

## Integration Test Plan (50+ tests)

### 1. Real HRC Patterns (30 tests)

```csharp
// Extract real patterns from HRC files and test them
public class HrcPatternTests
{
    [Theory]
    [MemberData(nameof(GetLuaPatterns))]
    public void LuaPatterns_Match(string pattern, string input, bool shouldMatch)
    {
        var regex = new ColorerRegex(pattern);
        Assert.Equal(shouldMatch, regex.IsMatch(input));
    }

    public static IEnumerable<object[]> GetLuaPatterns()
    {
        // Load from TestData/HrcPatterns/lua_patterns.json
        yield return new object[] { @"\[(?{OpenBracket}=*)\[", "[===[", true };
        yield return new object[] { @"--(?{Comment}.*)", "-- comment", true };
        yield return new object[] { @"function\s+(?{FuncName}\w+)", "function foo", true };
        // ... 30+ patterns
    }

    [Theory]
    [MemberData(nameof(GetCPatterns))]
    public void CPatterns_Match(string pattern, string input, bool shouldMatch) { }

    [Theory]
    [MemberData(nameof(GetCppPatterns))]
    public void CppPatterns_Match(string pattern, string input, bool shouldMatch) { }

    [Theory]
    [MemberData(nameof(GetPerlPatterns))]
    public void PerlPatterns_Match(string pattern, string input, bool shouldMatch) { }

    [Theory]
    [MemberData(nameof(GetPythonPatterns))]
    public void PythonPatterns_Match(string pattern, string input, bool shouldMatch) { }
}
```

### 2. Cross-Pattern Tests (10 tests)

```csharp
public class CrossPatternTests
{
    [Fact]
    public void LuaMultilineComment_MatchesCorrectly()
    {
        // START: --\[(?{OpenBracket}=*)\[
        // END: \](?{CloseBracket}\y1)\]

        var text = "text --[===[ comment ]===] more";

        var start = new ColorerRegex(@"--\[(?{OpenBracket}=*)\[");
        var startMatch = start.Match(text);
        Assert.True(startMatch.Success);
        Assert.Equal("===", startMatch.GetGroupSpan("OpenBracket").ToString());

        var end = new ColorerRegex(@"\](?{CloseBracket}\y1)\]");
        end.SetBackReference(start, startMatch);

        var remainingText = text.AsSpan(startMatch.Index + startMatch.Length);
        var endMatch = end.Match(remainingText);
        Assert.True(endMatch.Success);
    }

    [Fact]
    public void CRawString_MatchesCorrectly()
    {
        // START: R"(?{Delimiter}[^\(\)\\s]{0,16})\(
        // END: \)(?{EndDelim}\y1)"

        var text = @"R""delim(content)delim""";

        var start = new ColorerRegex(@"R\""(?{Delimiter}[^\(\)\\s]{0,16})\(");
        var startMatch = start.Match(text);
        Assert.True(startMatch.Success);
        Assert.Equal("delim", startMatch.GetGroupSpan("Delimiter").ToString());

        var end = new ColorerRegex(@"\)(?{EndDelim}\y1)\""");
        end.SetBackReference(start, startMatch);

        var remainingText = text.AsSpan(startMatch.Index + startMatch.Length);
        var endMatch = end.Match(remainingText);
        Assert.True(endMatch.Success);
    }

    // Similar tests for:
    // - Python triple-quoted strings
    // - Perl heredoc
    // - VB.NET block structures
    // - HTML paired tags
    // - C# interpolated strings
    // - Markdown code fences
}
```

### 3. Complex Pattern Tests (10 tests)

```csharp
public class ComplexPatternTests
{
    [Fact]
    public void NestedGroups_WithBackreferences_Works()
    {
        var pattern = @"((\w+)=\2)";
        var regex = new ColorerRegex(pattern);

        Assert.True(regex.IsMatch("foo=foo"));
        Assert.False(regex.IsMatch("foo=bar"));
    }

    [Fact]
    public void MultipleBackreferences_WithLookahead_Works()
    {
        var pattern = @"(\w+)\s+\1(?=\s)";
        var regex = new ColorerRegex(pattern);

        Assert.True(regex.IsMatch("foo foo "));
        Assert.False(regex.IsMatch("foo bar "));
    }

    [Fact]
    public void PositionMarkers_WithBackreferences_Works()
    {
        var pattern = @"\M(\w+)\m";
        var regex = new ColorerRegex(pattern);

        var match = regex.Match("  word  ");
        Assert.True(match.Success);
        // Effective start should be moved by \M
        Assert.Equal(2, match.EffectiveStart);
    }

    // Tests for combinations of:
    // - Backreferences + lookahead
    // - Position markers + groups
    // - Character classes + quantifiers + backreferences
    // - Unicode classes + lookahead
    // - All features together
}
```

## Performance Test Plan

### Benchmark Setup

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class RegexBenchmarks
{
    private string _input;
    private ColorerRegex _regex;

    [GlobalSetup]
    public void Setup()
    {
        _input = File.ReadAllText("TestData/SampleCode/sample.c");
        _regex = new ColorerRegex(@"(\w+)\s*=\s*(\w+)");
    }

    [Benchmark]
    public void SimplePattern_IsMatch()
    {
        _regex.IsMatch(_input);
    }

    [Benchmark]
    public void SimplePattern_Match()
    {
        _regex.Match(_input);
    }

    [Benchmark]
    public void ComplexPattern_WithBackreferences()
    {
        var regex = new ColorerRegex(@"(\w+)=\1");
        regex.IsMatch(_input);
    }

    [Benchmark]
    public void CrossPattern_Backreference()
    {
        var start = new ColorerRegex(@"\[(?{OpenBracket}=*)\[");
        var startMatch = start.Match("[===[");

        var end = new ColorerRegex(@"\](?{CloseBracket}\y1)\]");
        end.SetBackReference(start, startMatch);
        end.IsMatch("]===]");
    }

    [Benchmark]
    public void PatternCompilation()
    {
        new ColorerRegex(@"(\w+)\s*=\s*(\d+)");
    }
}
```

### Performance Targets

| Operation | Target | Baseline (C++) |
|-----------|--------|----------------|
| Simple pattern match | < 100 ns | 50 ns |
| Complex pattern match | < 500 ns | 250 ns |
| Cross-pattern backref | < 1 μs | 500 ns |
| Pattern compilation | < 10 μs | 5 μs |
| Large file (10K lines) | < 100 ms | 50 ms |
| Memory per match | < 500 bytes | 200 bytes |

## Test Data

### HRC Pattern Extraction

Extract patterns from key HRC files into JSON format:

```json
// TestData/HrcPatterns/lua_patterns.json
{
  "patterns": [
    {
      "id": "lua_multiline_comment_start",
      "file": "lua.hrc",
      "line": 118,
      "pattern": "--\\[(?{OpenBracket}=*)\\[",
      "description": "Lua multiline comment opening",
      "test_cases": [
        { "input": "--[=[", "should_match": true },
        { "input": "--[[", "should_match": true },
        { "input": "--[", "should_match": false }
      ]
    },
    {
      "id": "lua_multiline_comment_end",
      "file": "lua.hrc",
      "line": 124,
      "pattern": "\\](?{CloseBracket}\\y1)\\]",
      "description": "Lua multiline comment closing",
      "requires_backref": "lua_multiline_comment_start",
      "test_cases": [
        { "input": "]=]", "captured_delim": "=", "should_match": true },
        { "input": "]]", "captured_delim": "", "should_match": true },
        { "input": "]==]", "captured_delim": "=", "should_match": false }
      ]
    }
  ]
}
```

## Test Execution Strategy

### Phase 1: Foundation Tests (Week 1)
- Run after each feature implementation
- Target: 50+ tests passing
- Focus: Basic matching, quantifiers, groups

### Phase 2: Core Feature Tests (Week 2)
- Run daily
- Target: 100+ tests passing
- Focus: Backreferences, lookahead, position markers

### Phase 3: Advanced Feature Tests (Week 3)
- Run with each commit
- Target: 150+ tests passing
- Focus: Named backreferences, negative backreferences

### Phase 4: Integration Tests (Week 4)
- Run against real HRC patterns
- Target: All integration tests passing
- Focus: Real-world usage validation

### Phase 5: Performance Tests (Week 5)
- Run benchmarks
- Compare with C++ version
- Optimize based on results

## Continuous Integration

### CI Pipeline

```yaml
name: Regex Engine Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --no-restore

      - name: Run unit tests
        run: dotnet test --no-build --verbosity normal --filter "Category=Unit"

      - name: Run integration tests
        run: dotnet test --no-build --verbosity normal --filter "Category=Integration"

      - name: Run performance tests
        run: dotnet run --project Far.Colorer.Tests/Performance -- --job short

      - name: Upload coverage
        uses: codecov/codecov-action@v3
        with:
          files: ./coverage.xml
```

## Success Criteria

### Unit Tests
- [ ] 150+ tests written
- [ ] 95%+ passing
- [ ] 90%+ code coverage
- [ ] All critical paths covered

### Integration Tests
- [ ] 50+ tests written
- [ ] 100% passing
- [ ] All major HRC files covered
- [ ] Real-world patterns validated

### Performance Tests
- [ ] Benchmarks established
- [ ] < 2x slower than C++
- [ ] No memory leaks
- [ ] Acceptable for production use

## Next Steps

1. Set up test project structure
2. Install test frameworks (xUnit, FluentAssertions, BenchmarkDotNet)
3. Create test data files
4. Write initial unit tests (Phase 1)
5. Run tests with each implementation milestone
