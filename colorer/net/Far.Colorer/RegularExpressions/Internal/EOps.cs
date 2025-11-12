namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Regular expression operation codes - direct port from C++ cregexp.h lines 55-90
/// </summary>
internal enum EOps
{
    // Block operations
    ReBlockOps = 0,
    ReMul = 1,        // *
    RePlus = 2,       // +
    ReQuest = 3,      // ?
    ReNGMul = 4,      // *?
    ReNGPlus = 5,     // +?
    ReNGQuest = 6,    // ??
    ReRangeN = 7,     // {n,}
    ReRangeNM = 8,    // {n,m}
    ReNGRangeN = 9,   // {n,}?
    ReNGRangeNM = 10, // {n,m}?
    ReOr = 11,        // |
    ReBehind = 12,    // ?#n
    ReNBehind = 13,   // ?~n
    ReAhead = 14,     // ?=
    ReNAhead = 15,    // ?!

    // Symbol operations
    ReSymbolOps = 16,
    ReEmpty = 17,
    ReMetaSymb = 18,       // \W \s \d ...
    ReSymb = 19,           // a b c ...
    ReWord = 20,           // word...
    ReEnum = 21,           // []
    ReNEnum = 22,          // [^]
    ReBrackets = 23,       // (...)
    ReNamedBrackets = 24,  // (?{name} ...)

    // COLORERMODE - cross-pattern backreferences
    ReBkTrace = 25,        // \yN
    ReBkTraceN = 26,       // \YN
    ReBkTraceName = 27,    // \y{name}
    ReBkTraceNName = 28,   // \Y{name}

    ReBkBrack = 29,        // \N
    ReBkBrackName = 30     // \p{name}
}
