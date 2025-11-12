using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Far.Colorer.RegularExpressions.Enums;

namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Regular expression compiler - ports C++ CRegExp compilation methods
/// Converts regex pattern string into SRegInfo tree structure
/// Based on cregexp.cpp setStructs() and related methods
/// </summary>
internal unsafe class CRegExpCompiler : IDisposable
{
    private readonly string pattern;
    private int position;
    private int groupCount;
    private readonly bool ignoreCase;
    private readonly bool extend;
    private readonly bool multiLine;
    private readonly bool singleLine;
    private readonly Dictionary<string, int> namedGroups;
    private readonly List<IntPtr> allocatedNodes;  // Track for cleanup

    public int TotalGroups => groupCount;

    public CRegExpCompiler(string pattern, RegexOptions options)
    {
        this.pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        this.position = 0;
        this.groupCount = 0;
        this.ignoreCase = (options & RegexOptions.IgnoreCase) != 0;
        this.extend = (options & RegexOptions.Extended) != 0;
        this.multiLine = (options & RegexOptions.Multiline) != 0;
        this.singleLine = (options & RegexOptions.Singleline) != 0;
        this.namedGroups = new Dictionary<string, int>();
        this.allocatedNodes = new List<IntPtr>();
    }

    /// <summary>
    /// Main compilation entry point
    /// </summary>
    public SRegInfo* Compile()
    {
        // Create root bracket node (like C++ tree_root)
        SRegInfo* root = AllocateNode();
        root->op = EOps.ReBrackets;
        root->param0 = groupCount++;
        root->param = AllocateNode();
        root->param->parent = root;

        // Parse expression (empty pattern is valid - matches at any position)
        if (!string.IsNullOrEmpty(pattern))
        {
            position = 0;
            ParseSequence(root->param);

            // Post-process alternation (ReOr) nodes
            // Based on C++ cregexp.cpp lines 590-661
            // May return different start node if ReOr becomes root
            root->param = ProcessAlternation(root->param);
        }
        else
        {
            // Empty pattern: create an empty node
            root->param->op = EOps.ReEmpty;
        }

        return root;
    }

    /// <summary>
    /// Parse a sequence of regex elements (port of setStructs main loop)
    /// </summary>
    private void ParseSequence(SRegInfo* start)
    {
        SRegInfo* current = start;

        while (position < pattern.Length)
        {
            char ch = pattern[position];

            // Skip whitespace in extended mode
            if (extend && char.IsWhiteSpace(ch))
            {
                position++;
                continue;
            }

            // End of group
            if (ch == ')')
            {
                break;
            }

            // Alternation - just create ReOr node inline
            // Post-processing will reorganize the tree properly
            // Port of C++ line 417-420
            if (ch == '|')
            {
                position++;
                // Create next node for ReOr (will be processed later)
                if (current->op != EOps.ReEmpty)
                {
                    current->next = AllocateNode();
                    current->next->parent = current->parent;
                    current->next->prev = current;
                    current = current->next;
                }
                current->op = EOps.ReOr;
                continue;
            }

            // Create next node if not first
            if (current->op != EOps.ReEmpty)
            {
                current->next = AllocateNode();
                current->next->parent = current->parent;
                current->next->prev = current;
                current = current->next;
            }

            // Parse atom
            ParseAtom(current);

            // Check for quantifier
            if (position < pattern.Length)
            {
                ParseQuantifier(current);
            }
        }
    }

    /// <summary>
    /// Parse a single atom (character, escape, group, etc.)
    /// Ports C++ setStructs() escape and character handling
    /// </summary>
    private void ParseAtom(SRegInfo* node)
    {
        if (position >= pattern.Length)
            return;

        char ch = pattern[position];

        // Escape sequences
        if (ch == '\\')
        {
            ParseEscape(node);
            return;
        }

        // Groups
        if (ch == '(')
        {
            ParseGroup(node);
            return;
        }

        // Character classes
        if (ch == '[')
        {
            ParseCharacterClass(node);
            return;
        }

        // Metacharacters
        switch (ch)
        {
            case '.':
                position++;
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReAnyChr;
                break;

            case '^':
                position++;
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = multiLine ? EMetaSymbols.ReSoL : EMetaSymbols.ReSoScheme;
                break;

            case '$':
                position++;
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReEoL;
                break;

            case '~':  // COLORERMODE scheme start
                position++;
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReSoScheme;
                break;

            // These are not atoms, handle as errors or empty
            case '*':
            case '+':
            case '?':
            case '{':
            case '|':
            case ')':
                node->op = EOps.ReEmpty;
                break;

            default:
                // Literal character
                position++;
                node->op = EOps.ReSymb;
                node->symbol = ch;
                break;
        }
    }

    /// <summary>
    /// Parse escape sequences - ports C++ escape handling from setStructs
    /// </summary>
    private void ParseEscape(SRegInfo* node)
    {
        if (position + 1 >= pattern.Length)
            throw new RegexSyntaxException("Incomplete escape sequence");

        position++; // Skip backslash
        char ch = pattern[position];
        position++;

        switch (ch)
        {
            case 'd':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReDigit;
                break;
            case 'D':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReNDigit;
                break;
            case 'w':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReWordSymb;
                break;
            case 'W':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReNWordSymb;
                break;
            case 's':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReWSpace;
                break;
            case 'S':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReNWSpace;
                break;
            case 'u':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReUCase;
                break;
            case 'l':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReNUCase;
                break;
            case 'b':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReWBound;
                break;
            case 'B':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReNWBound;
                break;
            case 'c':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.RePreNW;
                break;

            // COLORERMODE metacharacters
            case 'm':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReStart;
                break;
            case 'M':
                node->op = EOps.ReMetaSymb;
                node->metaSymbol = EMetaSymbols.ReEnd;
                break;

            // Literal escapes
            case 't':
                node->op = EOps.ReSymb;
                node->symbol = '\t';
                break;
            case 'n':
                node->op = EOps.ReSymb;
                node->symbol = '\n';
                break;
            case 'r':
                node->op = EOps.ReSymb;
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
                node->op = EOps.ReBkBrack;
                node->param0 = ch - '0';
                break;

            // COLORERMODE cross-pattern backreferences
            case 'y':
            case 'Y':
                ParseBackreference(node, ch == 'y');
                break;

            // Named backreference \p{name}
            case 'p':
                ParseNamedBackreference(node);
                break;

            default:
                // Escaped literal
                node->op = EOps.ReSymb;
                node->symbol = ch;
                break;
        }
    }

    /// <summary>
    /// Parse \y or \Y backreferences (COLORERMODE feature)
    /// </summary>
    private void ParseBackreference(SRegInfo* node, bool positive)
    {
        if (position < pattern.Length && pattern[position] == '{')
        {
            // Named: \y{name} or \Y{name}
            int start = ++position;
            while (position < pattern.Length && pattern[position] != '}')
                position++;

            if (position >= pattern.Length)
                throw new RegexSyntaxException("Unclosed named backreference");

            string name = pattern.Substring(start, position - start);
            position++; // Skip }

            if (!namedGroups.TryGetValue(name, out int groupNum))
                throw new RegexSyntaxException($"Unknown group name: {name}");

            node->op = positive ? EOps.ReBkTraceName : EOps.ReBkTraceNName;
            node->param0 = groupNum;
        }
        else if (position < pattern.Length && char.IsDigit(pattern[position]))
        {
            // Numeric: \y1 or \Y1
            node->op = positive ? EOps.ReBkTrace : EOps.ReBkTraceN;
            node->param0 = pattern[position] - '0';
            position++;
        }
        else
        {
            throw new RegexSyntaxException("Invalid backreference");
        }
    }

    /// <summary>
    /// Parse \p{name} named backreference
    /// </summary>
    private void ParseNamedBackreference(SRegInfo* node)
    {
        if (position >= pattern.Length || pattern[position] != '{')
            throw new RegexSyntaxException("Expected { after \\p");

        int start = ++position;
        while (position < pattern.Length && pattern[position] != '}')
            position++;

        if (position >= pattern.Length)
            throw new RegexSyntaxException("Unclosed named backreference");

        string name = pattern.Substring(start, position - start);
        position++; // Skip }

        if (!namedGroups.TryGetValue(name, out int groupNum))
            throw new RegexSyntaxException($"Unknown group name: {name}");

        node->op = EOps.ReBkBrackName;
        node->param0 = groupNum;
    }

    /// <summary>
    /// Parse groups: (...), (?:...), (?{name}...), (?=...), (?!...), etc.
    /// Ports C++ bracket handling
    /// </summary>
    private void ParseGroup(SRegInfo* node)
    {
        if (pattern[position] != '(')
            throw new RegexSyntaxException("Expected (");

        position++; // Skip (

        node->op = EOps.ReBrackets;
        node->param0 = groupCount++;

        // Check for special group types
        if (position < pattern.Length && pattern[position] == '?')
        {
            position++;
            if (position >= pattern.Length)
                throw new RegexSyntaxException("Incomplete group");

            char type = pattern[position];
            position++;

            switch (type)
            {
                case ':':
                    // Non-capturing group
                    node->op = EOps.ReNamedBrackets;
                    node->param0 = -1;
                    groupCount--; // Don't count this
                    break;

                case '{':
                    // Named group (?{name}...)
                    int start = position;
                    while (position < pattern.Length && pattern[position] != '}')
                        position++;

                    if (position >= pattern.Length)
                        throw new RegexSyntaxException("Unclosed named group");

                    string name = pattern.Substring(start, position - start);
                    position++; // Skip }

                    if (string.IsNullOrEmpty(name))
                    {
                        node->op = EOps.ReNamedBrackets;
                        node->param0 = -1;
                        groupCount--;
                    }
                    else
                    {
                        node->op = EOps.ReNamedBrackets;
                        namedGroups[name] = node->param0;
                    }
                    break;

                case '=':
                    // Positive lookahead
                    node->op = EOps.ReAhead;
                    node->param0 = -1;
                    groupCount--;
                    break;

                case '!':
                    // Negative lookahead
                    node->op = EOps.ReNAhead;
                    node->param0 = -1;
                    groupCount--;
                    break;

                case '#':
                    // Lookbehind (?#N)
                    if (position < pattern.Length && char.IsDigit(pattern[position]))
                    {
                        node->op = EOps.ReBehind;
                        node->param0 = pattern[position] - '0';
                        position++;
                        groupCount--;
                    }
                    else
                        throw new RegexSyntaxException("Expected digit after (?#");
                    break;

                case '~':
                    // Negative lookbehind (?~N)
                    if (position < pattern.Length && char.IsDigit(pattern[position]))
                    {
                        node->op = EOps.ReNBehind;
                        node->param0 = pattern[position] - '0';
                        position++;
                        groupCount--;
                    }
                    else
                        throw new RegexSyntaxException("Expected digit after (?~");
                    break;

                default:
                    throw new RegexSyntaxException($"Unknown group type: ?{type}");
            }
        }

        // Parse group contents
        node->param = AllocateNode();
        node->param->parent = node;
        ParseSequence(node->param);

        // Expect closing )
        if (position >= pattern.Length || pattern[position] != ')')
            throw new RegexSyntaxException("Unclosed group");

        position++; // Skip )
    }

    /// <summary>
    /// Parse character class [...] or [^...]
    /// Port of C++ character class parsing from cregexp.cpp
    /// </summary>
    private void ParseCharacterClass(SRegInfo* node)
    {
        node->op = EOps.ReEnum;
        node->charclass = CreateSimpleCharClass();

        position++; // Skip [

        // Check for negation
        bool negated = false;
        if (position < pattern.Length && pattern[position] == '^')
        {
            position++;
            negated = true;
        }

        // Parse character class content
        while (position < pattern.Length && pattern[position] != ']')
        {
            char start = pattern[position];

            // Handle escape sequences in character class
            if (start == '\\' && position + 1 < pattern.Length)
            {
                position++;
                char escaped = pattern[position];

                // Handle standard escapes
                switch (escaped)
                {
                    case 'd': // digits [0-9]
                        for (char c = '0'; c <= '9'; c++)
                            node->charclass->AddChar(c);
                        break;
                    case 'w': // word chars [a-zA-Z0-9_]
                        for (char c = 'a'; c <= 'z'; c++)
                            node->charclass->AddChar(c);
                        for (char c = 'A'; c <= 'Z'; c++)
                            node->charclass->AddChar(c);
                        for (char c = '0'; c <= '9'; c++)
                            node->charclass->AddChar(c);
                        node->charclass->AddChar('_');
                        break;
                    case 's': // whitespace
                        node->charclass->AddChar(' ');
                        node->charclass->AddChar('\t');
                        node->charclass->AddChar('\n');
                        node->charclass->AddChar('\r');
                        node->charclass->AddChar('\f');
                        break;
                    case 'n':
                        node->charclass->AddChar('\n');
                        break;
                    case 't':
                        node->charclass->AddChar('\t');
                        break;
                    case 'r':
                        node->charclass->AddChar('\r');
                        break;
                    default:
                        // Literal escaped character
                        node->charclass->AddChar(escaped);
                        break;
                }
                position++;
            }
            else
            {
                position++;

                // Check for range [a-z]
                if (position < pattern.Length && pattern[position] == '-' &&
                    position + 1 < pattern.Length && pattern[position + 1] != ']')
                {
                    position++; // Skip -
                    char end = pattern[position];

                    // Handle escape in range end
                    if (end == '\\' && position + 1 < pattern.Length)
                    {
                        position++;
                        end = pattern[position];
                    }

                    // Add range
                    node->charclass->AddRange(start, end);
                    position++;
                }
                else
                {
                    // Single character
                    node->charclass->AddChar(start);
                }
            }
        }

        if (position < pattern.Length)
            position++; // Skip ]

        // Apply negation if needed
        // Note: For negated classes, we keep the character set as-is
        // The matcher will check "character NOT in set" for ReNEnum
        if (negated)
        {
            node->op = EOps.ReNEnum;
        }
    }

    /// <summary>
    /// Parse quantifiers: *, +, ?, {n}, {n,}, {n,m}
    /// Ports C++ quantifier handling
    /// </summary>
    private void ParseQuantifier(SRegInfo* node)
    {
        if (position >= pattern.Length)
            return;

        char ch = pattern[position];
        bool nonGreedy = false;

        // Check for non-greedy modifier
        if (position + 1 < pattern.Length && pattern[position + 1] == '?')
            nonGreedy = true;

        EOps quantOp;
        int min = 0, max = -1;

        switch (ch)
        {
            case '*':
                quantOp = nonGreedy ? EOps.ReNGRangeN : EOps.ReRangeN;
                min = 0;
                max = -1;
                position++;
                if (nonGreedy) position++;
                break;

            case '+':
                quantOp = nonGreedy ? EOps.ReNGRangeN : EOps.ReRangeN;
                min = 1;
                max = -1;
                position++;
                if (nonGreedy) position++;
                break;

            case '?':
                quantOp = nonGreedy ? EOps.ReNGRangeNM : EOps.ReRangeNM;
                min = 0;
                max = 1;
                position++;
                if (nonGreedy) position++;
                break;

            case '{':
                (min, max, nonGreedy) = ParseRangeQuantifier();
                quantOp = (max == -1)
                    ? (nonGreedy ? EOps.ReNGRangeN : EOps.ReRangeN)
                    : (nonGreedy ? EOps.ReNGRangeNM : EOps.ReRangeNM);
                break;

            default:
                return; // Not a quantifier
        }

        // Save the original atom data
        SRegInfo* atomNode = AllocateNode();
        *atomNode = *node; // Copy all fields

        // Clear pointers that shouldn't be copied
        atomNode->next = null;
        atomNode->prev = null;
        atomNode->parent = node; // Parent is the quantifier

        // Turn current node into quantifier
        node->op = quantOp;
        node->s = min;
        node->e = max;
        node->param = atomNode;
    }

    /// <summary>
    /// Parse {n}, {n,}, {n,m} range quantifiers
    /// </summary>
    private (int min, int max, bool nonGreedy) ParseRangeQuantifier()
    {
        if (pattern[position] != '{')
            throw new RegexSyntaxException("Expected {");

        position++; // Skip {
        int start = position;

        // Find comma and closing brace
        int commaPos = -1;
        while (position < pattern.Length && pattern[position] != '}')
        {
            if (pattern[position] == ',')
                commaPos = position;
            position++;
        }

        if (position >= pattern.Length)
            throw new RegexSyntaxException("Unclosed range quantifier");

        int endBrace = position; // Position of }
        position++; // Skip }

        // Check for non-greedy
        bool nonGreedy = false;
        if (position < pattern.Length && pattern[position] == '?')
        {
            nonGreedy = true;
            position++;
        }

        int min, max;
        if (commaPos == -1)
        {
            // {n} exact
            string numStr = pattern.Substring(start, endBrace - start);
            min = max = int.Parse(numStr);
        }
        else if (commaPos == endBrace - 1)
        {
            // {n,} unbounded
            string numStr = pattern.Substring(start, commaPos - start);
            min = int.Parse(numStr);
            max = -1;
        }
        else
        {
            // {n,m} range
            string minStr = pattern.Substring(start, commaPos - start);
            string maxStr = pattern.Substring(commaPos + 1, endBrace - commaPos - 1);
            min = int.Parse(minStr);
            max = int.Parse(maxStr);
        }

        return (min, max, nonGreedy);
    }

    /// <summary>
    /// Post-process alternation (ReOr) nodes to create proper tree structure
    /// Port of C++ cregexp.cpp lines 590-661
    /// Returns the potentially new start node (ReOr can become root)
    /// Processes recursively for all subtrees (groups, etc.)
    /// </summary>
    private SRegInfo* ProcessAlternation(SRegInfo* start)
    {
        if (start == null) return start;

        // First, recursively process all subtrees (groups, etc.)
        // We need to collect nodes first to avoid issues while modifying tree
        List<IntPtr> nodesToProcess = new List<IntPtr>();
        SRegInfo* node = start;
        while (node != null)
        {
            if (node->param != null && (node->op == EOps.ReBrackets || node->op == EOps.ReAhead || node->op == EOps.ReNAhead || node->op == EOps.ReBehind || node->op == EOps.ReNBehind))
            {
                // This is a node with a subtree that needs processing
                nodesToProcess.Add((IntPtr)node);
            }
            node = node->next;
        }

        // Process subtrees
        foreach (var ptr in nodesToProcess)
        {
            SRegInfo* n = (SRegInfo*)ptr;
            n->param = ProcessAlternation(n->param);
        }

        // First pass: Add empty alternatives for edge cases
        // Handles: |foo, foo||bar, foo|bar|, foo|bar|*
        // Based on lines 594-626
        SRegInfo* next = start;
        while (next != null)
        {
            if (next->op == EOps.ReOr)
            {
                SRegInfo* temp = AllocateNode();
                temp->parent = next->parent;

                // |foo|bar - empty before first alternative
                if (next->prev == null)
                {
                    temp->next = next;
                    next->prev = temp;
                    next = next->next;
                    continue;
                }

                // foo||bar - empty between alternatives
                if (next->next != null && next->next->op == EOps.ReOr)
                {
                    temp->prev = next;
                    temp->next = next->next;
                    if (next->next != null)
                        next->next->prev = temp;
                    next->next = temp;
                    next = temp->next;
                    continue;
                }

                // foo|bar| - empty after last alternative
                if (next->next == null)
                {
                    temp->prev = next;
                    temp->next = null;
                    next->next = temp;
                    break;
                }

                // foo|bar|* - empty before quantifier
                if (next->next->op > EOps.ReBlockOps && next->next->op < EOps.ReSymbolOps)
                {
                    temp->prev = next;
                    temp->next = next->next;
                    next->next->prev = temp;
                    next->next = temp;
                    next = temp->next;
                    continue;
                }
            }

            next = next->next;
        }

        // Second pass: Reorganize tree - move operands before ReOr into param
        // Based on lines 629-661
        next = start;
        SRegInfo* realFirst;

        while (next != null)
        {
            // Found a ReOr node
            if (next->op == EOps.ReOr)
            {
                if (next->prev == null)
                    throw new RegexSyntaxException("Invalid alternation: no left operand");

                // Find the first node of the left alternative
                realFirst = next->prev;
                realFirst->next = null;
                realFirst->parent = next;

                // Walk back to find the start of the left alternative
                // Stop at another ReOr or at the beginning
                // Port of C++ line 642: while (next->op == EOps::ReOr && realFirst->prev && realFirst->prev->op != EOps::ReOr)
                while (next->op == EOps.ReOr && realFirst->prev != null && realFirst->prev->op != EOps.ReOr)
                {
                    realFirst->parent = next;
                    realFirst = realFirst->prev;
                }

                // Rewire the tree
                if (realFirst->prev == null)
                {
                    // This ReOr becomes the new start
                    start = next;
                    next->param = realFirst;
                    next->prev = null;
                }
                else
                {
                    // Insert ReOr in place of the left alternative
                    next->param = realFirst;
                    next->prev = realFirst->prev;
                    realFirst->prev->next = next;
                }

                realFirst->prev = null;
            }

            next = next->next;
        }

        return start;
    }

    /// <summary>
    /// Allocate a new SRegInfo node
    /// </summary>
    private SRegInfo* AllocateNode()
    {
        SRegInfo* node = (SRegInfo*)Marshal.AllocHGlobal(sizeof(SRegInfo));
        allocatedNodes.Add((IntPtr)node);

        // Initialize to zero
        byte* ptr = (byte*)node;
        for (int i = 0; i < sizeof(SRegInfo); i++)
            ptr[i] = 0;

        node->op = EOps.ReEmpty;
        return node;
    }

    /// <summary>
    /// Create a simple empty character class
    /// </summary>
    private CharacterClass* CreateSimpleCharClass()
    {
        CharacterClass* cc = (CharacterClass*)Marshal.AllocHGlobal(sizeof(CharacterClass));
        *cc = new CharacterClass();
        cc->Clear();
        return cc;
    }

    /// <summary>
    /// Copy node contents (for quantifier wrapping)
    /// </summary>
    private void CopyNode(SRegInfo* dest, SRegInfo* src)
    {
        // This is a shallow copy - pointers are copied, not deep-copied
        *dest = *src;
    }

    public void Dispose()
    {
        foreach (var nodePtr in allocatedNodes)
        {
            SRegInfo* node = (SRegInfo*)nodePtr;

            // Free character class if present
            if ((node->op == EOps.ReEnum || node->op == EOps.ReNEnum) && node->charclass != null)
            {
                Marshal.FreeHGlobal((IntPtr)node->charclass);
            }

            Marshal.FreeHGlobal(nodePtr);
        }
        allocatedNodes.Clear();
    }
}
