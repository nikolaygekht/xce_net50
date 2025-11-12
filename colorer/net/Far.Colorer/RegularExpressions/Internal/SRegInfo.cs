using System.Runtime.InteropServices;

namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Regular expression tree node - direct port from C++ cregexp.h lines 137-163
/// CRITICAL: Must be unsafe struct (not class) to support pointers and sizeof
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 128)] // Ensure enough space for the union
internal unsafe struct SRegInfo
{
    // Linked list structure
    [FieldOffset(0)]
    public SRegInfo* parent;

    [FieldOffset(8)]
    public SRegInfo* next;

    [FieldOffset(16)]
    public SRegInfo* prev;

    // Union members - only one is valid based on 'op'
    // These all share the same memory location (offset 24)
    [FieldOffset(24)]
    public EMetaSymbols metaSymbol;

    [FieldOffset(24)]
    public char symbol;

    [FieldOffset(24)]
    public void* word;  // UnicodeString* in C++

    [FieldOffset(24)]
    public CharacterClass* charclass;

    [FieldOffset(24)]
    public SRegInfo* param;

    // State fields
    [FieldOffset(32)]
    public int oldParse;

    [FieldOffset(36)]
    public int param0;

    [FieldOffset(40)]
    public int param1;

    [FieldOffset(44)]
    public int s;

    [FieldOffset(48)]
    public int e;

    [FieldOffset(52)]
    public EOps op;
}
