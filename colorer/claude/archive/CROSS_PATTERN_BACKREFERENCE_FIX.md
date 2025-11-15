# Cross-Pattern Named Backreference Fix

## Problem

Cross-pattern named backreferences (`\y{name}`, `\Y{name}`) are broken:
1. **Compiler** validates group name exists in current pattern (line 388) - WRONG, should be resolved at match time
2. **Matcher** uses deprecated `ns/ne` arrays (lines 385-386, 409-410) instead of unified `s/e` storage
3. **SetBackReference** doesn't pass named group mapping to matcher

## HRC Usage

From `python.hrc`:
```xml
<block start="/(u?(?{Delim}&apos;{3}|&quot;{3}))/i" end="/(\y{Delim})/"
```

The `start` regex defines `(?{Delim}...)` and `end` regex references `\y{Delim}` - these are TWO SEPARATE regex compilations.

## Solution

### 1. Compiler Changes (CRegExpCompiler.cs line 373-412)

**Current (BUGGY):**
```csharp
string name = pattern.Substring(start, position - start);
if (!namedGroups.TryGetValue(name, out int groupNum))  // ← WRONG!
    throw new RegexSyntaxException($"Unknown group name: {name}");
node->param0 = groupNum;
```

**Fixed:**
```csharp
string name = pattern.Substring(start, position - start);
// Store name string - resolve at match time from backTrace
node->word = (void*)Marshal.StringToHGlobalUni(name);
node->param0 = -1; // Signal: resolve name at match time
```

### 2. Matcher Changes (CRegExpMatcher.cs lines 377-438)

**Current (BUGGY):**
```csharp
case EOps.ReBkTraceName:
    sv = re->param0;  // Group number
    int* bkNSArr = backTrace->ns;  // ← WRONG! Deprecated storage
    int* bkNEArr = backTrace->ne;
```

**Fixed:**
```csharp
case EOps.ReBkTraceName:
    // If param0 == -1, resolve name from word pointer
    if (re->param0 == -1)
    {
        string groupName = Marshal.PtrToStringUni((IntPtr)re->word)!;
        // Look up name in backTraceNamedGroups dictionary
        if (!backTraceNamedGroups.TryGetValue(groupName, out sv))
        {
            CheckStack(false, ...);
            continue;
        }
    }
    else
    {
        sv = re->param0;
    }

    // Use unified s/e arrays, not ns/ne
    int* bkSArr = backTrace->s;
    int* bkEArr = backTrace->e;
```

### 3. SetBackTrace API Enhancement

Need to pass named group dictionary from backMatch:

```csharp
public void SetBackTrace(string? str, SMatches* trace, Dictionary<string, int>? namedGroups = null)
{
    backStr = str;
    backTrace = trace;
    backTraceNamedGroups = namedGroups;  // New field
}
```

### 4. ColorerRegex.SetBackReference Enhancement

```csharp
public void SetBackReference(string? backStr, ColorerMatch? backMatch)
{
    if (backStr == null || backMatch == null)
    {
        matcher.SetBackTrace(null, null, null);
        return;
    }

    // Build named group dictionary from backMatch
    var namedGroups = new Dictionary<string, int>();
    foreach (var group in backMatch.Groups)
    {
        if (group.Name != null)
            namedGroups[group.Name] = group.Number;
    }

    // ... fill SMatches ...

    matcher.SetBackTrace(backStr, backTrace, namedGroups);
}
```

## Implementation Plan

1. ✅ Fix compiler to not validate cross-pattern names
2. ⏳ Add backTraceNamedGroups field to matcher
3. ⏳ Update SetBackTrace signature
4. ⏳ Fix matcher to resolve names and use s/e arrays
5. ⏳ Update SetBackReference to pass named groups
6. ⏳ Add memory cleanup for name strings

## Test Status

- 7 of 17 tests passing
- 10 tests fail due to these bugs

After fix, all should pass and we'll have coverage for lines 183-184, 329-439.
