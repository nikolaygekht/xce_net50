# Backreference to Named Groups - Refactoring Plan

**Date:** 2025-11-14
**Status:** Planning Phase
**Complexity:** Medium-High
**Estimated Effort:** 4-6 hours
**Risk Level:** Medium (requires careful matching logic changes)

---

## Problem Statement

Currently, backreferences (`\1`, `\2`, etc.) do **NOT** work with named capture groups (`(?{name}...)`).

### Current Behavior

```csharp
// This works ✅
var regex = new ColorerRegex(@"(\w+):\1");
var match = regex.Match("test:test");  // Matches!

// This fails ❌
var regex = new ColorerRegex(@"(?{tag}\w+):\1");
var match = regex.Match("test:test");  // Returns NULL!
```

### Root Cause

Named groups and regular groups use **separate storage arrays** in the matcher:
- **Regular brackets** (`(...)`) → stored in `matches->s[]` and `matches->e[]`
- **Named brackets** (`(?{name}...)`) → stored in `matches->ns[]` and `matches->ne[]`

When the backreference matcher tries to match `\1`, it only checks the regular storage arrays (`s/e`), so it can't find the content captured by a named group.

---

## Architecture Analysis

### Current Storage Structure

**File:** `net/Far.Colorer/RegularExpressions/Internal/SMatches.cs`

```csharp
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SMatches
{
    public fixed int s[10];   // Regular group starts
    public fixed int e[10];   // Regular group ends
    public fixed int ns[10];  // Named group starts
    public fixed int ne[10];  // Named group ends
}
```

### Current Matcher Logic

**File:** `net/Far.Colorer/RegularExpressions/Internal/CRegExpMatcher.cs`

**Capture storage (lines 215-253):**
```csharp
case EOps.ReBrackets:
case EOps.ReNamedBrackets:
    if (leftenter) {
        re->s = toParse;
        re = re->param;
        continue;
    }
    if (re->param0 == -1)
        break;

    if (re->op == EOps.ReBrackets) {
        // Store in s/e arrays
        int idx = re->param0;
        if (idx >= 0 && idx < 10) {
            int* sArr = matches->s;
            int* eArr = matches->e;
            sArr[idx] = re->s;
            eArr[idx] = toParse;
        }
    }
    else { // ReNamedBrackets
        // Store in ns/ne arrays
        int idx = re->param0;
        if (idx >= 0 && idx < 10) {
            int* nsArr = matches->ns;
            int* neArr = matches->ne;
            nsArr[idx] = re->s;
            neArr[idx] = toParse;
        }
    }
    break;
```

**Backreference matching (lines 604-634):**
```csharp
case EOps.ReBkTrace:
    int idx = re->param0;
    if (idx >= 0 && idx < 10)
    {
        int* sArr = matches->s;  // ❌ ONLY checks regular arrays!
        int* eArr = matches->e;

        int start = sArr[idx];
        int end = eArr[idx];

        if (start < 0 || end < 0)
        {
            CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
            continue;
        }

        // Match the backreferenced content
        int len = end - start;
        if (toParse + len > this.end || !MatchBackreference(start, end, toParse))
        {
            CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
            continue;
        }
        toParse += len;
    }
    break;
```

---

## Solution Approaches

### Option 1: Unified Storage (RECOMMENDED)

**Strategy:** Store all captures (named and regular) in the same arrays.

#### Advantages ✅
- Backreferences work automatically
- Simpler logic - no need to check two storage locations
- Better performance (fewer array lookups)
- Easier to maintain
- Matches standard regex behavior

#### Disadvantages ❌
- Requires updating all code that accesses `ns/ne` arrays
- May break compatibility if external code depends on separate arrays
- Need to migrate existing logic

#### Implementation Steps

1. **Phase 1: Modify SMatches Structure**
   - Keep `ns/ne` arrays for backward compatibility (deprecated)
   - Add deprecation warnings
   - Update documentation

2. **Phase 2: Update Matcher Storage Logic**
   - Change `ReNamedBrackets` case to store in `s/e` arrays (same as `ReBrackets`)
   - Keep group number assignments identical
   - Test thoroughly

3. **Phase 3: Update Retrieval Methods**
   - `GetNamedCapture()` should read from `s/e` arrays
   - Mark as deprecated, point to `GetCapture()`
   - Update `ColorerRegex.Match()` to use unified retrieval

4. **Phase 4: Cleanup (optional)**
   - Remove `ns/ne` arrays after deprecation period
   - Remove `GetNamedCapture()` method
   - Update all tests

---

### Option 2: Smart Backreference Lookup

**Strategy:** Check both storage arrays when resolving backreferences.

#### Advantages ✅
- Minimal code changes
- No breaking changes to storage structure
- Maintains separation between named/regular groups
- Lower risk

#### Disadvantages ❌
- Duplicates lookup logic
- Slightly slower (checks two locations)
- More complex - need to track which storage has the data
- Harder to maintain

#### Implementation Steps

1. **Phase 1: Add Group Type Tracking**
   ```csharp
   // Add to SMatches or maintain separate tracking
   public fixed bool isNamed[10];  // Track which groups are named
   ```

2. **Phase 2: Update Capture Storage**
   ```csharp
   case EOps.ReNamedBrackets:
       int idx = re->param0;
       if (idx >= 0 && idx < 10) {
           int* nsArr = matches->ns;
           int* neArr = matches->ne;
           nsArr[idx] = re->s;
           neArr[idx] = toParse;
           matches->isNamed[idx] = true;  // Mark as named
       }
       break;
   ```

3. **Phase 3: Update Backreference Matching**
   ```csharp
   case EOps.ReBkTrace:
       int idx = re->param0;
       if (idx >= 0 && idx < 10) {
           int start, end;

           // Check if this group is named
           if (matches->isNamed[idx]) {
               int* nsArr = matches->ns;
               int* neArr = matches->ne;
               start = nsArr[idx];
               end = neArr[idx];
           } else {
               int* sArr = matches->s;
               int* eArr = matches->e;
               start = sArr[idx];
               end = eArr[idx];
           }

           if (start < 0 || end < 0) {
               CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
               continue;
           }

           int len = end - start;
           if (toParse + len > this.end || !MatchBackreference(start, end, toParse)) {
               CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
               continue;
           }
           toParse += len;
       }
       break;
   ```

4. **Phase 4: Update GetCapture Methods**
   ```csharp
   public void GetCapture(int index, out int start, out int end)
   {
       if (index < 0 || index >= 10) {
           start = -1;
           end = -1;
           return;
       }

       lock (_matchLock) {
           // Check both storage locations
           if (matches->isNamed[index]) {
               start = matches->ns[index];
               end = matches->ne[index];
           } else {
               start = matches->s[index];
               end = matches->e[index];
           }
       }
   }
   ```

---

### Option 3: Copy Named Captures to Regular Arrays

**Strategy:** Duplicate named group captures into both storage arrays.

#### Advantages ✅
- Minimal changes to backreference logic
- Backward compatible
- Easy to implement

#### Disadvantages ❌
- Memory duplication (wastes 80 bytes per match)
- Inconsistent state if only one array is updated
- Doesn't feel like a proper solution
- Still have two storage mechanisms

#### Implementation (Brief)

Simply update the `ReNamedBrackets` storage case to write to both arrays:

```csharp
case EOps.ReNamedBrackets:
    int idx = re->param0;
    if (idx >= 0 && idx < 10) {
        // Store in BOTH arrays
        int* sArr = matches->s;
        int* eArr = matches->e;
        int* nsArr = matches->ns;
        int* neArr = matches->ne;

        sArr[idx] = re->s;
        eArr[idx] = toParse;
        nsArr[idx] = re->s;
        neArr[idx] = toParse;
    }
    break;
```

---

## Recommended Approach: Option 1 (Unified Storage)

### Detailed Implementation Plan

#### Step 1: Investigate C++ Original Behavior

**Action:** Check if the original C++ Colorer implementation has separate storage.

**Files to check:**
- `native/src/colorer/cregexp/cregexp.h`
- `native/src/colorer/cregexp/cregexp.cpp`

**Search for:** `#ifndef NAMED_MATCHES_IN_HASH` directive

**Goal:** Understand if separate storage was a deliberate design choice or an implementation artifact.

**Estimated time:** 30 minutes

---

#### Step 2: Create Feature Branch and Backup

```bash
git checkout -b feature/backreference-named-groups
git add -A
git commit -m "Backup: Before backreference refactoring"
```

**Estimated time:** 5 minutes

---

#### Step 3: Add Comprehensive Backreference Tests (TDD Approach)

**File:** Create `net/Far.Colorer.Tests/RegularExpressions/BackreferenceNamedGroupTests.cs`

**Test cases:**

```csharp
public class BackreferenceNamedGroupTests
{
    [Fact]
    public void NamedGroup_NumericBackreference_Matches()
    {
        var regex = new ColorerRegex(@"(?{tag}\w+):\1");
        var match = regex.Match("test:test");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("test:test", match.Value);
        Assert.Equal("test", match.GetGroupValue("tag"));
    }

    [Fact]
    public void NamedGroup_NumericBackreference_NoMatch()
    {
        var regex = new ColorerRegex(@"(?{tag}\w+):\1");
        var match = regex.Match("test:other");

        Assert.Null(match);
    }

    [Fact]
    public void MultipleNamedGroups_Backreference_Correct()
    {
        var regex = new ColorerRegex(@"(?{a}\w+)-(?{b}\w+):\1:\2");
        var match = regex.Match("foo-bar:foo:bar");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("foo", match.GetGroupValue("a"));
        Assert.Equal("bar", match.GetGroupValue("b"));
    }

    [Fact]
    public void MixedNamedAndRegular_Backreferences_Work()
    {
        var regex = new ColorerRegex(@"(?{named}\w+)(\d+):\1:\2");
        var match = regex.Match("test123:test:123");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("test", match.GetGroupValue("named"));
        Assert.Equal("123", match.GetGroupValue(2));
    }

    [Fact]
    public void NamedGroup_CaseInsensitive_Backreference()
    {
        var regex = new ColorerRegex(@"(?{word}\w+):\1", RegexOptions.IgnoreCase);
        var match = regex.Match("TEST:test");

        Assert.NotNull(match);
        Assert.True(match.Success);
    }

    [Fact]
    public void NamedGroup_EmptyBackreference_Matches()
    {
        var regex = new ColorerRegex(@"(?{opt}a?):\1");
        var match = regex.Match(":");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal(":", match.Value);
    }

    [Fact]
    public void NamedGroup_RepeatedBackreference()
    {
        var regex = new ColorerRegex(@"(?{tag}\w+):\1:\1");
        var match = regex.Match("foo:foo:foo");

        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("foo", match.GetGroupValue("tag"));
    }
}
```

**Verify all tests fail initially:**
```bash
dotnet test --filter "FullyQualifiedName~BackreferenceNamedGroupTests"
```

**Expected:** 7 failures

**Estimated time:** 45 minutes

---

#### Step 4: Modify Matcher Storage Logic

**File:** `net/Far.Colorer/RegularExpressions/Internal/CRegExpMatcher.cs`

**Location:** Lines 241-253

**Change:**
```csharp
else // ReNamedBrackets
{
    int idx = re->param0;
    if (idx >= 0 && idx < 10)
    {
        // CHANGE: Store in regular arrays (same as ReBrackets)
        int* sArr = matches->s;
        int* eArr = matches->e;
        sArr[idx] = re->s;
        eArr[idx] = toParse;
        if (eArr[idx] < sArr[idx])
            sArr[idx] = eArr[idx];

        // Also update ns/ne for backward compatibility (deprecated)
        int* nsArr = matches->ns;
        int* neArr = matches->ne;
        nsArr[idx] = re->s;
        neArr[idx] = toParse;
    }
}
```

**Estimated time:** 15 minutes

---

#### Step 5: Run All Tests

```bash
dotnet test net/Far.Colorer.Tests/Far.Colorer.Tests.csproj
```

**Expected results:**
- All previous 428 tests still pass ✅
- 7 new backreference tests pass ✅
- Total: 435 tests passing

**If tests fail:** Investigate and fix issues

**Estimated time:** 30 minutes (including debugging if needed)

---

#### Step 6: Update ColorerRegex.Match() Capture Extraction

**File:** `net/Far.Colorer/RegularExpressions/Internal/ColorerRegex.cs`

**Location:** Lines 99-126

**Current code:**
```csharp
// Add numbered captures (1-9)
for (int i = 1; i < 10; i++)
{
    // Check if this is a named group
    bool isNamedGroup = groupNames.ContainsKey(i);
    int capStart, capEnd;

    if (isNamedGroup)
    {
        // Use GetNamedCapture for named groups
        matcher.GetNamedCapture(i, out capStart, out capEnd);
    }
    else
    {
        // Use regular GetCapture for regular groups
        matcher.GetCapture(i, out capStart, out capEnd);
    }

    // ... rest of code
}
```

**Simplified code:**
```csharp
// Add numbered captures (1-9)
for (int i = 1; i < 10; i++)
{
    // ALL groups now use GetCapture (unified storage)
    matcher.GetCapture(i, out int capStart, out int capEnd);

    if (capStart >= 0 && capEnd >= 0 && capEnd >= capStart)
    {
        // Check if this group has a name
        string? name = groupNames.ContainsKey(i) ? groupNames[i] : null;
        captures.Add(new CaptureGroup(
            capStart,
            capEnd - capStart,
            i,
            name));
    }
}
```

**Estimated time:** 10 minutes

---

#### Step 7: Run Full Test Suite Again

```bash
dotnet test net/Far.Colorer.Tests/Far.Colorer.Tests.csproj
```

**Expected:** All 435 tests pass

**Estimated time:** 5 minutes

---

#### Step 8: Mark GetNamedCapture() as Deprecated

**File:** `net/Far.Colorer/RegularExpressions/Internal/CRegExpMatcher.cs`

**Location:** Lines 1050-1063

```csharp
/// <summary>
/// Get named capture group result.
/// DEPRECATED: Named groups now use same storage as regular groups.
/// Use GetCapture() instead.
/// Thread-safe: Reads named capture results that were stored by Parse().
/// </summary>
[Obsolete("Named groups now use unified storage. Use GetCapture() instead.")]
public void GetNamedCapture(int index, out int start, out int end)
{
    // For backward compatibility, delegate to GetCapture
    GetCapture(index, out start, out end);
}
```

**Estimated time:** 5 minutes

---

#### Step 9: Update Documentation

**Files to update:**

1. **README or API docs:**
   - Document that backreferences now work with named groups
   - Update examples

2. **CODE_COVERAGE_REPORT.md:**
   - Remove "backreferences to named groups" from known limitations
   - Add to completed features

3. **NAMED_GROUPS_IMPLEMENTATION_PLAN.md:**
   - Add completion notes
   - Mark backreference feature as implemented

**Estimated time:** 20 minutes

---

#### Step 10: Update Failing Test (Currently Skipped)

**File:** `net/Far.Colorer.Tests/RegularExpressions/NamedCaptureGroupTests.cs`

**Location:** Line 484

**Change:**
```csharp
// REMOVE Skip attribute
[Fact] // Was: [Fact(Skip = "...")]
public void NamedGroupWithBackreference_WorksTogether()
{
    var regex = new ColorerRegex(@"(?{tag}\w+):\1");
    var match = regex.Match("test:test");

    Assert.NotNull(match);
    Assert.True(match.Success);
    Assert.Equal("test", match.GetGroupValue("tag"));
}
```

**Estimated time:** 5 minutes

---

#### Step 11: Final Test Run

```bash
dotnet test net/Far.Colorer.Tests/Far.Colorer.Tests.csproj
```

**Expected results:**
- **436 tests passing** (435 + 1 previously skipped)
- **0 failures**
- **0 skipped**

**Estimated time:** 5 minutes

---

#### Step 12: Commit and Document

```bash
git add -A
git commit -m "feat: Add backreference support for named capture groups

- Unified storage for named and regular groups
- All captures now stored in s/e arrays
- Backreferences (\1, \2, etc.) now work with (?{name}...) groups
- Added 7 new comprehensive backreference tests
- Deprecated GetNamedCapture() method
- All 436 tests passing

Breaking changes: None (backward compatible)
Closes: Named groups backreference limitation"
```

**Estimated time:** 10 minutes

---

## Total Estimated Time

| Phase | Time |
|-------|------|
| Investigation (C++ code) | 30 min |
| Setup & branching | 5 min |
| Write tests (TDD) | 45 min |
| Modify matcher storage | 15 min |
| Test & debug | 30 min |
| Update ColorerRegex | 10 min |
| Re-test | 5 min |
| Deprecation markers | 5 min |
| Documentation | 20 min |
| Update skipped test | 5 min |
| Final testing | 5 min |
| Commit & cleanup | 10 min |
| **TOTAL** | **3 hours** |

**Buffer for unexpected issues:** +1-2 hours
**Total with buffer:** **4-5 hours**

---

## Risk Assessment

### Low Risk ✅
- Well-understood problem
- Clear solution path
- Comprehensive test coverage
- Backward compatible changes

### Medium Risk ⚠️
- Touching core matching logic
- Need to verify C++ original behavior
- May expose edge cases in existing code

### Mitigation Strategies
1. **Feature branch** - easy to revert if needed
2. **TDD approach** - tests written first
3. **Incremental changes** - small, testable steps
4. **Backward compatibility** - keep deprecated methods
5. **Comprehensive testing** - run full suite after each change

---

## Success Criteria

✅ All 436+ tests passing
✅ No skipped tests
✅ Backreferences work with named groups
✅ No performance regression
✅ Backward compatible (deprecated methods still work)
✅ Documentation updated
✅ Code coverage maintained or improved

---

## Future Cleanup (Optional - Phase 2)

After deprecation period (e.g., 6 months):

1. Remove `ns/ne` arrays from `SMatches`
2. Remove `GetNamedCapture()` method
3. Remove backward compatibility code
4. Update all comments/docs
5. Reduce memory footprint

**Estimated effort:** 1-2 hours
**Benefit:** Simpler codebase, less memory usage

---

## Alternative: Quick Prototype (Option 2)

If you want to **verify the approach works** before full implementation:

1. Just modify the `ReNamedBrackets` storage case (15 min)
2. Run the skipped test to see if it passes (5 min)
3. If it works, proceed with full plan
4. If not, investigate further

**Total prototype time:** 20 minutes

---

## Questions to Resolve Before Starting

1. ❓ Does the original C++ Colorer have separate storage by design or accident?
2. ❓ Are there external dependencies on `GetNamedCapture()` method?
3. ❓ Is there a specific reason named groups were stored separately?
4. ❓ What is the performance impact of checking group names vs. using separate arrays?

**Recommendation:** Investigate questions 1-3 before implementation (30 min research).

---

## Conclusion

**Recommended approach:** Option 1 (Unified Storage)

**Why:**
- Clean, maintainable solution
- Matches standard regex semantics
- Low risk with proper testing
- Backward compatible
- Fixes the root cause, not just symptoms

**Next step:** Review this plan, answer open questions, then proceed with implementation.
