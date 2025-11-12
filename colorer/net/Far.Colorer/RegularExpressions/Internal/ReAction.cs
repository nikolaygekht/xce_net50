namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Action codes for matcher control flow - direct port from C++ cregexp.h lines 181-191
/// </summary>
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
