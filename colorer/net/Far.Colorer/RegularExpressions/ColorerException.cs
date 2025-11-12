namespace Far.Colorer.RegularExpressions;

/// <summary>
/// Base exception for Colorer regex errors.
/// </summary>
public class ColorerException : Exception
{
    public ColorerException() : base()
    {
    }

    public ColorerException(string message) : base(message)
    {
    }

    public ColorerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exception thrown when a regex pattern has syntax errors.
/// </summary>
public class RegexSyntaxException : ColorerException
{
    public string? Pattern { get; }
    public int Position { get; }

    public RegexSyntaxException(string message, string? pattern = null, int position = -1)
        : base(message)
    {
        Pattern = pattern;
        Position = position;
    }

    public RegexSyntaxException(string message, string? pattern, int position, Exception innerException)
        : base(message, innerException)
    {
        Pattern = pattern;
        Position = position;
    }

    public override string Message
    {
        get
        {
            if (Pattern != null && Position >= 0)
            {
                return $"{base.Message} at position {Position} in pattern: {Pattern}";
            }
            return base.Message;
        }
    }
}

/// <summary>
/// Exception thrown when backreference operations fail.
/// </summary>
public class BackreferenceException : ColorerException
{
    public BackreferenceException(string message) : base(message)
    {
    }
}
