namespace Far.Colorer.RegularExpressions.Enums;

/// <summary>
/// Options for regex compilation and matching.
/// </summary>
[Flags]
public enum RegexOptions
{
    /// <summary>
    /// No options specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Case-insensitive matching.
    /// Equivalent to /i flag.
    /// </summary>
    IgnoreCase = 1 << 0,

    /// <summary>
    /// Multi-line mode: ^ and $ match line boundaries.
    /// Equivalent to /m flag.
    /// </summary>
    Multiline = 1 << 1,

    /// <summary>
    /// Single-line mode: . matches newline characters.
    /// Equivalent to /s flag.
    /// </summary>
    Singleline = 1 << 2,

    /// <summary>
    /// Extended mode: ignore whitespace and allow comments.
    /// Equivalent to /x flag.
    /// </summary>
    Extended = 1 << 3,

    /// <summary>
    /// Allow position moves during matching.
    /// Colorer-specific option for \M and \m support.
    /// </summary>
    PositionMoves = 1 << 4
}
