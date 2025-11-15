# C++ Regex Backtracking Implementation Analysis

## Overview

Analyzed the C++ implementation in `native/src/colorer/cregexp/cregexp.cpp` to understand how it handles greedy quantifier backtracking.

## Key Findings

### 1. Quantifier Simplification

All basic quantifiers are converted to range operators during compilation (lines 370-416):

```cpp
// Greedy quantifiers
if (expr[i] == '*') {
    next->op = EOps::ReRangeN;  // {0,}
    next->s = 0;
}
if (expr[i] == '+') {
    next->op = EOps::ReRangeN;  // {1,}
    next->s = 1;
}
if (expr[i] == '?') {
    next->op = EOps::ReRangeNM; // {0,1}
    next->s = 0;
    next->e = 1;
}

// Non-greedy quantifiers
if (expr[i] == '*' && expr[i + 1] == '?') {
    next->op = EOps::ReNGRangeN; // {0,}?
    next->s = 0;
}
// ... similar for +?, ??
```

**Impact**: Only need to implement backtracking for `ReRangeN` and `ReRangeNM`, not for each quantifier type.

### 2. Backtracking Stack System

The C++ code uses an explicit backtracking stack with two key functions:

#### `insert_stack()` (lines 808-842)
Pushes current state onto stack and creates a **choice point**:

```cpp
void CRegExp::insert_stack(
    SRegInfo** re, SRegInfo** prev, int* toParse, bool* leftenter,
    int ifTrueReturn,    // What to do if sub-match succeeds
    int ifFalseReturn,   // What to do if sub-match fails
    SRegInfo** re2,      // Where to go next
    SRegInfo** prev2,
    int toParse2         // Position to try
)
{
    // Push current state
    StackElem& ne = RegExpStack[count_elem++];
    ne.re = *re;
    ne.prev = *prev;
    ne.toParse = *toParse;
    ne.ifTrueReturn = ifTrueReturn;
    ne.ifFalseReturn = ifFalseReturn;
    ne.leftenter = *leftenter;

    // Jump to new position
    *re = *re2;
    *prev = *prev2;
    *toParse = toParse2;
    *leftenter = true;
}
```

#### `check_stack(bool res)` (lines 788-806)
Pops stack and decides which path to take:

```cpp
void CRegExp::check_stack(
    bool res,  // Did the previous operation succeed?
    SRegInfo** re, SRegInfo** prev, int* toParse, bool* leftenter, int* action
)
{
    if (count_elem == 0) {
        *action = res;  // No more backtracking - return result
        return;
    }

    // Pop stack
    StackElem& ne = RegExpStack[--count_elem];

    // Choose action based on success/failure
    if (res) {
        *action = ne.ifTrueReturn;
    } else {
        *action = ne.ifFalseReturn;
    }

    // Restore state
    *re = ne.re;
    *prev = ne.prev;
    *toParse = ne.toParse;
    *leftenter = ne.leftenter;
}
```

#### Action Constants
```cpp
enum {
    rea_False = -2,        // Match failed, backtrack
    rea_True = -3,         // Match succeeded, continue
    rea_Break = -1,        // Exit inner loop
    rea_RangeN_step2,      // Special: range quantifier second step
    rea_RangeNM_step2,     // Special: range quantifier second step
    rea_NGRangeNM_step2    // Special: non-greedy range second step
};
```

### 3. Greedy Quantifier Implementation

#### `ReRangeN` - Greedy {n,} (lines 1183-1204)

```cpp
case EOps::ReRangeN:
    if (leftenter) {
        re->param0 = re->s;      // Counter = minimum required
        re->oldParse = -1;
    }

    if (!re->param0 && re->oldParse == toParse)
        break;  // Prevent infinite loop (matched 0 chars)

    re->oldParse = toParse;

    if (!re->param0) {
        // Already matched minimum - create choice point
        insert_stack(&re, &prev, &toParse, &leftenter,
                    rea_True,          // If match: accept and continue
                    rea_RangeN_step2,  // If fail: try one less match
                    &re->un.param,     // Try to match target
                    nullptr,
                    toParse);
        continue;
    } else {
        // Still matching minimum
        re->param0--;
    }

    re = re->un.param;  // Match target
    leftenter = true;
    continue;
```

**How it works**:
1. First pass: Match minimum required times (decrement `param0`)
2. After minimum: For each additional match, create a choice point
3. Choice point says: "If rest of pattern fails, come back and try without this match"
4. This implements greedy backtracking!

#### `ReRangeNM` - Greedy {n,m} (lines 1205-1228)

Similar but with maximum limit:

```cpp
case EOps::ReRangeNM:
    if (leftenter) {
        re->param0 = re->s;          // Counter = minimum
        re->param1 = re->e - re->s;  // Remaining optional matches
        re->oldParse = -1;
    }

    if (!re->param0) {
        if (re->param1)
            re->param1--;  // One less optional match available
        else {
            // Reached maximum - must succeed or fail
            insert_stack(&re, &prev, &toParse, &leftenter,
                        rea_True, rea_False,
                        &re->next, &re, toParse);
            continue;
        }

        // Create choice point
        insert_stack(&re, &prev, &toParse, &leftenter,
                    rea_True,
                    rea_RangeNM_step2,
                    &re->un.param,
                    nullptr,
                    toParse);
        continue;
    } else {
        re->param0--;  // Still matching minimum
    }

    re = re->un.param;
    leftenter = true;
    continue;
```

#### Action Handlers (lines 1280-1330)

```cpp
switch (action) {
    case rea_False:
        if (count_elem) {
            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
            continue;
        }
        return false;

    case rea_True:
        if (count_elem) {
            check_stack(true, &re, &prev, &toParse, &leftenter, &action);
            continue;
        }
        return true;

    case rea_RangeN_step2:
        action = -1;
        // Push another choice: continue matching OR give up
        insert_stack(&re, &prev, &toParse, &leftenter,
                    rea_True, rea_False,
                    &re->next, &re, toParse);
        continue;

    case rea_RangeNM_step2:
        action = -1;
        // Similar to RangeN_step2
        insert_stack(&re, &prev, &toParse, &leftenter,
                    rea_True, rea_False,
                    &re->next, &re, toParse);
        continue;
}
```

### 4. Non-Greedy Quantifiers (lines 1229-1268)

Non-greedy quantifiers work similarly but create choice points in reverse:
- Try matching minimum first (no choice point)
- Create choice point to try one more match (reverse of greedy)

```cpp
case EOps::ReNGRangeN:  // Non-greedy {n,}?
    if (leftenter) {
        re->param0 = re->s;
        re->oldParse = -1;
    }

    if (!re->param0 && re->oldParse == toParse)
        break;

    re->oldParse = toParse;

    if (!re->param0) {
        // After minimum: try continuing WITHOUT matching more
        insert_stack(&re, &prev, &toParse, &leftenter,
                    rea_Break,         // If continue succeeds, done
                    rea_NGRangeN_step2, // If fails, try one more match
                    &re->next,         // Try rest of pattern
                    &re,
                    toParse);
        continue;
    } else {
        re->param0--;
    }

    re = re->un.param;
    leftenter = true;
    continue;
```

## Example Trace: Pattern `a.*b` on "axxxbzzz"

```
Position: 0
1. Match 'a' at 0 → success, position=1
2. Enter .* (ReRangeN, min=0)
3. param0=0 (met minimum), create choice:
   - ifTrueReturn: rea_True (continue matching)
   - ifFalseReturn: rea_RangeN_step2 (try less)
   Stack: [pos=1, re=.*, ifTrue=rea_True, ifFalse=rea_RangeN_step2]
4. Match 'x' with . → success, position=2
5. param0=0 (still optional), create choice:
   Stack: [pos=1, ...], [pos=2, re=.*, ifTrue=rea_True, ifFalse=rea_RangeN_step2]
6. Match 'x' with . → success, position=3
   Stack: [pos=1, ...], [pos=2, ...], [pos=3, ...]
7. Match 'x' with . → success, position=4
   Stack: [pos=1, ...], [pos=2, ...], [pos=3, ...], [pos=4, ...]
8. Match 'b' with . → success, position=5
   Stack: [pos=1, ...], [pos=2, ...], [pos=3, ...], [pos=4, ...], [pos=5, ...]
9. Match 'y' with . → success, position=6
   Stack: [pos=1, ...], ..., [pos=5, ...], [pos=6, ...]
10. Match 'y' with . → success, position=7
    Stack: [pos=1, ...], ..., [pos=6, ...], [pos=7, ...]
11. Match 'y' with . → success, position=8
    Stack: [pos=1, ...], ..., [pos=7, ...], [pos=8, ...]
12. .* done, try to match 'b' at position=8
13. Match 'b' with 'z' at 8 → FAIL
14. check_stack(false) → Pop [pos=8], action=rea_RangeN_step2
15. action=rea_RangeN_step2 → insert choice: try continuing vs give up
    Then jump to re->next (the 'b' node) at pos=8
16. Match 'b' with 'z' at 8 → FAIL
17. check_stack(false) → Pop, action=rea_False
18. action=rea_False → check_stack(false) → Pop [pos=7], action=rea_RangeN_step2
19. Jump to 'b' node at pos=7
20. Match 'b' with 'y' at 7 → FAIL
21. Backtrack... (continue popping)
22. Eventually reach [pos=4]
23. Jump to 'b' node at pos=4
24. Match 'b' with 'b' at 4 → SUCCESS!
25. End of pattern → MATCH FOUND!
```

## Key Takeaways for C# Implementation

1. **Simplify quantifiers to ranges** during compilation
   - `*` → `Range(0, -1, greedy=true)`
   - `+` → `Range(1, -1, greedy=true)`
   - `?` → `Range(0, 1, greedy=true)`
   - `*?` → `Range(0, -1, greedy=false)`
   - etc.

2. **Implement backtracking stack** with:
   - `struct BacktrackPoint { IRegexNode node; int position; int action; ... }`
   - `Stack<BacktrackPoint> backtrackStack`

3. **Quantifier matching logic**:
   - Match minimum required times (no backtracking)
   - For each optional match:
     - **Greedy**: Push backtracking point, try to match, if fail pop and continue
     - **Non-greedy**: Push backtracking point, skip match, if fail pop and try to match

4. **Refactor MatchNode to support continuations**:
   - Need to be able to say "try matching rest of pattern, if fails come back here"
   - This requires significant refactoring of current implementation

## Complexity Estimate

**Option 1: Full Backtracking Stack** (C++ approach)
- **Effort**: 8-12 hours
- **Files to modify**:
  - `RegexMatcher.cs` - Complete rewrite of matching logic (~400 lines)
  - `QuantifierNode.cs` - Simplify to just store ranges
  - `RegexCompiler.cs` - Simplify quantifier creation
- **Pros**:
  - Correct behavior for all patterns
  - Matches C++ implementation
  - 96-100% test pass rate
- **Cons**:
  - Significant refactoring
  - More complex code

**Option 2: Simple Continuation** (Lighter approach)
- **Effort**: 3-4 hours
- **Approach**: Add continuation parameter to `MatchNode`
- **Pros**:
  - Smaller change
  - Still fixes most greedy issues
- **Cons**:
  - Less robust than full stack
  - May not handle all edge cases

## Recommendation

Given the project's emphasis on **correctness** and **compatibility** with existing HRC files (which likely use complex quantifier patterns), recommend **Option 1: Full Backtracking Stack**.

This matches the proven C++ implementation and ensures we handle all edge cases that real-world HRC files might throw at us.

## Next Steps

1. Create `BacktrackPoint` struct
2. Add `Stack<BacktrackPoint>` to `RegexMatcher`
3. Refactor `MatchQuantifier` to use backtracking
4. Update quantifier compilation to simplify `*`/`+`/`?` to ranges
5. Test with failing patterns
6. Run full test suite

**Estimated Time to Completion**: 8-12 hours of focused work
