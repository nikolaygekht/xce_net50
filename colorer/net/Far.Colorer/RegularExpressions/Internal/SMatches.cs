using System.Runtime.InteropServices;

namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Structure to hold match positions for captures.
/// Port of C++ SMatches from cregexp.h lines 122-132
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct SMatches
{
    private const int MATCHES_NUM = 10;
    private const int NAMED_MATCHES_NUM = 10;

    // Start positions for numeric captures [0-9]
    public fixed int s[MATCHES_NUM];

    // End positions for numeric captures [0-9]
    public fixed int e[MATCHES_NUM];

    // Count of numeric captures
    public int cMatch;

    // Start positions for named captures
    public fixed int ns[NAMED_MATCHES_NUM];

    // End positions for named captures
    public fixed int ne[NAMED_MATCHES_NUM];

    // Count of named captures
    public int cnMatch;

    public void Clear()
    {
        fixed (int* sPtr = s, ePtr = e, nsPtr = ns, nePtr = ne)
        {
            for (int i = 0; i < MATCHES_NUM; i++)
            {
                sPtr[i] = -1;
                ePtr[i] = -1;
            }
            for (int i = 0; i < NAMED_MATCHES_NUM; i++)
            {
                nsPtr[i] = -1;
                nePtr[i] = -1;
            }
        }
        cMatch = 0;
        cnMatch = 0;
    }
}
