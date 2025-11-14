# Named Capture Groups - Implementation Fix + Test Plan

**Status**: Ready to implement
**Date**: 2025-11-13
**Estimated Time**: 1-2 hours
**Estimated Tests**: 35-42 tests

---

## Implementation Fix Required

### Step 1: Expose Named Groups from Compiler

**File**: `net/Far.Colorer/RegularExpressions/Internal/CRegExpCompiler.cs`

Add a property to expose the named groups dictionary:
```csharp
public IReadOnlyDictionary<string, int> NamedGroups => namedGroups;
```

**Location**: After the constructor, around line 50
**Lines to Add**: 1 line

---

### Step 2: Populate CaptureGroup Names in ColorerRegex

**File**: `net/Far.Colorer/RegularExpressions/Internal/ColorerRegex.cs`

**Current Code** (lines 78-104):
```csharp
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
```

**Fixed Code**:
```csharp
var captures = new List<CaptureGroup>();

// Get named groups from compiler
var namedGroups = compiler?.NamedGroups ??
    new Dictionary<string, int>();

// Build reverse lookup: group number -> name
var groupNames = new Dictionary<int, string>();
foreach (var kvp in namedGroups)
{
    groupNames[kvp.Value] = kvp.Key;
}

// Add capture group 0 (full match)
captures.Add(new CaptureGroup(
    matchStart,
    matchEnd - matchStart,
    0,
    groupNames.ContainsKey(0) ? groupNames[0] : null));

// Add numbered captures (1-9)
for (int i = 1; i < 10; i++)
{
    matcher.GetCapture(i, out int capStart, out int capEnd);
    if (capStart >= 0 && capEnd >= 0 && capEnd >= capStart)
    {
        string? name = groupNames.ContainsKey(i) ? groupNames[i] : null;
        captures.Add(new CaptureGroup(
            capStart,
            capEnd - capStart,
            i,
            name)); // Pass name!
    }
}

return new ColorerMatch(
    input,
    matchStart,
    matchEnd - matchStart,
    captures);
```

**Location**: Method `Match()`, lines 78-104
**Lines to Change**: ~15 lines

---

## Comprehensive Test Plan

### Total Tests: 35-42 tests

### Category 1: **Basic Named Group Syntax** (10 tests)

1. **SimpleNamedGroup_ParsesAndMatches**
   - Pattern: `(?{name}abc)`
   - Input: `"abc"`
   - Assert: Match succeeds, group "name" = "abc"

2. **MultipleNamedGroups_AllCaptured**
   - Pattern: `(?{first}\w+)-(?{second}\d+)`
   - Input: `"test-123"`
   - Assert: first="test", second="123"

3. **NamedGroupWithQuantifier_MatchesMultiple**
   - Pattern: `(?{word}\w+)+`
   - Input: `"hello"`
   - Assert: word="hello"

4. **NestedNamedGroups_OuterAndInner**
   - Pattern: `(?{outer}a(?{inner}b)c)`
   - Input: `"abc"`
   - Assert: outer="abc", inner="b"

5. **EmptyNameGroup_TreatedAsNonCapturing**
   - Pattern: `(?{}abc)`
   - Input: `"abc"`
   - Assert: Match succeeds, no named group

6. **NamedGroupDoesntMatch_FailedCapture**
   - Pattern: `(?{opt}a)?b`
   - Input: `"b"`
   - Assert: Match succeeds, group "opt" failed

7. **NamedGroupWithAlternation_CapturesMatched**
   - Pattern: `(?{choice}foo|bar)`
   - Input: `"bar"`
   - Assert: choice="bar"

8. **NamedGroupRepeated_CapturesLast**
   - Pattern: `(?{letter}a)+`
   - Input: `"aaa"`
   - Assert: letter="a" (last occurrence)

9. **NamedGroupAtStart_Index0**
   - Pattern: `(?{start}^abc)`
   - Input: `"abc"`
   - Assert: start="abc", Index=0

10. **NamedGroupAtEnd_CorrectPosition**
    - Pattern: `(?{end}xyz$)`
    - Input: `"xyz"`
    - Assert: end="xyz"

---

### Category 2: **Named Group Access API** (10 tests)

11. **GetGroup_ByName_ReturnsCorrectGroup**
    - Pattern: `(?{test}abc)`
    - Assert: GetGroup("test").Success == true

12. **GetGroup_NonExistentName_ReturnsFailedGroup**
    - Pattern: `(abc)`
    - Assert: GetGroup("missing").Success == false

13. **GetGroupValue_ByName_ReturnsText**
    - Pattern: `(?{num}\d+)`
    - Input: `"123"`
    - Assert: GetGroupValue("num") == "123"

14. **GetGroupValue_NonExistentName_ReturnsEmpty**
    - Pattern: `(abc)`
    - Assert: GetGroupValue("missing") == ""

15. **GetGroupSpan_ByName_ReturnsSpan**
    - Pattern: `(?{word}\w+)`
    - Input: `"hello"`
    - Assert: GetGroupSpan("word").ToString() == "hello"

16. **GetGroupSpan_NonExistentName_ReturnsEmpty**
    - Pattern: `(abc)`
    - Assert: GetGroupSpan("missing").IsEmpty == true

17. **TryGetGroupNumber_ValidName_ReturnsTrue**
    - Pattern: `(?{test}abc)`
    - Assert: TryGetGroupNumber("test", out n) == true, n == 1

18. **TryGetGroupNumber_InvalidName_ReturnsFalse**
    - Pattern: `(abc)`
    - Assert: TryGetGroupNumber("missing", out n) == false

19. **GetGroupNames_MultipleNamed_ReturnsAll**
    - Pattern: `(?{a}x)(?{b}y)(?{c}z)`
    - Assert: GetGroupNames() contains {"a", "b", "c"}

20. **GetGroupNames_NoNamedGroups_ReturnsEmpty**
    - Pattern: `(a)(b)(c)`
    - Assert: GetGroupNames().Count() == 0

---

### Category 3: **CaptureGroup Name Property** (8 tests)

21. **CaptureGroup_Name_PopulatedCorrectly**
    - Pattern: `(?{test}abc)`
    - Assert: Groups[1].Name == "test"

22. **CaptureGroup_Name_UnnamedIsNull**
    - Pattern: `(abc)`
    - Assert: Groups[1].Name == null

23. **CaptureGroup_ToString_ShowsName**
    - Pattern: `(?{test}abc)`
    - Assert: Groups[1].ToString() contains "test"

24. **CaptureGroup_ToString_UnnamedNoName**
    - Pattern: `(abc)`
    - Assert: Groups[1].ToString() doesn't contain name

25. **Groups_Collection_ContainsNamedGroups**
    - Pattern: `(?{a}x)(?{b}y)`
    - Assert: Groups[1].Name=="a", Groups[2].Name=="b"

26. **NamedGroup_FailedCapture_NameStillPresent**
    - Pattern: `(?{opt}a)?b`
    - Input: `"b"`
    - Assert: GetGroup("opt").Name == "opt", Success == false

27. **MixedNamedAndUnnamed_BothInGroups**
    - Pattern: `(?{named}a)(b)(?{other}c)`
    - Assert: Groups[1].Name="named", Groups[2].Name=null, Groups[3].Name="other"

28. **NamedGroup0_FullMatch_MayHaveName**
    - Pattern: `(?{full}abc)`
    - Input: `"abc"`
    - Test behavior: Group 0 typically unnamed

---

### Category 4: **Named Groups Edge Cases** (7 tests)

29. **DuplicateNames_Behavior**
    - Pattern: `(?{dup}a)|(?{dup}b)`
    - Input: `"b"`
    - Assert: Document behavior (last wins or error)

30. **VeryLongName_HandledCorrectly**
    - Pattern: `(?{very_long_name_with_underscores_123}abc)`
    - Assert: Name stored and retrievable

31. **NameWithNumbers_Valid**
    - Pattern: `(?{group1}a)(?{group2}b)`
    - Assert: Both names work

32. **NameWithUnderscores_Valid**
    - Pattern: `(?{my_group}abc)`
    - Assert: Name "my_group" works

33. **NonCapturingVsNamed_Different**
    - Pattern: `(?:a)(?{named}b)`
    - Assert: Only "named" in GetGroupNames()

34. **UnclosedNamedGroup_ThrowsException**
    - Pattern: `(?{unclosed`
    - Assert: Throws RegexSyntaxException

35. **InvalidNameChars_HandledOrError**
    - Pattern: `(?{name-with-dash}abc)` or `(?{name with space}abc)`
    - Assert: Either works or throws clear error

---

### Category 5: **Integration & Real-World** (5-7 tests)

36. **RealWorld_EmailPattern_NamedParts**
    - Pattern: `(?{user}\w+)@(?{domain}\w+)\.(?{tld}\w+)`
    - Input: `"user@example.com"`
    - Assert: user="user", domain="example", tld="com"

37. **RealWorld_DatePattern_NamedComponents**
    - Pattern: `(?{year}\d{4})-(?{month}\d{2})-(?{day}\d{2})`
    - Input: `"2025-11-13"`
    - Assert: year="2025", month="11", day="13"

38. **RealWorld_URLPattern_Protocol_Host_Path**
    - Pattern: `(?{proto}\w+)://(?{host}[\w.]+)/(?{path}[\w/]+)`
    - Input: `"https://example.com/path/to/file"`
    - Assert: All parts captured

39. **NamedGroupWithBackreference_WorksTogether**
    - Pattern: `(?{tag}\w+):\1`
    - Input: `"test:test"`
    - Assert: tag="test", backreference works

40. **NamedGroupInLookahead_Captured**
    - Pattern: `(?{word}\w+)(?=\d)`
    - Input: `"abc123"`
    - Assert: word="abc12" (backtracked for lookahead)

41. **ComplexNesting_NamedInAlternation**
    - Pattern: `((?{a}foo)|(?{b}bar))+`
    - Input: `"foobar"`
    - Assert: Named groups captured from alternation

42. **CaseInsensitive_NamedGroup_Works**
    - Pattern: `(?{word}[a-z]+)`
    - Options: IgnoreCase
    - Input: `"HELLO"`
    - Assert: word="HELLO"

---

## Implementation Summary

### Changes Required:
1. **CRegExpCompiler.cs**: Add 1 property (~1 line)
2. **ColorerRegex.cs**: Modify Match() method (~15 lines)

### Tests to Create:
- **File**: `NamedCaptureGroupTests.cs`
- **Test Count**: 35-42 tests
- **Estimated Coverage Gain**:
  - Named group features: 0% → ~95%
  - Overall compiler: +5%
  - Overall matcher: +3%

### What This Achieves:
- ✅ Complete Colorer `(?{name}...)` syntax support
- ✅ All Match API methods work with named groups
- ✅ CaptureGroup.Name property populated
- ✅ GetGroupNames(), TryGetGroupNumber() functional
- ✅ Real-world patterns with named parts
- ✅ Integration with backreferences, lookahead

### What This Does NOT Include:
- ❌ PCRE2 standard syntax `(?<name>...)` or `(?P<name>...)`
- ❌ Named backreferences `\k<name>`
- ❌ Other advanced PCRE2 features

This keeps us focused on **completing the Colorer implementation** without scope creep into full PCRE2 compatibility.

---

## Current Test Suite Status (Before This Work)

- **Total Regex Tests**: 387 tests (all passing ✅)
- **Recent Additions**:
  - CharacterClass tests: +89 tests (48% → 95% coverage)
  - Match API tests: +66 tests (34% → 80% coverage)
  - Backtracking tests: +47 tests (64% → 75% coverage)

## Expected After Named Groups Implementation

- **Total Regex Tests**: ~425 tests
- **Named Groups Coverage**: 0% → ~95%
- **All tests passing**: ✅

---

## Next Steps (Tomorrow)

1. ✅ Read this plan
2. Fix CRegExpCompiler.cs (add NamedGroups property)
3. Fix ColorerRegex.cs (populate CaptureGroup names)
4. Create NamedCaptureGroupTests.cs with ~40 tests
5. Run tests and verify all pass
6. Update CODE_COVERAGE_REPORT.md with results

**Estimated Time**: 1-2 hours
**Risk**: Low (minimal code changes, well-defined scope)
