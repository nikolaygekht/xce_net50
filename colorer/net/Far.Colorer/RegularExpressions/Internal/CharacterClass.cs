using System;
using System.Runtime.InteropServices;

namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Character class implementation using bitmap for efficient storage
/// Simplified version supporting BMP (first 65536 characters)
/// Based on C++ CharacterClass but adapted for .NET unmanaged structs
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct CharacterClass
{
    // Bitmap for first 65536 characters (BMP) = 65536 / 64 = 1024 ulongs
    private fixed ulong bits[1024];

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
        {
            AddChar(c);
            if (c == char.MaxValue) break; // Prevent overflow
        }
    }

    public readonly bool Contains(char c)
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

    public void Union(CharacterClass* other)
    {
        fixed (ulong* thisPtr = bits)
        {
            ulong* otherPtr = other->bits;
            for (int i = 0; i < 1024; i++)
                thisPtr[i] |= otherPtr[i];
        }
    }

    public void Intersect(CharacterClass* other)
    {
        fixed (ulong* thisPtr = bits)
        {
            ulong* otherPtr = other->bits;
            for (int i = 0; i < 1024; i++)
                thisPtr[i] &= otherPtr[i];
        }
    }

    public void Subtract(CharacterClass* other)
    {
        fixed (ulong* thisPtr = bits)
        {
            ulong* otherPtr = other->bits;
            for (int i = 0; i < 1024; i++)
                thisPtr[i] &= ~otherPtr[i];
        }
    }
}
