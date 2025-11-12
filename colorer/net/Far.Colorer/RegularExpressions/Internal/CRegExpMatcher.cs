using System;
using System.Runtime.InteropServices;

namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Regular Expression Matcher - executes compiled regex tree against input strings.
/// Port of C++ CRegExp lowParse and related methods from cregexp.cpp lines 670-1398
/// This class performs the actual matching using backtracking with an explicit stack.
/// </summary>
internal unsafe class CRegExpMatcher : IDisposable
{
    private const int INIT_MEM_SIZE = 512;
    private const int MEM_INC = 128;
    private const char BAD_WCHAR = '\uFFFF';

    // Regex tree root from compiler
    private readonly SRegInfo* treeRoot;

    // Input pattern and range
    private string? globalPattern;
    private int end;

    // Match results
    private SMatches* matches;
    private int cMatch;
    private int cnMatch;

    // Options
    private bool ignoreCase;
    private bool singleLine;
    private bool multiLine;
    private bool positionMoves = true; // Default true to search through string

    // COLORERMODE support
    private string? backStr;
    private SMatches* backTrace;
    private int schemeStart;
    private bool startChange;
    private bool endChange;

    // Backtracking stack (static shared across instances like in C++)
    private static StackElem* regExpStack;
    private static int regExpStackSize;
    private int countElem;

    // First character optimization
    private char firstChar = BAD_WCHAR;
    private EMetaSymbols firstMetaChar = EMetaSymbols.ReBadMeta;

    public CRegExpMatcher(SRegInfo* compiledTree, bool ignoreCase = false,
                          bool singleLine = false, bool multiLine = false)
    {
        if (compiledTree == null)
            throw new ArgumentNullException(nameof(compiledTree));

        this.treeRoot = compiledTree;
        this.ignoreCase = ignoreCase;
        this.singleLine = singleLine;
        this.multiLine = multiLine;

        // Allocate matches structure
        matches = (SMatches*)Marshal.AllocHGlobal(sizeof(SMatches));
        matches->Clear();

        // Initialize static stack if needed
        if (regExpStack == null)
        {
            regExpStackSize = INIT_MEM_SIZE;
            regExpStack = (StackElem*)Marshal.AllocHGlobal(sizeof(StackElem) * regExpStackSize);
        }

        // Optimize first character if possible
        OptimizeFirstChar();
    }

    public void Dispose()
    {
        if (matches != null)
        {
            Marshal.FreeHGlobal((IntPtr)matches);
            matches = null;
        }
        GC.SuppressFinalize(this);
    }

    ~CRegExpMatcher()
    {
        Dispose();
    }

    public static void ClearRegExpStack()
    {
        if (regExpStack != null)
        {
            Marshal.FreeHGlobal((IntPtr)regExpStack);
            regExpStack = null;
            regExpStackSize = 0;
        }
    }

    /// <summary>
    /// Match the pattern against the input string.
    /// Port of parse() method from cregexp.cpp lines 1428-1451
    /// </summary>
    public bool Parse(string str, int pos, int eol, int soscheme = 0, bool? posMovesOverride = null)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));

        globalPattern = str;
        end = Math.Min(eol, str.Length);
        schemeStart = soscheme;
        startChange = false;
        endChange = false;

        // Allow override of position moves
        bool originalPosMoves = positionMoves;
        if (posMovesOverride.HasValue)
            positionMoves = posMovesOverride.Value;

        bool result = ParseRE(pos);

        positionMoves = originalPosMoves;
        return result;
    }

    /// <summary>
    /// Internal parsing method with position search loop.
    /// Port of parseRE() method from cregexp.cpp lines 1400-1426
    /// </summary>
    private bool ParseRE(int pos)
    {
        int toParse = pos;

        // Quick check optimization (skip if positionMoves is enabled)
        if (!positionMoves && !QuickCheck(toParse))
            return false;

        // Clear matches
        int* sArr = matches->s;
        int* eArr = matches->e;
        int* nsArr = matches->ns;
        int* neArr = matches->ne;

        for (int i = 0; i < 10; i++)
        {
            sArr[i] = -1;
            eArr[i] = -1;
            nsArr[i] = -1;
            neArr[i] = -1;
        }
        matches->cMatch = cMatch = 10; // MATCHES_NUM
        matches->cnMatch = cnMatch = 10; // NAMED_MATCHES_NUM

        // Try matching at different positions
        do
        {
            countElem = 0; // Reset backtracking stack for each attempt

            if (LowParse(treeRoot, null, toParse))
                return true;

            // If positionMoves is false, only try the starting position
            if (!positionMoves)
                return false;

            // Try next position
            toParse = ++pos;
        } while (toParse <= end);

        return false;
    }

    /// <summary>
    /// Set backreference support for COLORERMODE (\y, \Y operators)
    /// </summary>
    public void SetBackTrace(string? str, SMatches* trace)
    {
        backStr = str;
        backTrace = trace;
    }

    /// <summary>
    /// Core matching engine with backtracking.
    /// Port of lowParse() from cregexp.cpp lines 844-1348
    /// </summary>
    private bool LowParse(SRegInfo* re, SRegInfo* prev, int toParse)
    {
        bool leftenter = true;
        bool br = false;
        int action = -1;
        int i, sv;

        if (re == null)
        {
            re = prev->parent;
            leftenter = false;
        }

        while (true)
        {
            while (re != null || action != -1)
            {
                if (re != null && action == -1)
                {
                    switch (re->op)
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

                            if (re->op == EOps.ReBrackets)
                            {
                                int idx = re->param0;
                                if (idx >= 0 && idx < 10)
                                {
                                    int* sArr = matches->s;
                                    int* eArr = matches->e;
                                    if (idx > 0 || !startChange)
                                        sArr[idx] = re->s;
                                    if (idx > 0 || !endChange)
                                        eArr[idx] = toParse;
                                    if (eArr[idx] < sArr[idx])
                                        sArr[idx] = eArr[idx];
                                }
                            }
                            else // ReNamedBrackets
                            {
                                int idx = re->param0;
                                if (idx >= 0 && idx < 10)
                                {
                                    int* nsArr = matches->ns;
                                    int* neArr = matches->ne;
                                    nsArr[idx] = re->s;
                                    neArr[idx] = toParse;
                                    if (neArr[idx] < nsArr[idx])
                                        nsArr[idx] = neArr[idx];
                                }
                            }
                            break;

                        case EOps.ReSymb:
                            if (toParse >= end)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            if (ignoreCase)
                            {
                                if (Character.ToLowerCase(globalPattern![toParse]) != Character.ToLowerCase(re->symbol) &&
                                    Character.ToUpperCase(globalPattern[toParse]) != Character.ToUpperCase(re->symbol))
                                {
                                    CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                    continue;
                                }
                            }
                            else if (globalPattern![toParse] != re->symbol)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            toParse++;
                            break;

                        case EOps.ReMetaSymb:
                            if (!CheckMetaSymbol(re->metaSymbol, ref toParse))
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            break;

                        case EOps.ReEnum:
                            if (toParse >= end)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            if (!re->charclass->Contains(globalPattern![toParse]))
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            toParse++;
                            break;

                        case EOps.ReNEnum:
                            if (toParse >= end)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            if (re->charclass->Contains(globalPattern![toParse]))
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            toParse++;
                            break;

                        // COLORERMODE backreferences
                        case EOps.ReBkTrace:
                            sv = re->param0;
                            if (backStr == null || backTrace == null || sv == -1)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            br = false;
                            int* bkSArr = backTrace->s;
                            int* bkEArr = backTrace->e;
                            for (i = bkSArr[sv]; i < bkEArr[sv]; i++)
                            {
                                if (toParse >= end || globalPattern![toParse] != backStr[i])
                                {
                                    CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                    br = true;
                                    break;
                                }
                                toParse++;
                            }
                            if (br)
                                continue;
                            break;

                        case EOps.ReBkTraceN:
                            sv = re->param0;
                            if (backStr == null || backTrace == null || sv == -1)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            br = false;
                            bkSArr = backTrace->s;
                            bkEArr = backTrace->e;
                            for (i = bkSArr[sv]; i < bkEArr[sv]; i++)
                            {
                                if (toParse >= end ||
                                    Character.ToLowerCase(globalPattern![toParse]) != Character.ToLowerCase(backStr[i]))
                                {
                                    CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                    br = true;
                                    break;
                                }
                                toParse++;
                            }
                            if (br)
                                continue;
                            break;

                        case EOps.ReBkTraceName:
                            sv = re->param0;
                            if (backStr == null || backTrace == null || sv == -1)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            br = false;
                            int* bkNSArr = backTrace->ns;
                            int* bkNEArr = backTrace->ne;
                            for (i = bkNSArr[sv]; i < bkNEArr[sv]; i++)
                            {
                                if (toParse >= end || globalPattern![toParse] != backStr[i])
                                {
                                    CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                    br = true;
                                    break;
                                }
                                toParse++;
                            }
                            if (br)
                                continue;
                            break;

                        case EOps.ReBkTraceNName:
                            sv = re->param0;
                            if (backStr == null || backTrace == null || sv == -1)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            br = false;
                            bkNSArr = backTrace->ns;
                            bkNEArr = backTrace->ne;
                            for (i = bkNSArr[sv]; i < bkNEArr[sv]; i++)
                            {
                                if (toParse >= end ||
                                    Character.ToLowerCase(globalPattern![toParse]) != Character.ToLowerCase(backStr[i]))
                                {
                                    CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                    br = true;
                                    break;
                                }
                                toParse++;
                            }
                            if (br)
                                continue;
                            break;

                        // Standard backreferences
                        case EOps.ReBkBrack:
                            sv = re->param0;
                            if (sv == -1 || cMatch <= sv)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            int* matchSArr = matches->s;
                            int* matchEArr = matches->e;
                            if (matchSArr[sv] == -1 || matchEArr[sv] == -1)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            br = false;
                            for (i = matchSArr[sv]; i < matchEArr[sv]; i++)
                            {
                                if (toParse >= end || globalPattern![toParse] != globalPattern[i])
                                {
                                    CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                    br = true;
                                    break;
                                }
                                toParse++;
                            }
                            if (br)
                                continue;
                            break;

                        case EOps.ReBkBrackName:
                            sv = re->param0;
                            if (sv == -1 || cnMatch <= sv)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            int* matchNSArr = matches->ns;
                            int* matchNEArr = matches->ne;
                            if (matchNSArr[sv] == -1 || matchNEArr[sv] == -1)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            br = false;
                            for (i = matchNSArr[sv]; i < matchNEArr[sv]; i++)
                            {
                                if (toParse >= end || globalPattern![toParse] != globalPattern[i])
                                {
                                    CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                    br = true;
                                    break;
                                }
                                toParse++;
                            }
                            if (br)
                                continue;
                            break;

                        // Lookahead/lookbehind
                        case EOps.ReAhead:
                            if (!leftenter)
                            {
                                CheckStack(true, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                       ReAction.rea_Break, ReAction.rea_False,
                                       re->param, null, toParse);
                            continue;

                        case EOps.ReNAhead:
                            if (!leftenter)
                            {
                                CheckStack(true, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                       ReAction.rea_False, ReAction.rea_Break,
                                       re->param, null, toParse);
                            continue;

                        case EOps.ReBehind:
                            if (!leftenter)
                            {
                                CheckStack(true, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            if (toParse - re->param0 < 0)
                            {
                                CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                       ReAction.rea_Break, ReAction.rea_False,
                                       re->param, null, toParse - re->param0);
                            continue;

                        case EOps.ReNBehind:
                            if (!leftenter)
                            {
                                CheckStack(true, ref re, ref prev, ref toParse, ref leftenter, ref action);
                                continue;
                            }
                            if (toParse - re->param0 >= 0)
                            {
                                InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                           ReAction.rea_False, ReAction.rea_Break,
                                           re->param, null, toParse - re->param0);
                                continue;
                            }
                            break;

                        // Alternation
                        case EOps.ReOr:
                            if (!leftenter)
                            {
                                while (re->next != null)
                                    re = re->next;
                                break;
                            }
                            InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                       ReAction.rea_True, ReAction.rea_Break,
                                       re->param, null, toParse);
                            continue;

                        // Quantifiers - greedy
                        case EOps.ReRangeN:
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
                                InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                           ReAction.rea_True, ReAction.rea_RangeN_step2,
                                           re->param, null, toParse);
                                continue;
                            }
                            else
                            {
                                re->param0--;
                            }
                            re = re->param;
                            leftenter = true;
                            continue;

                        case EOps.ReRangeNM:
                            if (leftenter)
                            {
                                re->param0 = re->s;
                                re->param1 = re->e - re->s;
                                re->oldParse = -1;
                            }
                            if (re->param0 == 0)
                            {
                                if (re->param1 > 0)
                                    re->param1--;
                                else
                                {
                                    InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                               ReAction.rea_True, ReAction.rea_False,
                                               re->next, re, toParse);
                                    continue;
                                }
                                InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                           ReAction.rea_True, ReAction.rea_RangeNM_step2,
                                           re->param, null, toParse);
                                continue;
                            }
                            else
                            {
                                re->param0--;
                            }
                            re = re->param;
                            leftenter = true;
                            continue;

                        // Quantifiers - non-greedy
                        case EOps.ReNGRangeN:
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
                                InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                           ReAction.rea_True, ReAction.rea_NGRangeN_step2,
                                           re->next, re, toParse);
                                continue;
                            }
                            else
                            {
                                re->param0--;
                            }
                            re = re->param;
                            leftenter = true;
                            continue;

                        case EOps.ReNGRangeNM:
                            if (leftenter)
                            {
                                re->param0 = re->s;
                                re->param1 = re->e - re->s;
                                re->oldParse = -1;
                            }
                            if (re->param0 == 0)
                            {
                                if (re->param1 > 0)
                                    re->param1--;
                                else
                                {
                                    InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                               ReAction.rea_True, ReAction.rea_False,
                                               re->next, re, toParse);
                                    continue;
                                }
                                InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                           ReAction.rea_True, ReAction.rea_NGRangeNM_step2,
                                           re->next, re, toParse);
                                continue;
                            }
                            else
                            {
                                re->param0--;
                            }
                            re = re->param;
                            leftenter = true;
                            continue;

                        default:
                            break;
                    }
                }

                // Action processing
                switch ((ReAction)action)
                {
                    case ReAction.rea_False:
                        if (countElem > 0)
                        {
                            CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                            continue;
                        }
                        return false;

                    case ReAction.rea_True:
                        if (countElem > 0)
                        {
                            CheckStack(true, ref re, ref prev, ref toParse, ref leftenter, ref action);
                            continue;
                        }
                        return true;

                    case ReAction.rea_Break:
                        action = -1;
                        break;

                    case ReAction.rea_RangeN_step2:
                        action = -1;
                        InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                   ReAction.rea_True, ReAction.rea_False,
                                   re->next, re, toParse);
                        continue;

                    case ReAction.rea_RangeNM_step2:
                        action = -1;
                        InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                   ReAction.rea_True, ReAction.rea_RangeNM_step3,
                                   re->next, re, toParse);
                        continue;

                    case ReAction.rea_RangeNM_step3:
                        action = -1;
                        re->param1++;
                        CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
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
                        InsertStack(ref re, ref prev, ref toParse, ref leftenter,
                                   ReAction.rea_True, ReAction.rea_NGRangeNM_step3,
                                   re->param, null, toParse);
                        continue;

                    case ReAction.rea_NGRangeNM_step3:
                        action = -1;
                        re->param1++;
                        CheckStack(false, ref re, ref prev, ref toParse, ref leftenter, ref action);
                        continue;
                }

                // Move to next node
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

            CheckStack(true, ref re, ref prev, ref toParse, ref leftenter, ref action);
        }
    }

    /// <summary>
    /// Check metacharacter match.
    /// Port of checkMetaSymbol() from cregexp.cpp lines 686-786
    /// </summary>
    private bool CheckMetaSymbol(EMetaSymbols symb, ref int toParse)
    {
        switch (symb)
        {
            case EMetaSymbols.ReAnyChr:
                if (toParse >= end)
                    return false;
                if (!singleLine && IsLineTerminator(globalPattern![toParse]))
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReSoL:
                if (multiLine)
                {
                    if (toParse > 0 && IsLineTerminator(globalPattern![toParse - 1]))
                        return true;
                }
                return (toParse == 0);

            case EMetaSymbols.ReEoL:
                if (multiLine)
                {
                    if (toParse > 0 && toParse < end && IsLineTerminator(globalPattern![toParse - 1]))
                        return true;
                }
                return (end == toParse);

            case EMetaSymbols.ReDigit:
                if (toParse >= end || !Character.IsDigit(globalPattern![toParse]))
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReNDigit:
                if (toParse >= end || Character.IsDigit(globalPattern![toParse]))
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReWordSymb:
                if (toParse >= end || !(Character.IsLetterOrDigit(globalPattern![toParse]) || globalPattern[toParse] == '_'))
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReNWordSymb:
                if (toParse >= end || Character.IsLetterOrDigit(globalPattern![toParse]) || globalPattern![toParse] == '_')
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReWSpace:
                if (toParse >= end || !Character.IsWhitespace(globalPattern![toParse]))
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReNWSpace:
                if (toParse >= end || Character.IsWhitespace(globalPattern![toParse]))
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReUCase:
                if (toParse >= end || !Character.IsUpperCase(globalPattern![toParse]))
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReNUCase:
                if (toParse >= end || !Character.IsLowerCase(globalPattern![toParse]))
                    return false;
                toParse++;
                return true;

            case EMetaSymbols.ReWBound:
                return IsWordBoundary(toParse);

            case EMetaSymbols.ReNWBound:
                return IsNWordBoundary(toParse);

            case EMetaSymbols.RePreNW:
                if (toParse >= end)
                    return true;
                return toParse == 0 || !Character.IsLetter(globalPattern![toParse - 1]);

            // COLORERMODE symbols
            case EMetaSymbols.ReSoScheme:
                return (schemeStart == toParse);

            case EMetaSymbols.ReStart:
                int* sArr = matches->s;
                sArr[0] = toParse;
                startChange = true;
                return true;

            case EMetaSymbols.ReEnd:
                int* eArr = matches->e;
                eArr[0] = toParse;
                endChange = true;
                return true;

            default:
                return false;
        }
    }

    private bool IsLineTerminator(char c)
    {
        return c == 0x0A || c == 0x0B || c == 0x0C ||
               c == 0x0D || c == 0x85 || c == 0x2028 || c == 0x2029;
    }

    private bool IsWordBoundary(int toParse)
    {
        int before = 0;
        int after = 0;
        if (toParse < end && (Character.IsLetterOrDigit(globalPattern![toParse]) || globalPattern[toParse] == '_'))
            after = 1;
        if (toParse > 0 && (Character.IsLetterOrDigit(globalPattern![toParse - 1]) || globalPattern[toParse - 1] == '_'))
            before = 1;
        return before + after == 1;
    }

    private bool IsNWordBoundary(int toParse)
    {
        return !IsWordBoundary(toParse);
    }

    /// <summary>
    /// Pop from backtracking stack.
    /// Port of check_stack() from cregexp.cpp lines 788-806
    /// </summary>
    private void CheckStack(bool res, ref SRegInfo* re, ref SRegInfo* prev,
                           ref int toParse, ref bool leftenter, ref int action)
    {
        if (countElem == 0)
        {
            action = res ? 1 : 0;
            return;
        }

        StackElem* ne = &regExpStack[--countElem];
        action = res ? ne->ifTrueReturn : ne->ifFalseReturn;
        re = ne->re;
        prev = ne->prev;
        toParse = ne->toParse;
        leftenter = ne->leftenter;
    }

    /// <summary>
    /// Push onto backtracking stack.
    /// Port of insert_stack() from cregexp.cpp lines 808-842
    /// </summary>
    private void InsertStack(ref SRegInfo* re, ref SRegInfo* prev, ref int toParse,
                            ref bool leftenter, ReAction ifTrueReturn, ReAction ifFalseReturn,
                            SRegInfo* re2, SRegInfo* prev2, int toParse2)
    {
        // Expand stack if needed
        if (regExpStackSize == countElem)
        {
            regExpStackSize += MEM_INC;
            StackElem* newStack = (StackElem*)Marshal.AllocHGlobal(sizeof(StackElem) * regExpStackSize);

            // Copy old data
            for (int i = 0; i < countElem; i++)
            {
                newStack[i] = regExpStack[i];
            }

            Marshal.FreeHGlobal((IntPtr)regExpStack);
            regExpStack = newStack;
        }

        StackElem* ne = &regExpStack[countElem++];
        ne->re = re;
        ne->prev = prev;
        ne->toParse = toParse;
        ne->ifTrueReturn = (int)ifTrueReturn;
        ne->ifFalseReturn = (int)ifFalseReturn;
        ne->leftenter = leftenter;

        if (prev2 == null)
            prev = null;
        else
            prev = prev2;
        re = re2;
        toParse = toParse2;

        // Init operation from lowParse
        leftenter = true;
        if (re == null && prev != null)
        {
            re = prev->parent;
            leftenter = false;
        }
    }

    /// <summary>
    /// Quick check for first character optimization.
    /// Port of quickCheck() from cregexp.cpp lines 1350-1398
    /// </summary>
    private bool QuickCheck(int toParse)
    {
        if (firstChar != BAD_WCHAR)
        {
            if (toParse >= end)
                return false;
            if (ignoreCase)
            {
                if (Character.ToLowerCase(globalPattern![toParse]) != Character.ToLowerCase(firstChar))
                    return false;
            }
            else if (globalPattern![toParse] != firstChar)
            {
                return false;
            }
            return true;
        }

        if (firstMetaChar != EMetaSymbols.ReBadMeta)
        {
            switch (firstMetaChar)
            {
                case EMetaSymbols.ReSoL:
                    return toParse == 0;
                case EMetaSymbols.ReSoScheme:
                    return toParse == schemeStart;
                default:
                    break;
            }
        }
        return true;
    }

    /// <summary>
    /// Optimize first character for quick rejection.
    /// Analyzes the tree root to extract first possible character/metachar.
    /// </summary>
    private void OptimizeFirstChar()
    {
        if (treeRoot == null || treeRoot->param == null)
            return;

        SRegInfo* first = treeRoot->param;

        if (first->op == EOps.ReSymb)
        {
            firstChar = first->symbol;
        }
        else if (first->op == EOps.ReMetaSymb)
        {
            firstMetaChar = first->metaSymbol;
        }
    }

    /// <summary>
    /// Get match results after successful parse.
    /// </summary>
    public void GetMatches(out int start, out int end)
    {
        int* sArr = matches->s;
        int* eArr = matches->e;
        start = sArr[0];
        end = eArr[0];
    }

    /// <summary>
    /// Get specific capture group result.
    /// </summary>
    public void GetCapture(int index, out int start, out int end)
    {
        if (index < 0 || index >= 10)
        {
            start = -1;
            end = -1;
            return;
        }

        int* sArr = matches->s;
        int* eArr = matches->e;
        start = sArr[index];
        end = eArr[index];
    }

    /// <summary>
    /// Get named capture group result.
    /// </summary>
    public void GetNamedCapture(int index, out int start, out int end)
    {
        if (index < 0 || index >= 10)
        {
            start = -1;
            end = -1;
            return;
        }

        int* nsArr = matches->ns;
        int* neArr = matches->ne;
        start = nsArr[index];
        end = neArr[index];
    }
}
