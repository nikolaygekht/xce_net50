# Regex Engine Cleanup Summary

## Date: 2025-11-12

## Purpose
Removed old tree-based regex implementation to prepare for new C++ direct-port implementation with linked list architecture and pointer-based matching.

---

## Files Removed

### 1. Old Entry Point
- ❌ **ColorerRegex.cs** - Old main Regex class (replaced by new `Regex` class in plan)

### 2. Old Compiler/Matcher
- ❌ **Internal/RegexCompiler.cs** - Tree-based compiler (replaced by `CRegExpCompiler`)
- ❌ **Internal/RegexMatcher.cs** - State machine matcher (replaced by `CRegExpMatcher`)

### 3. Old Node Architecture
- ❌ **Nodes/IRegexNode.cs** - Node interface (incompatible with new linked list)
- ❌ **Nodes/EmptyNode.cs** - Empty node
- ❌ **Nodes/GroupNode.cs** - Group node
- ❌ **Nodes/LiteralNode.cs** - Literal node
- ❌ **Nodes/MetacharacterNode.cs** - Metacharacter node
- ❌ **Nodes/QuantifierNode.cs** - Quantifier node
- ❌ **Nodes/SequenceNode.cs** - Sequence node

**Why removed**: Old architecture used tree structure with parent/children. New implementation uses C++ linked list structure with `next`, `prev`, `parent` pointers directly in `SRegInfo` struct.

### 4. Old Enums
- ❌ **Enums/RegexOperator.cs** - Old operator enum (replaced by `EOps`)
- ❌ **Enums/MetaSymbol.cs** - Old meta symbol enum (replaced by `EMetaSymbols`)

**Why removed**: New enums match C++ exactly for 100% compatibility.

---

## Files Kept (Public API)

### 1. Exception Classes
- ✅ **ColorerException.cs** - Base exception and all derived types
  - `ColorerException` - Base class
  - `RegexSyntaxException` - Pattern syntax errors
  - `BackreferenceException` - Backreference errors

**Why kept**: Used for error handling in new implementation.

### 2. Match Result Types
- ✅ **CaptureGroup.cs** - Represents a captured group
  - `Index`, `Length`, `Success`, `GroupNumber`, `Name` properties
  - Zero-allocation struct design

- ✅ **ColorerMatch.cs** - Match result container
  - Groups, named groups, effective start/end
  - Span-based access methods
  - **Added**: New constructor accepting `List<CaptureGroup>` for convenience

**Why kept**: Public API for match results. New implementation will populate these.

### 3. Options
- ✅ **Enums/RegexOptions.cs** - Regex compilation/matching options
  - `IgnoreCase`, `Multiline`, `Singleline`, `Extended`, `PositionMoves`

**Why kept**: Used by new implementation's constructor and matcher.

---

## Directory Structure After Cleanup

```
net/Far.Colorer/RegularExpressions/
├── CaptureGroup.cs              ✅ KEPT
├── ColorerException.cs          ✅ KEPT
├── ColorerMatch.cs              ✅ KEPT (updated with new constructor)
├── Enums/
│   └── RegexOptions.cs          ✅ KEPT
└── Internal/                     [TO BE POPULATED]
    ├── SRegInfo.cs              🔜 NEW (Phase 1)
    ├── StackElem.cs             🔜 NEW (Phase 1)
    ├── EOps.cs                  🔜 NEW (Phase 1)
    ├── ReAction.cs              🔜 NEW (Phase 1)
    ├── EMetaSymbols.cs          🔜 NEW (Phase 1)
    ├── CharacterClass.cs        🔜 NEW (Phase 2)
    ├── CRegExpCompiler.cs       🔜 NEW (Phase 2)
    └── CRegExpMatcher.cs        🔜 NEW (Phase 3)
```

---

## Architecture Differences

### Old Implementation (Removed)
- **Tree structure**: Parent/children relationships
- **Node classes**: Separate class per operator type
- **Managed memory**: GC-tracked objects
- **High-level**: .NET idiomatic approach
- **Partial compatibility**: Only basic regex features

### New Implementation (To Be Added)
- **Linked list structure**: next/prev/parent pointers in single struct
- **Union-style struct**: Single `SRegInfo` struct with union-like fields
- **Unmanaged memory**: Manual allocation with `Marshal.AllocHGlobal`
- **Low-level**: Direct C++ port with `unsafe` code
- **Full compatibility**: 100% match with C++ including Colorer extensions

---

## Key Changes to Existing Files

### ColorerMatch.cs
**Added**: New constructor overload
```csharp
internal ColorerMatch(
    string input,
    int index,
    int length,
    List<CaptureGroup> groups)
```

**Why**: Simplifies usage from new matcher implementation. Automatically builds named groups dictionary from list.

---

## Test Impact

### Existing Tests
- Tests in `Far.Colorer.Tests/RegularExpressions/` will need updates
- Most test cases remain valid (testing behavior, not implementation)
- Test patterns stay the same

### What Will Break
- Any tests directly instantiating old classes:
  - `ColorerRegex` → `Regex`
  - Node classes → No longer accessible
  - `RegexCompiler`/`RegexMatcher` → Internal now

### What Will Work
- Tests using public API:
  - `new Regex(pattern).IsMatch(input)` → ✅ Same signature
  - `regex.Match(input)` → ✅ Same signature
  - `match.Groups`, `match.Value` → ✅ Same API

---

## Migration Path for Users

### Before (Old API)
```csharp
using Far.Colorer.RegularExpressions;

var regex = new ColorerRegex("a+b", RegexOptions.IgnoreCase);
var match = regex.Match("AAB");
if (match.Success) {
    Console.WriteLine(match.Value);
}
```

### After (New API)
```csharp
using Far.Colorer.RegularExpressions;

var regex = new Regex("a+b", RegexOptions.IgnoreCase);
var match = regex.Match("AAB");
if (match.Success) {
    Console.WriteLine(match.Value);
}
```

**Only change**: `ColorerRegex` → `Regex`. Everything else identical.

---

## Next Steps

1. ✅ **Cleanup complete**
2. 🔜 **Implement Phase 1** - Data structures (4 hours)
3. 🔜 **Implement Phase 2** - Compiler (8 hours)
4. 🔜 **Implement Phase 3** - Matcher (10 hours)
5. 🔜 **Implement Phase 4** - Integration (4 hours)
6. 🔜 **Run tests** - Fix issues (6-8 hours)

**Total estimated**: 3-4 days

---

## References

- **Implementation Plan**: See `REGEX_REWRITE_PLAN_V2.md`
- **Supplemental Details**: See `REGEX_REWRITE_SUPPLEMENT.md`
- **Project Overview**: See `CLAUDE.md`

---

## Validation

To verify cleanup was successful:

```bash
# Should show only 4 files
find net/Far.Colorer/RegularExpressions -name "*.cs" | wc -l
# Expected: 4

# Should compile without errors
dotnet build net/Far.Colorer/Far.Colorer.csproj
# Expected: Build succeeded (with potential warnings about unused code)
```

---

## Rollback Instructions

If needed to restore old implementation:

```bash
git checkout HEAD -- net/Far.Colorer/RegularExpressions/
```

**Note**: Don't rollback unless absolutely necessary. The old implementation had the hanging bug that motivated this rewrite.
