namespace Far.Colorer.RegularExpressions.Internal;

/// <summary>
/// Character classification and conversion utilities.
/// Port of C++ Character class from colorer/strings/icu/Character.h
/// </summary>
internal static class Character
{
    public static bool IsWhitespace(char c)
    {
        return char.IsWhiteSpace(c);
    }

    public static bool IsLowerCase(char c)
    {
        return char.IsLower(c);
    }

    public static bool IsUpperCase(char c)
    {
        return char.IsUpper(c);
    }

    public static bool IsLetter(char c)
    {
        return char.IsLetter(c);
    }

    public static bool IsLetterOrDigit(char c)
    {
        return char.IsLetterOrDigit(c);
    }

    public static bool IsDigit(char c)
    {
        return char.IsDigit(c);
    }

    public static char ToLowerCase(char c)
    {
        return char.ToLowerInvariant(c);
    }

    public static char ToUpperCase(char c)
    {
        return char.ToUpperInvariant(c);
    }

    public static char ToTitleCase(char c)
    {
        return char.ToUpperInvariant(c); // Title case approximation
    }
}
