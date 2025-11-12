namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Metacharacter symbols - direct port from C++ cregexp.h lines 92-117
/// </summary>
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
    ReWSpace = 9,      // \s isWhiteSpace()
    ReNWSpace = 10,    // \S
    ReUCase = 11,      // \u
    ReNUCase = 12,     // \l
    ReWBound = 13,     // \b
    ReNWBound = 14,    // \B
    RePreNW = 15,      // \c
    ReStart = 16,      // \m (COLORERMODE - match start marker)
    ReEnd = 17,        // \M (COLORERMODE - match end marker)
    ReChrLast = 18
}
