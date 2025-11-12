namespace Far.Colorer.RegularExpressions;

/// <summary>
/// Represents the result of a regex match operation.
/// Designed for efficient access with Span support.
/// </summary>
public sealed class ColorerMatch
{
    private readonly string _input;
    private readonly CaptureGroup[] _groups;
    private readonly Dictionary<string, int> _namedGroups;

    /// <summary>
    /// Gets whether the match was successful.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the starting index of the match in the input string.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the length of the matched text.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the effective start position (modified by \M marker).
    /// </summary>
    public int EffectiveStart { get; internal set; }

    /// <summary>
    /// Gets the effective end position (modified by \m marker).
    /// </summary>
    public int EffectiveEnd { get; internal set; }

    /// <summary>
    /// Gets all captured groups (including group 0 which is the entire match).
    /// </summary>
    public IReadOnlyList<CaptureGroup> Groups => _groups;

    /// <summary>
    /// Gets the input string that was matched against.
    /// </summary>
    public string Input => _input;

    /// <summary>
    /// Gets the matched text as a string (allocates).
    /// For zero-allocation access, use GetMatchSpan().
    /// </summary>
    public string Value => Success ? _input.Substring(Index, Length) : string.Empty;

    /// <summary>
    /// Creates a successful match.
    /// </summary>
    internal ColorerMatch(
        string input,
        int index,
        int length,
        CaptureGroup[] groups,
        Dictionary<string, int> namedGroups)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _groups = groups ?? throw new ArgumentNullException(nameof(groups));
        _namedGroups = namedGroups ?? throw new ArgumentNullException(nameof(namedGroups));

        Success = true;
        Index = index;
        Length = length;
        EffectiveStart = index;
        EffectiveEnd = index + length;
    }

    /// <summary>
    /// Creates a successful match with a List of groups (for convenience).
    /// </summary>
    internal ColorerMatch(
        string input,
        int index,
        int length,
        List<CaptureGroup> groups)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _groups = groups?.ToArray() ?? throw new ArgumentNullException(nameof(groups));
        _namedGroups = new Dictionary<string, int>();

        // Build named groups map
        for (int i = 0; i < _groups.Length; i++)
        {
            if (_groups[i].Name is not null)
            {
                _namedGroups[_groups[i].Name!] = i;
            }
        }

        Success = true;
        Index = index;
        Length = length;
        EffectiveStart = index;
        EffectiveEnd = index + length;
    }

    /// <summary>
    /// Creates a failed match.
    /// </summary>
    private ColorerMatch(string input)
    {
        _input = input ?? string.Empty;
        _groups = Array.Empty<CaptureGroup>();
        _namedGroups = new Dictionary<string, int>();

        Success = false;
        Index = -1;
        Length = 0;
        EffectiveStart = -1;
        EffectiveEnd = -1;
    }

    /// <summary>
    /// Singleton failed match instance.
    /// </summary>
    public static ColorerMatch Empty { get; } = new ColorerMatch(string.Empty);

    /// <summary>
    /// Creates a failed match for a given input.
    /// </summary>
    public static ColorerMatch CreateFailed(string input)
    {
        return new ColorerMatch(input);
    }

    /// <summary>
    /// Gets the matched text as a span (zero allocation).
    /// </summary>
    public ReadOnlySpan<char> GetMatchSpan()
    {
        return Success ? _input.AsSpan(Index, Length) : ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Gets the effective match span (considering \M and \m markers).
    /// </summary>
    public ReadOnlySpan<char> GetEffectiveSpan()
    {
        if (!Success) return ReadOnlySpan<char>.Empty;

        int start = EffectiveStart;
        int end = EffectiveEnd;

        if (start < 0 || end < start || end > _input.Length)
            return GetMatchSpan();

        return _input.AsSpan(start, end - start);
    }

    /// <summary>
    /// Gets a captured group by number.
    /// </summary>
    public CaptureGroup GetGroup(int groupNumber)
    {
        if (groupNumber < 0 || groupNumber >= _groups.Length)
            return CaptureGroup.CreateFailed(groupNumber);

        return _groups[groupNumber];
    }

    /// <summary>
    /// Gets a captured group by name.
    /// </summary>
    public CaptureGroup GetGroup(string name)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        if (_namedGroups.TryGetValue(name, out int groupNumber))
            return _groups[groupNumber];

        return CaptureGroup.CreateFailed(-1, name);
    }

    /// <summary>
    /// Gets the text of a captured group as a span (zero allocation).
    /// </summary>
    public ReadOnlySpan<char> GetGroupSpan(int groupNumber)
    {
        var group = GetGroup(groupNumber);
        return group.Success ? _input.AsSpan(group.Index, group.Length) : ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Gets the text of a named captured group as a span (zero allocation).
    /// </summary>
    public ReadOnlySpan<char> GetGroupSpan(string name)
    {
        var group = GetGroup(name);
        return group.Success ? _input.AsSpan(group.Index, group.Length) : ReadOnlySpan<char>.Empty;
    }

    /// <summary>
    /// Gets the text of a captured group as a string (allocates).
    /// </summary>
    public string GetGroupValue(int groupNumber)
    {
        return GetGroupSpan(groupNumber).ToString();
    }

    /// <summary>
    /// Gets the text of a named captured group as a string (allocates).
    /// </summary>
    public string GetGroupValue(string name)
    {
        return GetGroupSpan(name).ToString();
    }

    /// <summary>
    /// Tries to get the group number for a named group.
    /// </summary>
    public bool TryGetGroupNumber(string name, out int groupNumber)
    {
        return _namedGroups.TryGetValue(name, out groupNumber);
    }

    /// <summary>
    /// Gets all named group names.
    /// </summary>
    public IEnumerable<string> GetGroupNames()
    {
        return _namedGroups.Keys;
    }

    public override string ToString()
    {
        if (!Success)
            return "[Failed Match]";

        return $"Match: [{Index}..{Index + Length}): \"{Value}\"";
    }
}
