# Complete Regex Engine Rewrite Plan - Direct C++ Port

## Overview
Complete rewrite of the regex engine to exactly match C++ implementation, ensuring 100% compatibility with HRC files and optimal performance for real-time parsing.

## Phase 1: Data Structure Definition (2 hours)

### 1.1 Create C++ Compatible Node Structure

**File**: `net/Far.Colorer/RegularExpressions/Internal/SRegInfo.cs`

```csharp
namespace Far.Colorer.RegularExpressions.Internal;

// Direct port of C++ SRegInfo structure
internal unsafe class SRegInfo
{
    // Core structure
    public SRegInfo* next;      // Next in sequence
    public SRegInfo* parent;    // Parent node
    public SRegInfo* prev;      // Previous (for backtracking)

    // Operation type (EOps enum)
    public int op;

    // Union simulation (C++ has union, we'll use fields)
    // Only one of these is valid based on op
    public SRegInfo* param;      // For brackets, quantifiers (un.param)
    public char symbol;          // For ReSymb (un.symbol)
    public CharacterClass* charclass; // For ReEnum/ReNEnum (un.charclass)
    public fixed char word[256]; // For ReWord (un.word)
    public int metaSymbol;       // For ReMetaSymb (un.metaSymbol)

    // Parameters
    public int param0;           // Multi-purpose (group number, etc.)
    public int param1;           // Range max for ReRangeNM
    public int s, e;            // Start/end for ranges
    public int oldParse;        // Zero-width loop detection

    // For named groups
    public int nameIndex;       // Index into name table

    // Metadata
    public int treeLevel;       // Debug/optimization hint
}
```

### 1.2 Create Stack Element Structure

**File**: `net/Far.Colorer/RegularExpressions/Internal/StackElem.cs`

```csharp
internal unsafe struct StackElem
{
    public SRegInfo* re;
    public SRegInfo* prev;
    public int toParse;
    public int ifTrueReturn;   // ReAction value
    public int ifFalseReturn;  // ReAction value
    public bool leftenter;
}
```

### 1.3 Define Operation Enum (EOps)

**File**: `net/Far.Colorer/RegularExpressions/Internal/EOps.cs`

```csharp
internal enum EOps
{
    ReEmpty = 0,
    ReBrackets = 0x1,
    ReSymb = 0x2,
    ReWord = 0x3,
    ReEnum = 0x4,
    ReNEnum = 0x5,
    ReBkTrace = 0x6,
    ReBkBrack = 0x7,
    ReAnyChr = 0x8,
    ReMetaSymb = 0x9,

    // Quantifiers
    ReMul = 0x10,        // * (converted to ReRangeN)
    RePlus = 0x11,       // + (converted to ReRangeN)
    ReQuest = 0x12,      // ? (converted to ReRangeNM)
    ReNGMul = 0x13,      // *? (converted to ReNGRangeN)
    ReNGPlus = 0x14,     // +? (converted to ReNGRangeN)
    ReNGQuest = 0x15,    // ?? (converted to ReNGRangeNM)

    ReRangeN = 0x16,     // {n,}
    ReRangeNM = 0x17,    // {n,m}
    ReNGRangeN = 0x18,   // {n,}?
    ReNGRangeNM = 0x19,  // {n,m}?

    // Special
    ReNamedBrackets = 0x1A,
    ReBkTraceName = 0x1B,
    ReBkBrackName = 0x1C,
    ReOr = 0x1D,
    ReBehind = 0x1E,
    ReNBehind = 0x1F,
    ReBkTraceNName = 0x20,
    ReBkBrackNName = 0x21,

    ReSymbolOps = 0x40   // Marker for symbol operations
}
```

### 1.4 Define Action Enum

**File**: `net/Far.Colorer/RegularExpressions/Internal/ReAction.cs`

```csharp
internal enum ReAction
{
    rea_False = 0,
    rea_True = 1,
    rea_Break = 2,
    rea_RangeNM_step2 = 3,
    rea_RangeNM_step3 = 4,
    rea_RangeN_step2 = 5,
    rea_NGRangeN_step2 = 6,
    rea_NGRangeNM_step2 = 7,
    rea_NGRangeNM_step3 = 8
}
```

## Phase 2: Compiler Rewrite (3 hours)

### 2.1 Create New Compiler

**File**: `net/Far.Colorer/RegularExpressions/Internal/CRegExpCompiler.cs`

```csharp
internal unsafe class CRegExpCompiler
{
    private SRegInfo* tree;
    private SRegInfo* firstNode;
    private SRegInfo* lastNode;
    private SRegInfo* current;
    private readonly string pattern;
    private int position;
    private Stack<SRegInfo*> groupStack;

    public SRegInfo* Compile(string pattern, RegexOptions options)
    {
        this.pattern = pattern;
        this.position = 0;

        // Allocate root node
        tree = AllocateNode();
        tree->op = EOps.ReEmpty;

        // Parse pattern
        ParseSequence(tree);

        // Post-process: convert basic quantifiers to ranges
        ConvertQuantifiersToRanges(tree);

        return tree;
    }

    private void ParseSequence(SRegInfo* parent)
    {
        SRegInfo* prev = null;
        SRegInfo* first = null;

        while (position < pattern.Length)
        {
            char ch = pattern[position];

            // Check for sequence end
            if (ch == ')' || ch == '|') break;

            SRegInfo* node = ParseAtom();
            if (node == null) continue;

            // Check for quantifiers
            if (position < pattern.Length)
            {
                node = ParseQuantifier(node);
            }

            // Link nodes
            if (first == null)
            {
                first = node;
                parent->param = first;
            }

            if (prev != null)
            {
                prev->next = node;
                node->prev = prev;
            }

            node->parent = parent;
            prev = node;
        }
    }

    private SRegInfo* ParseAtom()
    {
        if (position >= pattern.Length) return null;

        char ch = pattern[position];
        SRegInfo* node;

        switch (ch)
        {
            case '\\':
                return ParseEscape();

            case '(':
                return ParseGroup();

            case '.':
                node = AllocateNode();
                node->op = EOps.ReAnyChr;
                position++;
                return node;

            case '^':
                node = AllocateNode();
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = CMetaSymbol.ZeroStart;
                position++;
                return node;

            case '$':
                node = AllocateNode();
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = CMetaSymbol.ZeroEnd;
                position++;
                return node;

            case '[':
                return ParseCharacterClass();

            default:
                // Literal character
                node = AllocateNode();
                node->op = EOps.ReSymb;
                node->symbol = ch;
                position++;
                return node;
        }
    }

    private SRegInfo* ParseQuantifier(SRegInfo* atom)
    {
        if (position >= pattern.Length) return atom;

        char ch = pattern[position];
        bool nonGreedy = false;

        // Check for non-greedy modifier
        if (position + 1 < pattern.Length && pattern[position + 1] == '?')
        {
            nonGreedy = true;
        }

        SRegInfo* quant = AllocateNode();
        quant->param = atom;
        atom->parent = quant;

        switch (ch)
        {
            case '*':
                quant->op = nonGreedy ? EOps.ReNGRangeN : EOps.ReRangeN;
                quant->s = 0;
                quant->e = -1;
                position++;
                if (nonGreedy) position++;
                break;

            case '+':
                quant->op = nonGreedy ? EOps.ReNGRangeN : EOps.ReRangeN;
                quant->s = 1;
                quant->e = -1;
                position++;
                if (nonGreedy) position++;
                break;

            case '?':
                quant->op = nonGreedy ? EOps.ReNGRangeNM : EOps.ReRangeNM;
                quant->s = 0;
                quant->e = 1;
                position++;
                if (nonGreedy) position++;
                break;

            case '{':
                return ParseRangeQuantifier(atom);

            default:
                // Not a quantifier
                return atom;
        }

        return quant;
    }

    private SRegInfo* AllocateNode()
    {
        // Use native memory for performance
        SRegInfo* node = (SRegInfo*)Marshal.AllocHGlobal(sizeof(SRegInfo));
        *node = default;
        return node;
    }
}
```

## Phase 3: Matcher Implementation (4 hours)

### 3.1 Create CRegExp Matcher Class

**File**: `net/Far.Colorer/RegularExpressions/Internal/CRegExp.cs`

```csharp
internal unsafe class CRegExp
{
    private SRegInfo* tree;
    private StackElem* stack;
    private int stackSize = 256;
    private int count_elem;

    private readonly char* pattern;
    private readonly int patternLength;
    private int end;
    private bool ignoreCase;

    // Match results
    private int* matchS;  // Start positions
    private int* matchE;  // End positions
    private const int MaxGroups = 10;

    public CRegExp(SRegInfo* compiledTree, string inputPattern, RegexOptions options)
    {
        tree = compiledTree;
        ignoreCase = (options & RegexOptions.IgnoreCase) != 0;

        // Allocate stack
        stack = (StackElem*)Marshal.AllocHGlobal(sizeof(StackElem) * stackSize);

        // Allocate match arrays
        matchS = (int*)Marshal.AllocHGlobal(sizeof(int) * MaxGroups);
        matchE = (int*)Marshal.AllocHGlobal(sizeof(int) * MaxGroups);
    }

    public bool Parse(char* input, int inputLength, int startPos, int endPos)
    {
        pattern = input;
        patternLength = inputLength;
        end = endPos;

        for (int pos = startPos; pos <= endPos; pos++)
        {
            if (LowParse(tree, null, pos))
            {
                return true;
            }
        }

        return false;
    }

    // CRITICAL: This must be a line-by-line port of C++ lowParse
    private bool LowParse(SRegInfo* re, SRegInfo* prev, int toParse)
    {
        int action = -1;  // Start with "continue processing"
        bool leftenter = true;
        count_elem = 0;   // Reset stack

        // Initialize match positions
        for (int i = 0; i < MaxGroups; i++)
        {
            matchS[i] = matchE[i] = -1;
        }

        // Main loop - EXACT C++ MATCH (line 857)
        while (re != null || action != -1)
        {
            // Process node when action == -1 (line 858)
            if (re != null && action == -1)
            {
                switch ((EOps)re->op)
                {
                    case EOps.ReEmpty:
                        break;

                    case EOps.ReBrackets:
                    case EOps.ReNamedBrackets:
                        if (leftenter)
                        {
                            re->s = toParse;
                            re = re->param;
                            continue;
                        }
                        if (re->param0 == -1)
                            break;
                        if (re->op == (int)EOps.ReBrackets)
                        {
                            matchS[re->param0] = re->s;
                            matchE[re->param0] = toParse;
                        }
                        break;

                    case EOps.ReSymb:
                        if (toParse >= end)
                        {
                            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                            continue;
                        }
                        if (ignoreCase)
                        {
                            if (CharToLower(pattern[toParse]) != CharToLower(re->symbol) &&
                                CharToUpper(pattern[toParse]) != CharToUpper(re->symbol))
                            {
                                check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                                continue;
                            }
                        }
                        else if (pattern[toParse] != re->symbol)
                        {
                            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                            continue;
                        }
                        toParse++;
                        break;

                    case EOps.ReMetaSymb:
                        if (!CheckMetaSymbol(re->metaSymbol, toParse))
                        {
                            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                            continue;
                        }
                        break;

                    case EOps.ReAnyChr:
                        if (toParse >= end)
                        {
                            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                            continue;
                        }
                        toParse++;
                        break;

                    case EOps.ReEnum:
                        if (toParse >= end)
                        {
                            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                            continue;
                        }
                        if (!re->charclass->Contains(pattern[toParse]))
                        {
                            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                            continue;
                        }
                        toParse++;
                        break;

                    case EOps.ReNEnum:
                        if (toParse >= end)
                        {
                            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                            continue;
                        }
                        if (re->charclass->Contains(pattern[toParse]))
                        {
                            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                            continue;
                        }
                        toParse++;
                        break;

                    case EOps.ReRangeN:  // {n,} greedy
                        if (leftenter)
                        {
                            re->param0 = re->s;
                            re->oldParse = -1;
                        }
                        if (re->param0 == 0 && re->oldParse == toParse)
                            break;
                        re->oldParse = toParse;

                        if (re->param0 == 0)
                        {
                            // Try matching more
                            insert_stack(&re, &prev, &toParse, &leftenter,
                                (int)ReAction.rea_True, (int)ReAction.rea_RangeN_step2,
                                &re->param, null, toParse);
                            continue;
                        }
                        else
                        {
                            re->param0--;
                        }
                        re = re->param;
                        leftenter = true;
                        continue;

                    case EOps.ReRangeNM:  // {n,m} greedy
                        if (leftenter)
                        {
                            re->param0 = re->s;
                            re->param1 = re->e - re->s;
                            re->oldParse = -1;
                        }
                        if (re->param0 == 0)
                        {
                            if (re->param1 > 0)
                            {
                                re->param1--;
                            }
                            else
                            {
                                insert_stack(&re, &prev, &toParse, &leftenter,
                                    (int)ReAction.rea_True, (int)ReAction.rea_False,
                                    &re->next, &re, toParse);
                                continue;
                            }
                            insert_stack(&re, &prev, &toParse, &leftenter,
                                (int)ReAction.rea_True, (int)ReAction.rea_RangeNM_step2,
                                &re->param, null, toParse);
                            continue;
                        }
                        else
                        {
                            re->param0--;
                        }
                        re = re->param;
                        leftenter = true;
                        continue;

                    case EOps.ReNGRangeN:  // {n,}? non-greedy
                        if (leftenter)
                        {
                            re->param0 = re->s;
                            re->oldParse = -1;
                        }
                        if (re->param0 == 0 && re->oldParse == toParse)
                            break;
                        re->oldParse = toParse;

                        if (re->param0 == 0)
                        {
                            // Try NOT matching (non-greedy)
                            insert_stack(&re, &prev, &toParse, &leftenter,
                                (int)ReAction.rea_True, (int)ReAction.rea_NGRangeN_step2,
                                &re->next, &re, toParse);
                            continue;
                        }
                        else
                        {
                            re->param0--;
                        }
                        re = re->param;
                        leftenter = true;
                        continue;

                    case EOps.ReNGRangeNM:  // {n,m}? non-greedy
                        if (leftenter)
                        {
                            re->param0 = re->s;
                            re->param1 = re->e - re->s;
                            re->oldParse = -1;
                        }
                        if (re->param0 == 0)
                        {
                            // Try NOT matching first (non-greedy)
                            insert_stack(&re, &prev, &toParse, &leftenter,
                                (int)ReAction.rea_True, (int)ReAction.rea_NGRangeNM_step2,
                                &re->next, &re, toParse);
                            continue;
                        }
                        else
                        {
                            re->param0--;
                        }
                        re = re->param;
                        leftenter = true;
                        continue;

                    case EOps.ReOr:
                        if (!leftenter)
                        {
                            while (re->next != null) re = re->next;
                            break;
                        }
                        insert_stack(&re, &prev, &toParse, &leftenter,
                            (int)ReAction.rea_True, (int)ReAction.rea_Break,
                            &re->param, null, toParse);
                        continue;

                    default:
                        // Unknown op
                        check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                        continue;
                }
            }

            // CRITICAL: Node advancement (C++ lines 1337-1344)
            if (action == -1 && re != null)
            {
                if (re->next == null)
                {
                    re = re->parent;
                    leftenter = false;
                }
                else
                {
                    re = re->next;
                    leftenter = true;
                }
            }

            // Handle actions (C++ lines 1280-1335)
            switch ((ReAction)action)
            {
                case ReAction.rea_False:
                    if (count_elem > 0)
                    {
                        check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                        continue;
                    }
                    else
                    {
                        return false;
                    }

                case ReAction.rea_True:
                    if (count_elem > 0)
                    {
                        check_stack(true, &re, &prev, &toParse, &leftenter, &action);
                        continue;
                    }
                    else
                    {
                        return true;
                    }

                case ReAction.rea_Break:
                    action = -1;
                    break;

                case ReAction.rea_RangeN_step2:
                    action = -1;
                    insert_stack(&re, &prev, &toParse, &leftenter,
                        (int)ReAction.rea_True, (int)ReAction.rea_False,
                        &re->next, &re, toParse);
                    continue;

                case ReAction.rea_RangeNM_step2:
                    action = -1;
                    if (re->param1 > 0)
                    {
                        re->param1--;
                        insert_stack(&re, &prev, &toParse, &leftenter,
                            (int)ReAction.rea_True, (int)ReAction.rea_RangeNM_step3,
                            &re->param, null, toParse);
                    }
                    else
                    {
                        insert_stack(&re, &prev, &toParse, &leftenter,
                            (int)ReAction.rea_True, (int)ReAction.rea_False,
                            &re->next, &re, toParse);
                    }
                    continue;

                case ReAction.rea_RangeNM_step3:
                    action = -1;
                    re->param1++;
                    check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                    continue;

                case ReAction.rea_NGRangeN_step2:
                    action = -1;
                    if (re->param0 > 0)
                        re->param0--;
                    re = re->param;
                    leftenter = true;
                    continue;

                case ReAction.rea_NGRangeNM_step2:
                    action = -1;
                    if (re->param0 > 0)
                        re->param0--;
                    if (re->param1 > 0)
                    {
                        re->param1--;
                        re = re->param;
                        leftenter = true;
                    }
                    else
                    {
                        check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                    }
                    continue;

                case ReAction.rea_NGRangeNM_step3:
                    action = -1;
                    re->param1++;
                    check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                    continue;
            }
        }

        // Final check (C++ line 1346)
        check_stack(true, &re, &prev, &toParse, &leftenter, &action);
        return action == (int)ReAction.rea_True;
    }

    private void insert_stack(SRegInfo** re, SRegInfo** prev, int* toParse, bool* leftenter,
        int ifTrueReturn, int ifFalseReturn,
        SRegInfo** re2, SRegInfo** prev2, int toParse2)
    {
        if (count_elem >= stackSize)
        {
            // Expand stack
            stackSize *= 2;
            StackElem* newStack = (StackElem*)Marshal.AllocHGlobal(sizeof(StackElem) * stackSize);
            Buffer.MemoryCopy(stack, newStack, stackSize * sizeof(StackElem), count_elem * sizeof(StackElem));
            Marshal.FreeHGlobal((IntPtr)stack);
            stack = newStack;
        }

        StackElem* elem = &stack[count_elem++];
        elem->re = *re;
        elem->prev = *prev;
        elem->toParse = *toParse;
        elem->ifTrueReturn = ifTrueReturn;
        elem->ifFalseReturn = ifFalseReturn;
        elem->leftenter = *leftenter;

        *re = *re2;
        if (prev2 != null) *prev = *prev2;
        *toParse = toParse2;
        *leftenter = true;
    }

    private void check_stack(bool res, SRegInfo** re, SRegInfo** prev, int* toParse, bool* leftenter, int* action)
    {
        if (count_elem == 0)
        {
            *action = res ? (int)ReAction.rea_True : (int)ReAction.rea_False;
            return;
        }

        StackElem* elem = &stack[--count_elem];
        *action = res ? elem->ifTrueReturn : elem->ifFalseReturn;
        *re = elem->re;
        *prev = elem->prev;
        *toParse = elem->toParse;
        *leftenter = elem->leftenter;
    }

    private bool CheckMetaSymbol(int metaSymbol, int pos)
    {
        switch (metaSymbol)
        {
            case CMetaSymbol.ZeroStart:
                return pos == 0;
            case CMetaSymbol.ZeroEnd:
                return pos == end;
            case CMetaSymbol.WordBoundary:
                // Check word boundary
                bool prevWord = pos > 0 && IsWordChar(pattern[pos - 1]);
                bool nextWord = pos < end && IsWordChar(pattern[pos]);
                return prevWord != nextWord;
            case CMetaSymbol.NonWordBoundary:
                return !CheckMetaSymbol(CMetaSymbol.WordBoundary, pos);
            case CMetaSymbol.DigitChar:
                return pos < end && char.IsDigit(pattern[pos]);
            case CMetaSymbol.NonDigitChar:
                return pos < end && !char.IsDigit(pattern[pos]);
            case CMetaSymbol.SpaceChar:
                return pos < end && char.IsWhiteSpace(pattern[pos]);
            case CMetaSymbol.NonSpaceChar:
                return pos < end && !char.IsWhiteSpace(pattern[pos]);
            case CMetaSymbol.WordChar:
                return pos < end && IsWordChar(pattern[pos]);
            case CMetaSymbol.NonWordChar:
                return pos < end && !IsWordChar(pattern[pos]);
            default:
                return false;
        }
    }

    private bool IsWordChar(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }

    private char CharToLower(char c)
    {
        // Simple ASCII lowercase for performance
        if (c >= 'A' && c <= 'Z')
            return (char)(c + 32);
        return c;
    }

    private char CharToUpper(char c)
    {
        // Simple ASCII uppercase for performance
        if (c >= 'a' && c <= 'z')
            return (char)(c - 32);
        return c;
    }
}
```

### 3.2 Define Meta Symbol Constants

**File**: `net/Far.Colorer/RegularExpressions/Internal/CMetaSymbol.cs`

```csharp
internal static class CMetaSymbol
{
    public const int ZeroStart = 1;       // ^
    public const int ZeroEnd = 2;         // $
    public const int WordBoundary = 3;    // \b
    public const int NonWordBoundary = 4; // \B
    public const int DigitChar = 5;       // \d
    public const int NonDigitChar = 6;    // \D
    public const int SpaceChar = 7;       // \s
    public const int NonSpaceChar = 8;    // \S
    public const int WordChar = 9;        // \w
    public const int NonWordChar = 10;    // \W
    public const int Tab = 11;            // \t
    public const int Newline = 12;        // \n
    public const int Return = 13;         // \r
}
```

## Phase 4: Integration Layer (1 hour)

### 4.1 Create Managed Wrapper

**File**: `net/Far.Colorer/RegularExpressions/Regex.cs`

```csharp
public class Regex
{
    private readonly unsafe SRegInfo* compiledTree;
    private readonly RegexOptions options;
    private readonly string pattern;

    public Regex(string pattern, RegexOptions options = RegexOptions.None)
    {
        this.pattern = pattern;
        this.options = options;

        unsafe
        {
            var compiler = new CRegExpCompiler();
            compiledTree = compiler.Compile(pattern, options);
        }
    }

    public bool IsMatch(string input)
    {
        return IsMatch(input, 0);
    }

    public bool IsMatch(string input, int startPos)
    {
        unsafe
        {
            fixed (char* inputPtr = input)
            {
                var matcher = new CRegExp(compiledTree, pattern, options);
                return matcher.Parse(inputPtr, input.Length, startPos, input.Length);
            }
        }
    }

    public ColorerMatch? Match(string input, int startPos = 0)
    {
        unsafe
        {
            fixed (char* inputPtr = input)
            {
                var matcher = new CRegExp(compiledTree, pattern, options);
                if (matcher.Parse(inputPtr, input.Length, startPos, input.Length))
                {
                    // Extract match groups
                    return ExtractMatch(matcher);
                }
            }
        }
        return null;
    }

    // Cleanup
    ~Regex()
    {
        unsafe
        {
            if (compiledTree != null)
            {
                FreeTree(compiledTree);
            }
        }
    }

    private unsafe void FreeTree(SRegInfo* node)
    {
        if (node == null) return;
        FreeTree(node->next);
        FreeTree(node->param);
        Marshal.FreeHGlobal((IntPtr)node);
    }
}
```

## Phase 5: Testing Strategy (1 hour)

### 5.1 Unit Tests for Each Component

1. **Compiler Tests**:
   - Test each pattern type compilation
   - Verify tree structure
   - Check quantifier conversion

2. **Matcher Tests**:
   - Test each operator individually
   - Test backtracking scenarios
   - Test greedy vs non-greedy

3. **Integration Tests**:
   - Run all 50 existing tests
   - Add C++ compatibility tests
   - Performance benchmarks

### 5.2 Debug Utilities

```csharp
internal static unsafe class DebugUtils
{
    public static void DumpTree(SRegInfo* node, int indent = 0)
    {
        if (node == null) return;

        Console.WriteLine($"{new string(' ', indent)}Op: {(EOps)node->op}, s={node->s}, e={node->e}");

        if (node->param != null)
        {
            Console.WriteLine($"{new string(' ', indent)}  Param:");
            DumpTree(node->param, indent + 4);
        }

        if (node->next != null)
        {
            Console.WriteLine($"{new string(' ', indent)}Next:");
            DumpTree(node->next, indent);
        }
    }

    public static void DumpStack(StackElem* stack, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var elem = stack[i];
            Console.WriteLine($"Stack[{i}]: toParse={elem.toParse}, ifTrue={elem.ifTrueReturn}, ifFalse={elem.ifFalseReturn}");
        }
    }
}
```

## Phase 6: Performance Optimization (1 hour)

### 6.1 Optimizations to Apply

1. **Memory Management**:
   - Pool SRegInfo allocations
   - Reuse stack between matches
   - Use stackalloc for small buffers

2. **Fast Paths**:
   - Quick literal prefix check
   - Boyer-Moore for long literals
   - Character class bitmaps

3. **Inlining**:
   ```csharp
   [MethodImpl(MethodImplOptions.AggressiveInlining)]
   private bool IsWordChar(char c) { ... }
   ```

4. **SIMD Operations**:
   - Use Vector<char> for character searches
   - Vectorized case conversion

## Implementation Timeline

### Day 1 (8 hours)
- **Hours 1-2**: Phase 1 - Data structures
- **Hours 3-5**: Phase 2 - Compiler
- **Hours 6-8**: Phase 3 - Matcher core loop

### Day 2 (4 hours)
- **Hour 9**: Phase 3 - Complete matcher
- **Hour 10**: Phase 4 - Integration
- **Hour 11**: Phase 5 - Testing
- **Hour 12**: Phase 6 - Optimization

## Critical Success Factors

1. **Exact C++ Match**: Every line in lowParse must match C++
2. **Unsafe Code**: Use pointers for performance
3. **No Allocations**: After setup, zero allocations per match
4. **Comprehensive Testing**: Test every code path

## Risk Mitigation

1. **Keep Old Code**: Don't delete existing implementation until new one works
2. **Incremental Testing**: Test each component as built
3. **Debug Logging**: Add extensive logging during development
4. **Memory Safety**: Use debug builds with bounds checking

## Validation Checklist

- [ ] Pattern "ab+c" matches "abc" without hanging
- [ ] All 50 tests pass
- [ ] Performance < 1ms for typical patterns
- [ ] Zero memory leaks (use memory profiler)
- [ ] Handles all Colorer-specific features
- [ ] Matches C++ behavior exactly

## Final Notes

This is a complete rewrite that directly ports the C++ implementation. The key is to:

1. Match C++ data structures exactly
2. Port lowParse line-by-line
3. Use unsafe code for performance
4. Test comprehensively

The implementation should be done incrementally, testing each phase before moving to the next.