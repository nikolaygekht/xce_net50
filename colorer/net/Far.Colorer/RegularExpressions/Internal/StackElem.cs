using System.Runtime.InteropServices;

namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Stack element for backtracking - direct port from C++ cregexp.h lines 165-176
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct StackElem
{
    public SRegInfo* re;
    public SRegInfo* prev;
    public int toParse;
    public bool leftenter;
    public int ifTrueReturn;   // ReAction value
    public int ifFalseReturn;  // ReAction value
}
