# Regex Matcher Debugging Status

## Current Problem
The C++ state machine implementation is hanging (not using CPU, but taking >10 seconds) on even simple patterns like "ab+c" matching "abc". The test suite times out after 60 seconds.

## What We're Trying To Do
Port the C++ Colorer regex engine's backtracking algorithm exactly to .NET. We need this for HRC file compatibility - native .NET regex doesn't support Colorer's special features like cross-pattern backreferences (\yN, \y{name}).

## Test Status
- Before C++ port: 42/50 tests passing (84%)
- After C++ port: Tests hang, never complete
- User notes: "This time test doesn't cycle CPU at 100%" - suggesting it's not an infinite loop, but possibly deadlocked or waiting

## Architecture Implemented

### AST Structure
Successfully changed from tree (SequenceNode with List<IRegexNode>) to linked list:
- Nodes have `Next`, `Previous`, `Parent` properties
- RegexCompiler.ParseSequence() builds linked list correctly
- QuantifierNode has Target property pointing to sub-expression

### C++ Control Flow Mapping
The C++ uses:
```cpp
while (re || action != -1) {
  if (re && action == -1)
    switch (re->op) {
      case ReSymb:
        if (match) { toParse++; break; }
        else { check_stack(false, ...); continue; }
      case ReRangeN: // Plus quantifier
        if (leftenter) re->param0 = re->s;
        if (re->param0) { re->param0--; }
        re = re->un.param;  // Jump to target
        leftenter = true;
        continue;
    }
  // After switch, advance to next node
  if (!re->next) { re = re->parent; leftenter = false; }
  else { re = re->next; leftenter = true; }
  
  switch (action) {
    case rea_False: check_stack(false, ...); continue;
    case rea_True: check_stack(true, ...); continue;
    // Custom actions for quantifier backtracking
  }
}
```

Key insight: After processing a node with `break`, C++ automatically advances to `re->next`.

### Our Implementation
**File**: `net/Far.Colorer/RegularExpressions/Internal/RegexMatcher.cs` (779 lines)

**Main Loop**:
```csharp
private bool LowParse(IRegexNode? startNode, ref MatchContext ctx)
{
    IRegexNode? re = startNode;
    int action = Action_Continue;
    bool leftEnter = true;

    while (true)
    {
        // Inner loop: process nodes while action == Continue
        while (re != null && action == Action_Continue)
        {
            ProcessNode(ref re, ref ctx, ref leftEnter, ref action);
        }

        // Handle action results
        switch (action)
        {
            case Action_False:
                if (ctx.StackCount > 0) {
                    CheckStack(false, ref ctx, ref re, ref leftEnter, ref action);
                    continue;
                }
                return false;
            case Action_True:
                if (ctx.StackCount > 0) {
                    CheckStack(true, ref ctx, ref re, ref leftEnter, ref action);
                    continue;
                }
                return true;
            case Action_Continue:
                if (re == null) return true;
                break;  // <-- Potential infinite loop point
            default:
                HandleCustomAction(action, ref ctx, ref re, ref leftEnter, ref action);
                continue;
        }
    }
}
```

**ProcessNode**: Modifies `re`, `leftEnter`, `action` parameters directly:
```csharp
private void ProcessNode(ref IRegexNode? re, ref MatchContext ctx, ref bool leftEnter, ref int action)
{
    switch (re.Operator)
    {
        case RegexOperator.Symb:
            if (MatchLiteral((LiteralNode)re, ref ctx)) {
                re = re.Next;  // Advance to next
                leftEnter = true;
            } else {
                action = Action_False;
            }
            break;
        case RegexOperator.Plus:
            ProcessPlus((QuantifierNode)re, ref re, ref ctx, ref leftEnter, ref action);
            break;
    }
}
```

**ProcessPlus** (greedy + quantifier):
```csharp
private void ProcessPlus(QuantifierNode quant, ref IRegexNode? re, ref MatchContext ctx, ref bool leftEnter, ref int action)
{
    bool isNonGreedy = quant.Operator == RegexOperator.PlusNonGreedy;

    if (leftEnter) {
        quant.Counter = 1; // Must match at least once
        quant.OldParse = -1;
    }

    if (quant.Counter > 0) {
        // Still matching minimum
        quant.Counter--;
        re = quant.Target;  // Jump to target
        leftEnter = true;
        return;
    }

    // Prevent infinite zero-width loops
    if (quant.OldParse == ctx.CurrentPos) {
        re = quant.Next;
        leftEnter = true;
        return;
    }

    quant.OldParse = ctx.CurrentPos;

    // Greedy: try matching one more
    if (isNonGreedy) {
        InsertStack(ref re, ref ctx, ref leftEnter,
            Action_Continue, Action_NGRangeN_Step2,
            quant.Next, ctx.CurrentPos);
    } else {
        InsertStack(ref re, ref ctx, ref leftEnter,
            Action_True, Action_RangeN_Step2,
            quant.Target, ctx.CurrentPos);
    }
}
```

**InsertStack** (push backtrack point):
```csharp
private void InsertStack(
    ref IRegexNode? re,
    ref MatchContext ctx,
    ref bool leftEnter,
    int ifTrueReturn,
    int ifFalseReturn,
    IRegexNode? re2,
    int toParse2)
{
    // Push current state
    ref var point = ref ctx.BacktrackStack[ctx.StackCount++];
    point.Node = re;
    point.Position = ctx.CurrentPos;
    point.IfTrueReturn = ifTrueReturn;
    point.IfFalseReturn = ifFalseReturn;
    point.LeftEnter = leftEnter;

    // Jump to new state
    re = re2;
    ctx.CurrentPos = toParse2;
    leftEnter = true;
}
```

**HandleCustomAction** (backtracking handlers):
```csharp
private void HandleCustomAction(int actionCode, ref MatchContext ctx, ref IRegexNode? re, ref bool leftEnter, ref int action)
{
    switch (actionCode)
    {
        case Action_RangeN_Step2:  // Greedy failed, try less
            action = Action_Continue;
            InsertStack(ref re, ref ctx, ref leftEnter,
                Action_True, Action_False,
                re!.Next, ctx.CurrentPos);
            break;

        case Action_NGRangeN_Step2:  // Non-greedy, try more
            action = Action_Continue;
            var quant = (QuantifierNode)re!;
            if (quant.Counter > 0) quant.Counter--;
            re = quant.Target;
            leftEnter = true;
            break;
    }
}
```

## Action Code Values
**C++ enum (cregexp.h:181-191)**:
```cpp
enum ReAction {
  rea_False = 0,
  rea_True = 1,
  rea_Break = 2,
  rea_RangeNM_step2 = 3,
  rea_RangeNM_step3 = 4,
  rea_RangeN_step2 = 5,
  rea_NGRangeN_step2 = 6,
  rea_NGRangeNM_step2 = 7,
  rea_NGRangeNM_step3 = 8
};
```

**Our constants**:
```csharp
private const int Action_False = -2;
private const int Action_True = -3;
private const int Action_Continue = 0;
private const int Action_RangeN_Step2 = 1;
private const int Action_RangeNM_Step2 = 2;
private const int Action_NGRangeN_Step2 = 3;
private const int Action_NGRangeNM_Step2 = 4;
```

**DISCREPANCY NOTED**: C++ uses -1 for "continue processing" (loop condition: `action != -1`), but we use 0 for Action_Continue. This might be causing issues!

## Potential Issues Identified

### Issue 1: Action Code Mismatch
C++ loop: `while (re || action != -1)` - continues while action is NOT -1
Our loop: `while (re != null && action == Action_Continue)` where `Action_Continue = 0`

The C++ uses -1 as the "keep going" value, but we're using 0. Need to verify this isn't causing control flow problems.

### Issue 2: Infinite Loop Detection Added But Not Triggered
Added code:
```csharp
if (++iterationCount > 10000) {
    throw new RegexSyntaxException($"Infinite loop detected at position {ctx.CurrentPos}, node type: {re?.Operator}");
}
```

This should have thrown an exception, but the program just hangs silently. This suggests the loop isn't iterating at all - possibly deadlocked or waiting on something.

### Issue 3: Possible Control Flow Bug
After ProcessNode returns, if `action == Action_Continue` and `re != null`, the inner while loop continues. But if ProcessNode doesn't modify anything, this could loop infinitely. However, the iteration counter should catch this, and it doesn't.

### Issue 4: Not Using CPU
User noted "this time test doesn't cycle CPU at 100%", which means:
- NOT an infinite tight loop
- Possibly waiting on I/O, lock, or some blocking operation
- Or the process is genuinely stuck in a deadlock (but single-threaded, so unlikely)
- Or there's some long-running operation we're not aware of

## Files Modified in This Session

1. **RegexCompiler.cs**: ParseSequence() changed to build linked list
2. **QuantifierNode.cs**: Added mutable fields (Counter, Remaining, OldParse)
3. **GroupNode.cs**: Added mutable field (StartPos)
4. **RegexMatcher.cs**: Complete rewrite twice (~779 lines final version)

## How to Test

Simple test that should work but hangs:
```csharp
var regex = new Regex("ab+c");
regex.IsMatch("abc")  // Should return true, but hangs
```

## What Needs Investigation

1. **Why isn't the infinite loop detector triggering?** If there's a tight loop, iteration count should hit 10000 quickly.

2. **Action code mapping**: Verify the action codes match C++ behavior. The C++ uses -1 for "continue", we use 0.

3. **Control flow equivalence**: Does our ProcessNode + main loop truly match the C++ switch + advance + action switch pattern?

4. **Backtracking logic**: When CheckStack pops with Action_RangeN_Step2, does HandleCustomAction correctly replicate C++ behavior?

5. **Hidden blocking**: Is there any code path that could cause blocking/waiting without spinning CPU?

## Next Steps to Try

1. Check if the issue is in pattern compilation (print the AST before matching)
2. Add verbose logging to see which node types are being processed
3. Verify action codes match C++ values exactly
4. Simplify to absolute minimum - just literal matching, no quantifiers
5. Check if there's some initialization issue causing null refs or similar

## Reference Files

- C++ implementation: `native/src/colorer/cregexp/cregexp.cpp` lines 857-1345 (lowParse function)
- C++ header: `native/src/colorer/cregexp/cregexp.h` lines 180-191 (ReAction enum)
- Our implementation: `net/Far.Colorer/RegularExpressions/Internal/RegexMatcher.cs`
- Test file: `test_debug.cs` (simple pattern "ab+c")

## Build Status

✅ Code builds successfully with no warnings or errors
❌ Tests hang and timeout
