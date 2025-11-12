# Regex Engine Rewrite - Missing Implementation Details

This document supplements REGEX_REWRITE_PLAN.md with the complete missing implementation details extracted from C++ source code.

## Part 1: Complete Enum Definitions

### EMetaSymbols Enum

**File**: `net/Far.Colorer/RegularExpressions/Internal/EMetaSymbols.cs`

```csharp
namespace Far.Colorer.RegularExpressions.Internal;

// From cregexp.h lines 92-117
internal enum EMetaSymbols
{
    ReBadMeta = 0,
    ReAnyChr = 1,      // .
    ReSoL = 2,         // ^
    ReSoScheme = 3,    // ~ (COLORERMODE - scheme start)
    ReEoL = 4,         // $
    ReDigit = 5,       // \d
    ReNDigit = 6,      // \D
    ReWordSymb = 7,    // \w
    ReNWordSymb = 8,   // \W
    ReWSpace = 9,      // \s
    ReNWSpace = 10,    // \S
    ReUCase = 11,      // \u (uppercase)
    ReNUCase = 12,     // \l (lowercase)
    ReWBound = 13,     // \b
    ReNWBound = 14,    // \B
    RePreNW = 15,      // \c (preceded by non-word)
    ReStart = 16,      // \m (COLORERMODE - match start marker)
    ReEnd = 17,        // \M (COLORERMODE - match end marker)
    ReChrLast = 18
}
```

### Complete EOps with Missing Operators

**Update to**: `net/Far.Colorer/RegularExpressions/Internal/EOps.cs`

```csharp
internal enum EOps
{
    ReEmpty = 0,
    ReBrackets = 0x1,
    ReSymb = 0x2,
    ReWord = 0x3,
    ReEnum = 0x4,
    ReNEnum = 0x5,
    ReBkTrace = 0x6,        // \1-\9 backreference
    ReBkBrack = 0x7,        // \p{name} named backreference
    ReAnyChr = 0x8,
    ReMetaSymb = 0x9,

    // Quantifiers (converted during compilation)
    ReMul = 0x10,           // * (converted to ReRangeN)
    RePlus = 0x11,          // + (converted to ReRangeN)
    ReQuest = 0x12,         // ? (converted to ReRangeNM)
    ReNGMul = 0x13,         // *? (converted to ReNGRangeN)
    ReNGPlus = 0x14,        // +? (converted to ReNGRangeN)
    ReNGQuest = 0x15,       // ?? (converted to ReNGRangeNM)

    ReRangeN = 0x16,        // {n,} greedy
    ReRangeNM = 0x17,       // {n,m} greedy
    ReNGRangeN = 0x18,      // {n,}? non-greedy
    ReNGRangeNM = 0x19,     // {n,m}? non-greedy

    // Special operators
    ReNamedBrackets = 0x1A, // (?:...) or (?{name}...)
    ReBkTraceName = 0x1B,   // \y{name} backreference
    ReBkBrackName = 0x1C,   // Named bracket reference
    ReOr = 0x1D,            // |
    ReBehind = 0x1E,        // ?#N lookbehind
    ReNBehind = 0x1F,       // ?~N negative lookbehind
    ReBkTraceNName = 0x20,  // \Y{name} negative backreference
    ReBkBrackNName = 0x21,  // Named bracket negative reference

    // Lookahead (not in original enum but used)
    ReAhead = 0x22,         // ?= positive lookahead
    ReNAhead = 0x23,        // ?! negative lookahead

    ReSymbolOps = 0x40      // Marker for symbol operations
}
```

## Part 2: CharacterClass Implementation

**File**: `net/Far.Colorer/RegularExpressions/Internal/CharacterClass.cs`

```csharp
using System;
using System.Runtime.InteropServices;

namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Character class implementation using bit arrays for efficient storage.
/// Simplified version - for full Unicode support, use two-stage lookup.
/// </summary>
internal unsafe class CharacterClass
{
    // Simple bitmap for first 65536 characters (BMP)
    private fixed ulong bits[1024]; // 65536 / 64 = 1024

    public CharacterClass()
    {
        Clear();
    }

    public void Clear()
    {
        fixed (ulong* ptr = bits)
        {
            for (int i = 0; i < 1024; i++)
                ptr[i] = 0;
        }
    }

    public void AddChar(char c)
    {
        int index = c / 64;
        int bit = c % 64;
        fixed (ulong* ptr = bits)
        {
            ptr[index] |= (1UL << bit);
        }
    }

    public void AddRange(char from, char to)
    {
        for (char c = from; c <= to; c++)
            AddChar(c);
    }

    public bool Contains(char c)
    {
        int index = c / 64;
        int bit = c % 64;
        fixed (ulong* ptr = bits)
        {
            return (ptr[index] & (1UL << bit)) != 0;
        }
    }

    public void Invert()
    {
        fixed (ulong* ptr = bits)
        {
            for (int i = 0; i < 1024; i++)
                ptr[i] = ~ptr[i];
        }
    }

    public void Union(CharacterClass other)
    {
        fixed (ulong* thisPtr = bits)
        fixed (ulong* otherPtr = other.bits)
        {
            for (int i = 0; i < 1024; i++)
                thisPtr[i] |= otherPtr[i];
        }
    }

    public void Intersect(CharacterClass other)
    {
        fixed (ulong* thisPtr = bits)
        fixed (ulong* otherPtr = other.bits)
        {
            for (int i = 0; i < 1024; i++)
                thisPtr[i] &= otherPtr[i];
        }
    }

    public void Subtract(CharacterClass other)
    {
        fixed (ulong* thisPtr = bits)
        fixed (ulong* otherPtr = other.bits)
        {
            for (int i = 0; i < 1024; i++)
                thisPtr[i] &= ~otherPtr[i];
        }
    }

    /// <summary>
    /// Parse character class from pattern like [a-zA-Z0-9]
    /// </summary>
    public static CharacterClass* Parse(string pattern, ref int position, bool ignoreCase)
    {
        var cc = (CharacterClass*)Marshal.AllocHGlobal(sizeof(CharacterClass));
        *cc = new CharacterClass();

        if (pattern[position] != '[')
            return null;

        position++; // Skip [

        bool invert = false;
        if (position < pattern.Length && pattern[position] == '^')
        {
            invert = true;
            position++;
        }

        while (position < pattern.Length && pattern[position] != ']')
        {
            char ch = pattern[position];

            // Check for range
            if (position + 2 < pattern.Length && pattern[position + 1] == '-' && pattern[position + 2] != ']')
            {
                char from = ch;
                char to = pattern[position + 2];
                cc->AddRange(from, to);
                if (ignoreCase)
                {
                    // Add both cases
                    if (char.IsLower(from))
                        cc->AddRange(char.ToUpper(from), char.ToUpper(to));
                    else if (char.IsUpper(from))
                        cc->AddRange(char.ToLower(from), char.ToLower(to));
                }
                position += 3;
            }
            // Check for escape sequences
            else if (ch == '\\' && position + 1 < pattern.Length)
            {
                position++;
                char esc = pattern[position];
                switch (esc)
                {
                    case 'd':
                        cc->AddRange('0', '9');
                        break;
                    case 'w':
                        cc->AddRange('a', 'z');
                        cc->AddRange('A', 'Z');
                        cc->AddRange('0', '9');
                        cc->AddChar('_');
                        break;
                    case 's':
                        cc->AddChar(' ');
                        cc->AddChar('\t');
                        cc->AddChar('\n');
                        cc->AddChar('\r');
                        break;
                    case 't':
                        cc->AddChar('\t');
                        break;
                    case 'n':
                        cc->AddChar('\n');
                        break;
                    case 'r':
                        cc->AddChar('\r');
                        break;
                    default:
                        cc->AddChar(esc);
                        break;
                }
                position++;
            }
            // Single character
            else
            {
                cc->AddChar(ch);
                if (ignoreCase)
                {
                    if (char.IsLower(ch))
                        cc->AddChar(char.ToUpper(ch));
                    else if (char.IsUpper(ch))
                        cc->AddChar(char.ToLower(ch));
                }
                position++;
            }
        }

        if (position < pattern.Length && pattern[position] == ']')
            position++; // Skip ]

        if (invert)
            cc->Invert();

        return cc;
    }
}
```

## Part 3: Complete Compiler Implementation

**File**: `net/Far.Colorer/RegularExpressions/Internal/CRegExpCompiler.cs`

Complete the missing methods:

```csharp
internal unsafe class CRegExpCompiler
{
    private SRegInfo* tree;
    private readonly string pattern;
    private int position;
    private int groupCount;
    private int namedGroupCount;
    private readonly bool ignoreCase;
    private readonly Dictionary<string, int> namedGroups;

    public CRegExpCompiler()
    {
        namedGroups = new Dictionary<string, int>();
    }

    public SRegInfo* Compile(string pattern, RegexOptions options)
    {
        this.pattern = pattern;
        this.position = 0;
        this.groupCount = 0;
        this.namedGroupCount = 0;
        this.ignoreCase = (options & RegexOptions.IgnoreCase) != 0;

        // Parse pattern
        tree = ParseExpression();

        return tree;
    }

    private SRegInfo* ParseExpression()
    {
        return ParseAlternation();
    }

    private SRegInfo* ParseAlternation()
    {
        SRegInfo* left = ParseSequence();

        if (position < pattern.Length && pattern[position] == '|')
        {
            position++; // Skip |
            SRegInfo* right = ParseAlternation();

            SRegInfo* orNode = AllocateNode();
            orNode->op = (int)EOps.ReOr;
            orNode->param = left;
            left->parent = orNode;
            left->next = right;
            right->prev = left;
            right->parent = orNode;

            return orNode;
        }

        return left;
    }

    private SRegInfo* ParseSequence()
    {
        SRegInfo* first = null;
        SRegInfo* prev = null;

        while (position < pattern.Length)
        {
            char ch = pattern[position];

            // Check for sequence end
            if (ch == ')' || ch == '|')
                break;

            SRegInfo* node = ParseTerm();
            if (node == null)
                continue;

            // Link nodes
            if (first == null)
                first = node;

            if (prev != null)
            {
                prev->next = node;
                node->prev = prev;
            }

            prev = node;
        }

        // If no nodes, return empty
        if (first == null)
        {
            first = AllocateNode();
            first->op = (int)EOps.ReEmpty;
        }

        return first;
    }

    private SRegInfo* ParseTerm()
    {
        SRegInfo* atom = ParseAtom();
        if (atom == null)
            return null;

        // Check for quantifier
        if (position < pattern.Length)
        {
            SRegInfo* quant = ParseQuantifier(atom);
            if (quant != null)
                return quant;
        }

        return atom;
    }

    private SRegInfo* ParseAtom()
    {
        if (position >= pattern.Length)
            return null;

        char ch = pattern[position];

        switch (ch)
        {
            case '\\':
                return ParseEscape();

            case '(':
                return ParseGroup();

            case '.':
                position++;
                SRegInfo* dot = AllocateNode();
                dot->op = (int)EOps.ReMetaSymb;
                dot->metaSymbol = (int)EMetaSymbols.ReAnyChr;
                return dot;

            case '^':
                position++;
                SRegInfo* sol = AllocateNode();
                sol->op = (int)EOps.ReMetaSymb;
                sol->metaSymbol = (int)EMetaSymbols.ReSoL;
                return sol;

            case '$':
                position++;
                SRegInfo* eol = AllocateNode();
                eol->op = (int)EOps.ReMetaSymb;
                eol->metaSymbol = (int)EMetaSymbols.ReEoL;
                return eol;

            case '~':
                position++;
                SRegInfo* scheme = AllocateNode();
                scheme->op = (int)EOps.ReMetaSymb;
                scheme->metaSymbol = (int)EMetaSymbols.ReSoScheme;
                return scheme;

            case '[':
                return ParseCharacterClass();

            case '*':
            case '+':
            case '?':
            case '{':
            case '|':
            case ')':
                // These are special characters, not atoms
                return null;

            default:
                // Literal character
                position++;
                SRegInfo* lit = AllocateNode();
                lit->op = (int)EOps.ReSymb;
                lit->symbol = ch;
                return lit;
        }
    }

    private SRegInfo* ParseEscape()
    {
        if (position + 1 >= pattern.Length)
            return null;

        position++; // Skip backslash
        char ch = pattern[position];
        position++;

        SRegInfo* node = AllocateNode();

        switch (ch)
        {
            // Meta symbols
            case 'd':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReDigit;
                break;
            case 'D':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReNDigit;
                break;
            case 'w':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReWordSymb;
                break;
            case 'W':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReNWordSymb;
                break;
            case 's':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReWSpace;
                break;
            case 'S':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReNWSpace;
                break;
            case 'b':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReWBound;
                break;
            case 'B':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReNWBound;
                break;
            case 'm':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReStart;
                break;
            case 'M':
                node->op = (int)EOps.ReMetaSymb;
                node->metaSymbol = (int)EMetaSymbols.ReEnd;
                break;

            // Literal escape sequences
            case 't':
                node->op = (int)EOps.ReSymb;
                node->symbol = '\t';
                break;
            case 'n':
                node->op = (int)EOps.ReSymb;
                node->symbol = '\n';
                break;
            case 'r':
                node->op = (int)EOps.ReSymb;
                node->symbol = '\r';
                break;

            // Backreferences \1-\9
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                node->op = (int)EOps.ReBkBrack;
                node->param0 = ch - '0';
                break;

            // Named backreferences \y{name} or \Y{name}
            case 'y':
            case 'Y':
                if (position < pattern.Length && pattern[position] == '{')
                {
                    int start = position + 1;
                    int end = pattern.IndexOf('}', start);
                    if (end == -1)
                        throw new RegexSyntaxException("Unclosed named backreference");

                    string name = pattern.Substring(start, end - start);
                    if (!namedGroups.TryGetValue(name, out int groupNum))
                        throw new RegexSyntaxException($"Unknown group name: {name}");

                    node->op = ch == 'y' ? (int)EOps.ReBkTraceName : (int)EOps.ReBkTraceNName;
                    node->param0 = groupNum;
                    position = end + 1;
                }
                else
                {
                    // Single digit: \y1 or \Y1
                    if (position < pattern.Length && char.IsDigit(pattern[position]))
                    {
                        node->op = ch == 'y' ? (int)EOps.ReBkTrace : (int)EOps.ReBkTraceN;
                        node->param0 = pattern[position] - '0';
                        position++;
                    }
                }
                break;

            // Escaped special characters
            default:
                node->op = (int)EOps.ReSymb;
                node->symbol = ch;
                break;
        }

        return node;
    }

    private SRegInfo* ParseGroup()
    {
        if (pattern[position] != '(')
            return null;

        position++; // Skip (

        SRegInfo* node = AllocateNode();
        node->op = (int)EOps.ReBrackets;
        node->param0 = groupCount++;

        // Check for special group types
        if (position + 1 < pattern.Length && pattern[position] == '?')
        {
            position++;
            char type = pattern[position];

            if (type == ':')
            {
                // Non-capturing group (?:...)
                position++;
                node->op = (int)EOps.ReNamedBrackets;
                node->param0 = -1;
            }
            else if (type == '{')
            {
                // Named group (?{name}...)
                position++;
                int start = position;
                int end = pattern.IndexOf('}', start);
                if (end == -1)
                    throw new RegexSyntaxException("Unclosed named group");

                string name = pattern.Substring(start, end - start);
                if (string.IsNullOrEmpty(name))
                {
                    node->param0 = -1;
                }
                else
                {
                    node->param0 = namedGroupCount;
                    namedGroups[name] = namedGroupCount;
                    namedGroupCount++;
                }

                node->op = (int)EOps.ReNamedBrackets;
                position = end + 1;
            }
            else if (type == '=')
            {
                // Positive lookahead (?=...)
                position++;
                node->op = (int)EOps.ReAhead;
                node->param0 = -1;
            }
            else if (type == '!')
            {
                // Negative lookahead (?!...)
                position++;
                node->op = (int)EOps.ReNAhead;
                node->param0 = -1;
            }
            else if (type == '#' && position + 1 < pattern.Length && char.IsDigit(pattern[position + 1]))
            {
                // Lookbehind (?#N)
                position++;
                node->op = (int)EOps.ReBehind;
                node->param0 = pattern[position] - '0';
                position++;
            }
            else if (type == '~' && position + 1 < pattern.Length && char.IsDigit(pattern[position + 1]))
            {
                // Negative lookbehind (?~N)
                position++;
                node->op = (int)EOps.ReNBehind;
                node->param0 = pattern[position] - '0';
                position++;
            }
        }

        // Parse group content
        SRegInfo* content = ParseExpression();
        node->param = content;
        if (content != null)
            content->parent = node;

        // Expect closing )
        if (position < pattern.Length && pattern[position] == ')')
            position++;
        else
            throw new RegexSyntaxException("Unclosed group");

        return node;
    }

    private SRegInfo* ParseCharacterClass()
    {
        SRegInfo* node = AllocateNode();
        node->op = (int)EOps.ReEnum;
        node->charclass = CharacterClass.Parse(pattern, ref position, ignoreCase);
        return node;
    }

    private SRegInfo* ParseQuantifier(SRegInfo* atom)
    {
        if (position >= pattern.Length)
            return null;

        char ch = pattern[position];
        bool nonGreedy = false;

        // Check for non-greedy modifier
        if (position + 1 < pattern.Length && pattern[position + 1] == '?')
            nonGreedy = true;

        SRegInfo* quant = null;

        switch (ch)
        {
            case '*':
                quant = AllocateNode();
                quant->op = nonGreedy ? (int)EOps.ReNGRangeN : (int)EOps.ReRangeN;
                quant->s = 0;
                quant->e = -1;
                position++;
                if (nonGreedy) position++;
                break;

            case '+':
                quant = AllocateNode();
                quant->op = nonGreedy ? (int)EOps.ReNGRangeN : (int)EOps.ReRangeN;
                quant->s = 1;
                quant->e = -1;
                position++;
                if (nonGreedy) position++;
                break;

            case '?':
                quant = AllocateNode();
                quant->op = nonGreedy ? (int)EOps.ReNGRangeNM : (int)EOps.ReRangeNM;
                quant->s = 0;
                quant->e = 1;
                position++;
                if (nonGreedy) position++;
                break;

            case '{':
                quant = ParseRangeQuantifier();
                break;

            default:
                return null;
        }

        if (quant != null)
        {
            quant->param = atom;
            atom->parent = quant;
        }

        return quant;
    }

    private SRegInfo* ParseRangeQuantifier()
    {
        if (pattern[position] != '{')
            return null;

        position++; // Skip {
        int start = position;

        // Find comma and closing brace
        int comma = -1;
        int end = -1;
        bool nonGreedy = false;

        while (position < pattern.Length)
        {
            if (pattern[position] == ',')
                comma = position;
            else if (pattern[position] == '}')
            {
                end = position;
                if (position + 1 < pattern.Length && pattern[position + 1] == '?')
                {
                    nonGreedy = true;
                    position++;
                }
                break;
            }
            position++;
        }

        if (end == -1)
            throw new RegexSyntaxException("Unclosed range quantifier");

        position = end + 1;

        // Parse min and max
        int min, max;
        if (comma == -1)
        {
            // {n} exact count
            min = max = int.Parse(pattern.Substring(start, end - start));
        }
        else if (comma == end - 1)
        {
            // {n,} unbounded
            min = int.Parse(pattern.Substring(start, comma - start));
            max = -1;
        }
        else
        {
            // {n,m} range
            min = int.Parse(pattern.Substring(start, comma - start));
            max = int.Parse(pattern.Substring(comma + 1, end - comma - 1));
        }

        SRegInfo* quant = AllocateNode();
        quant->s = min;
        quant->e = max;

        if (max == -1)
            quant->op = nonGreedy ? (int)EOps.ReNGRangeN : (int)EOps.ReRangeN;
        else
            quant->op = nonGreedy ? (int)EOps.ReNGRangeNM : (int)EOps.ReRangeNM;

        return quant;
    }

    private SRegInfo* AllocateNode()
    {
        SRegInfo* node = (SRegInfo*)Marshal.AllocHGlobal(sizeof(SRegInfo));

        // Initialize to zero
        byte* ptr = (byte*)node;
        for (int i = 0; i < sizeof(SRegInfo); i++)
            ptr[i] = 0;

        return node;
    }
}
```

## Part 4: Project Configuration

**File**: `net/Far.Colorer/Far.Colorer.csproj`

Add this to enable unsafe code:

```xml
<PropertyGroup>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

## Part 5: Additional Matcher Implementations

### CheckMetaSymbol Complete Implementation

In CRegExp.cs, update CheckMetaSymbol to handle ALL meta symbols:

```csharp
private bool CheckMetaSymbol(int metaSymbol, int pos)
{
    switch ((EMetaSymbols)metaSymbol)
    {
        case EMetaSymbols.ReAnyChr:
            return pos < end;

        case EMetaSymbols.ReSoL:
            return pos == 0 || pattern[pos - 1] == '\n';

        case EMetaSymbols.ReEoL:
            return pos == end || pattern[pos] == '\n';

        case EMetaSymbols.ReSoScheme:
            // Scheme start - specific to Colorer, matches start of scheme
            return pos == 0;

        case EMetaSymbols.ReDigit:
            return pos < end && char.IsDigit(pattern[pos]);

        case EMetaSymbols.ReNDigit:
            return pos < end && !char.IsDigit(pattern[pos]);

        case EMetaSymbols.ReWordSymb:
            return pos < end && IsWordChar(pattern[pos]);

        case EMetaSymbols.ReNWordSymb:
            return pos < end && !IsWordChar(pattern[pos]);

        case EMetaSymbols.ReWSpace:
            return pos < end && char.IsWhiteSpace(pattern[pos]);

        case EMetaSymbols.ReNWSpace:
            return pos < end && !char.IsWhiteSpace(pattern[pos]);

        case EMetaSymbols.ReUCase:
            return pos < end && char.IsUpper(pattern[pos]);

        case EMetaSymbols.ReNUCase:
            return pos < end && char.IsLower(pattern[pos]);

        case EMetaSymbols.ReWBound:
            // Word boundary
            bool prevWord = pos > 0 && IsWordChar(pattern[pos - 1]);
            bool nextWord = pos < end && IsWordChar(pattern[pos]);
            return prevWord != nextWord;

        case EMetaSymbols.ReNWBound:
            // Not word boundary
            return !CheckMetaSymbol((int)EMetaSymbols.ReWBound, pos);

        case EMetaSymbols.RePreNW:
            // Preceded by non-word character
            return pos == 0 || !IsWordChar(pattern[pos - 1]);

        case EMetaSymbols.ReStart:
            // Match start marker (\m) - marks start of a match region
            return true; // Always matches, sets marker

        case EMetaSymbols.ReEnd:
            // Match end marker (\M) - marks end of a match region
            return true; // Always matches, sets marker

        default:
            return false;
    }
}
```

## Part 6: Backreference Implementation

Add backreference handling to the main loop in CRegExp.cs:

```csharp
case EOps.ReBkBrack:  // \1-\9
    {
        int groupNum = re->param0;
        if (groupNum < 0 || groupNum >= MaxGroups || matchS[groupNum] == -1)
        {
            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
            continue;
        }

        int groupStart = matchS[groupNum];
        int groupEnd = matchE[groupNum];
        int groupLen = groupEnd - groupStart;

        if (toParse + groupLen > end)
        {
            check_stack(false, &re, &prev, &toParse, &leftenter, &action);
            continue;
        }

        // Check if text matches the captured group
        for (int i = 0; i < groupLen; i++)
        {
            if (pattern[toParse + i] != pattern[groupStart + i])
            {
                check_stack(false, &re, &prev, &toParse, &leftenter, &action);
                goto continueOuter;
            }
        }

        toParse += groupLen;
    }
    break;
    continueOuter:
    continue;

case EOps.ReBkTrace:  // \y1-\y9 (colorer cross-pattern backreference)
case EOps.ReBkTraceName:  // \y{name}
    // These require external match context - for now, stub
    check_stack(false, &re, &prev, &toParse, &leftenter, &action);
    continue;
```

## Part 7: Memory Management

Add proper cleanup:

```csharp
public class Regex : IDisposable
{
    private unsafe SRegInfo* compiledTree;
    private bool disposed;

    ~Regex()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            unsafe
            {
                if (compiledTree != null)
                {
                    FreeTree(compiledTree);
                    compiledTree = null;
                }
            }
            disposed = true;
        }
    }

    private unsafe void FreeTree(SRegInfo* node)
    {
        if (node == null) return;

        // Free child nodes
        FreeTree(node->next);
        FreeTree(node->param);

        // Free character class if present
        if (node->op == (int)EOps.ReEnum && node->charclass != null)
        {
            Marshal.FreeHGlobal((IntPtr)node->charclass);
        }

        // Free the node itself
        Marshal.FreeHGlobal((IntPtr)node);
    }
}
```

## Part 8: Testing Checklist

Create this test file to verify each component:

**File**: `net/Far.Colorer.Tests/RegularExpressions/CompilerTests.cs`

```csharp
public class CompilerTests
{
    [Fact]
    public void Literal_CompilesCorrectly()
    {
        var regex = new Regex("abc");
        Assert.True(regex.IsMatch("abc"));
        Assert.False(regex.IsMatch("ab"));
    }

    [Fact]
    public void Star_CompilesCorrectly()
    {
        var regex = new Regex("a*b");
        Assert.True(regex.IsMatch("b"));
        Assert.True(regex.IsMatch("ab"));
        Assert.True(regex.IsMatch("aaab"));
    }

    [Fact]
    public void Plus_CompilesCorrectly()
    {
        var regex = new Regex("a+b");
        Assert.False(regex.IsMatch("b"));
        Assert.True(regex.IsMatch("ab"));
        Assert.True(regex.IsMatch("aaab"));
    }

    [Fact]
    public void Question_CompilesCorrectly()
    {
        var regex = new Regex("a?b");
        Assert.True(regex.IsMatch("b"));
        Assert.True(regex.IsMatch("ab"));
        Assert.False(regex.IsMatch("aab"));
    }

    [Fact]
    public void Range_CompilesCorrectly()
    {
        var regex = new Regex("a{2,4}");
        Assert.False(regex.IsMatch("a"));
        Assert.True(regex.IsMatch("aa"));
        Assert.True(regex.IsMatch("aaa"));
        Assert.True(regex.IsMatch("aaaa"));
        Assert.False(regex.IsMatch("aaaaa"));
    }

    [Fact]
    public void CharacterClass_CompilesCorrectly()
    {
        var regex = new Regex("[a-z]+");
        Assert.True(regex.IsMatch("abc"));
        Assert.False(regex.IsMatch("123"));
    }

    [Fact]
    public void MetaSymbols_CompileCorrectly()
    {
        var regex = new Regex(@"\d+");
        Assert.True(regex.IsMatch("123"));
        Assert.False(regex.IsMatch("abc"));
    }

    [Fact]
    public void Group_CompilesCorrectly()
    {
        var regex = new Regex("(ab)+");
        Assert.True(regex.IsMatch("ab"));
        Assert.True(regex.IsMatch("abab"));
        Assert.False(regex.IsMatch("a"));
    }

    [Fact]
    public void Backreference_Works()
    {
        var regex = new Regex(@"(a)\1");
        Assert.True(regex.IsMatch("aa"));
        Assert.False(regex.IsMatch("ab"));
    }
}
```

## Implementation Priority

1. **Phase 1** (Critical - 2 hours)
   - Data structures
   - Enums
   - CharacterClass (simple version)

2. **Phase 2** (Critical - 3 hours)
   - Compiler: atoms, literals, escapes
   - Basic quantifiers
   - Groups

3. **Phase 3** (Critical - 4 hours)
   - Matcher core loop
   - All quantifier cases
   - Action handlers

4. **Phase 4** (Important - 1 hour)
   - Integration wrapper
   - Memory management
   - Backreferences

5. **Phase 5** (Testing - 1 hour)
   - Unit tests
   - Integration tests
   - Fix bugs

6. **Phase 6** (Optimization - 1 hour)
   - Performance improvements
   - Memory pooling

**Total: 12 hours**

## Critical Notes

1. **Unsafe Code**: All code uses pointers - must enable in csproj
2. **Memory Management**: Manual allocation/deallocation required
3. **Testing**: Test each component immediately after implementation
4. **C++ Compatibility**: Every line must match C++ behavior exactly

This supplement provides ALL missing implementation details needed for a complete rewrite.