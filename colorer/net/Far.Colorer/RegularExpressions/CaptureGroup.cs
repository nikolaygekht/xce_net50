namespace Far.Colorer.RegularExpressions;

/// <summary>
/// Represents a captured group in a regex match.
/// Designed for efficient access with minimal allocations.
/// </summary>
public readonly struct CaptureGroup
{
    /// <summary>
    /// Gets the start index of the capture in the input string.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the length of the captured text.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets whether this capture was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the group number (0-based). Group 0 is the entire match.
    /// </summary>
    public int GroupNumber { get; }

    /// <summary>
    /// Gets the name of the group, if it's a named group.
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// Creates a successful capture group.
    /// </summary>
    public CaptureGroup(int index, int length, int groupNumber, string? name = null)
    {
        Index = index;
        Length = length;
        Success = true;
        GroupNumber = groupNumber;
        Name = name;
    }

    /// <summary>
    /// Creates a failed capture group.
    /// </summary>
    private CaptureGroup(int groupNumber, string? name)
    {
        Index = -1;
        Length = 0;
        Success = false;
        GroupNumber = groupNumber;
        Name = name;
    }

    /// <summary>
    /// Creates a failed capture group.
    /// </summary>
    public static CaptureGroup CreateFailed(int groupNumber, string? name = null)
    {
        return new CaptureGroup(groupNumber, name);
    }

    /// <summary>
    /// Gets the end index (exclusive) of the capture.
    /// </summary>
    public int EndIndex => Index + Length;

    public override string ToString()
    {
        if (!Success)
            return $"Group {GroupNumber}{(Name != null ? $" ({Name})" : "")}: [Failed]";

        return $"Group {GroupNumber}{(Name != null ? $" ({Name})" : "")}: [{Index}..{EndIndex})";
    }
}
