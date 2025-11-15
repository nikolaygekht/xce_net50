# Colorer Regex API Overview

**Document Version**: 1.0
**Date**: 2025-01-15
**Status**: Active

## Introduction

This document provides a concise overview of the **Colorer Regular Expression API** for .NET developers. It focuses on how to use the API, not on the regex language syntax or internal implementation details.

**For detailed information, see**:
- **REGEX_LANGUAGE.md** - Complete regex syntax and language features
- **UNSAFE_CODE_REGEX.md** - Technical implementation details

---

## Quick Start

### Basic Pattern Matching

```csharp
using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Enums;

// Create a regex
var regex = new ColorerRegex(@"\b\w+@\w+\.\w+\b");

// Match against input
var match = regex.Match("Email: user@example.com");

if (match != null && match.Success)
{
    Console.WriteLine($"Found: {match.Value}");  // "user@example.com"
    Console.WriteLine($"At position: {match.Index}");  // 7
}
```

### Static Methods (One-Time Use)

```csharp
// Quick matching without creating a reusable regex object
bool found = ColorerRegex.IsMatch("hello world", @"\bworld\b");

var match = ColorerRegex.Match("test 123", @"\d+");
// match.Value = "123"

// Find all matches
var matches = ColorerRegex.Matches("foo bar baz", @"\b\w+\b");
foreach (var m in matches)
{
    Console.WriteLine(m.Value);  // "foo", "bar", "baz"
}
```

---

## Core API Classes

### 1. ColorerRegex

The main class for compiling and executing regular expressions.

#### Constructors

```csharp
// Basic constructor
public ColorerRegex(string pattern, RegexOptions options = RegexOptions.None)

// Example
var regex = new ColorerRegex(@"\d+", RegexOptions.IgnoreCase);
```

#### Factory Method for Cross-Pattern Backreferences

```csharp
// Create regex with backreference to another regex (Colorer-specific)
public static ColorerRegex CreateWithBackRE(
    string pattern,
    ColorerRegex backRE,
    RegexOptions options = RegexOptions.None)

// Example: Matching paired delimiters
var startRegex = new ColorerRegex(@"(?{quote}["'])");
var endRegex = ColorerRegex.CreateWithBackRE(@"\y{quote}", startRegex);
```

#### Instance Methods

```csharp
// Match at specified position
public ColorerMatch? Match(string input, int startIndex = 0)

// Advanced matching with Colorer-specific parameters
public ColorerMatch? Match(
    string input,
    int startIndex,
    int endIndex,
    int schemeStart = 0,
    bool? posMovesOverride = null)

// Test if pattern matches
public bool IsMatch(string input, int startIndex = 0)

// Find all matches
public IEnumerable<ColorerMatch> Matches(string input)

// Set backreference for cross-pattern matching (Colorer-specific)
public void SetBackReference(string? backStr, ColorerMatch? backMatch)
```

#### Static Methods

```csharp
// One-time matching
public static ColorerMatch? Match(
    string input,
    string pattern,
    RegexOptions options = RegexOptions.None)

public static bool IsMatch(
    string input,
    string pattern,
    RegexOptions options = RegexOptions.None)

public static IEnumerable<ColorerMatch> Matches(
    string input,
    string pattern,
    RegexOptions options = RegexOptions.None)
```

#### Properties

```csharp
public string Pattern { get; }  // The original pattern string
public RegexOptions Options { get; }  // The regex options
```

---

### 2. ColorerMatch

Represents the result of a regex match operation.

#### Properties

```csharp
public bool Success { get; }           // Whether the match succeeded
public int Index { get; }              // Start position in input
public int Length { get; }             // Length of matched text
public string Value { get; }           // Matched text (allocates string)
public string Input { get; }           // Original input string
public IReadOnlyList<CaptureGroup> Groups { get; }  // All capture groups

// Colorer-specific: positions modified by \M and \m markers
public int EffectiveStart { get; }
public int EffectiveEnd { get; }
```

#### Methods - Efficient Access (Zero-Allocation)

```csharp
// Get matched text as span (no allocation)
public ReadOnlySpan<char> GetMatchSpan()

// Get effective match considering \M and \m markers
public ReadOnlySpan<char> GetEffectiveSpan()

// Get capture group text as span (no allocation)
public ReadOnlySpan<char> GetGroupSpan(int groupNumber)
public ReadOnlySpan<char> GetGroupSpan(string name)
```

#### Methods - String Access (Allocates)

```csharp
// Get capture group text as string (allocates)
public string GetGroupValue(int groupNumber)
public string GetGroupValue(string name)
```

#### Methods - Group Access

```csharp
// Get capture group by number or name
public CaptureGroup GetGroup(int groupNumber)
public CaptureGroup GetGroup(string name)

// Try to resolve named group to number
public bool TryGetGroupNumber(string name, out int groupNumber)

// Get all named group names
public IEnumerable<string> GetGroupNames()
```

#### Static Members

```csharp
public static ColorerMatch Empty { get; }  // Singleton failed match
public static ColorerMatch CreateFailed(string input)
```

---

### 3. CaptureGroup

Represents a captured group within a match (lightweight struct).

#### Properties

```csharp
public int Index { get; }         // Start position
public int Length { get; }        // Length of capture
public bool Success { get; }      // Whether captured successfully
public int GroupNumber { get; }   // Group number (0 = entire match)
public string? Name { get; }      // Group name (if named group)
public int EndIndex { get; }      // End position (Index + Length)
```

#### Static Methods

```csharp
public static CaptureGroup CreateFailed(int groupNumber, string? name = null)
```

---

### 4. RegexOptions

Options for controlling regex compilation and matching behavior.

```csharp
[Flags]
public enum RegexOptions
{
    None = 0,
    IgnoreCase = 1,      // Case-insensitive matching
    Multiline = 2,       // ^ and $ match line boundaries
    Singleline = 4,      // . matches newlines
    Extended = 8,        // Ignore whitespace (not fully implemented)
    PositionMoves = 16   // Colorer-specific: enable \M and \m markers
}
```

#### Usage

```csharp
// Combine options with bitwise OR
var regex = new ColorerRegex(
    @"\w+",
    RegexOptions.IgnoreCase | RegexOptions.Multiline);
```

---

## Common Usage Patterns

### Pattern 1: Simple Matching

```csharp
var regex = new ColorerRegex(@"\d{3}-\d{4}");
var match = regex.Match("Call 555-1234 now");

if (match != null)
{
    Console.WriteLine($"Phone: {match.Value}");  // "555-1234"
}
```

### Pattern 2: Named Capture Groups

```csharp
var regex = new ColorerRegex(@"(?{name}\w+)=(?{value}\d+)");
var match = regex.Match("timeout=30");

if (match != null)
{
    string name = match.GetGroupValue("name");    // "timeout"
    string value = match.GetGroupValue("value");  // "30"
}
```

### Pattern 3: Finding All Matches

```csharp
var regex = new ColorerRegex(@"\b\w+\b");
var input = "The quick brown fox";

foreach (var match in regex.Matches(input))
{
    Console.WriteLine($"Word: {match.Value}");
}
// Output:
// Word: The
// Word: quick
// Word: brown
// Word: fox
```

### Pattern 4: Case-Insensitive Matching

```csharp
var regex = new ColorerRegex(@"\btest\b", RegexOptions.IgnoreCase);

Console.WriteLine(regex.IsMatch("Test"));   // True
Console.WriteLine(regex.IsMatch("TEST"));   // True
Console.WriteLine(regex.IsMatch("test"));   // True
Console.WriteLine(regex.IsMatch("testing")); // False (word boundary)
```

### Pattern 5: Multiple Capture Groups

```csharp
var regex = new ColorerRegex(@"(\d{4})-(\d{2})-(\d{2})");
var match = regex.Match("Date: 2025-01-15");

if (match != null)
{
    string year = match.GetGroupValue(1);   // "2025"
    string month = match.GetGroupValue(2);  // "01"
    string day = match.GetGroupValue(3);    // "15"

    // Or access via Groups collection
    Console.WriteLine($"Group 0: {match.Groups[0].Index}"); // Entire match
    Console.WriteLine($"Group 1: {match.Groups[1].Index}"); // Year
}
```

### Pattern 6: Zero-Allocation Matching (Performance)

```csharp
var regex = new ColorerRegex(@"\b\w+\b");
var input = "hello world";

foreach (var match in regex.Matches(input))
{
    // Use span instead of string to avoid allocation
    ReadOnlySpan<char> word = match.GetMatchSpan();

    // Process without allocating strings
    if (word.Length > 5)
    {
        Console.WriteLine($"Long word: {word.ToString()}");
    }
}
```

### Pattern 7: Cross-Pattern Backreferences (Colorer-Specific)

**Use case**: Matching paired delimiters in syntax highlighting

```csharp
// Pattern to capture opening quote
var startRegex = new ColorerRegex(@"(?{delim}["'])");

// Pattern to match closing quote (same as opening)
var endRegex = ColorerRegex.CreateWithBackRE(@"\y{delim}", startRegex);

// Match opening quote
var startMatch = startRegex.Match("\"hello world\"");
if (startMatch != null)
{
    // Tell endRegex what was captured
    endRegex.SetBackReference("\"hello world\"", startMatch);

    // Find matching closing quote
    var endMatch = endRegex.Match("\"hello world\"", startIndex: 12);

    if (endMatch != null)
    {
        Console.WriteLine("Found matching delimiter");
    }
}
```

### Pattern 8: Reusing Regex Objects (Performance)

```csharp
// Create once, use many times (thread-safe)
var emailRegex = new ColorerRegex(@"\b[\w.]+@[\w.]+\.\w+\b");

// Use in multiple threads safely
Parallel.For(0, 1000, i =>
{
    string email = $"user{i}@example.com";
    var match = emailRegex.Match(email);
    // Process match...
});
```

### Pattern 9: Conditional Matching

```csharp
var regex = new ColorerRegex(@"\b(if|while|for)\b");
var input = "if (condition) while (true) for (;;)";

foreach (var match in regex.Matches(input))
{
    switch (match.Value)
    {
        case "if":
            Console.WriteLine("Found conditional");
            break;
        case "while":
        case "for":
            Console.WriteLine("Found loop");
            break;
    }
}
```

### Pattern 10: Validating Input

```csharp
public static bool IsValidEmail(string email)
{
    var regex = new ColorerRegex(@"^[\w.+-]+@[\w.-]+\.[a-zA-Z]{2,}$");
    return regex.IsMatch(email);
}

public static bool IsValidPhoneNumber(string phone)
{
    var regex = new ColorerRegex(@"^\d{3}-\d{3}-\d{4}$");
    return regex.IsMatch(phone);
}
```

---

## Performance Considerations

### Best Practices

#### 1. Reuse Regex Objects

**Bad** (creates new regex every time):
```csharp
foreach (var line in lines)
{
    var regex = new ColorerRegex(@"\d+");  // DON'T DO THIS
    var match = regex.Match(line);
}
```

**Good** (creates once, reuses):
```csharp
var regex = new ColorerRegex(@"\d+");
foreach (var line in lines)
{
    var match = regex.Match(line);
}
```

#### 2. Use Static Methods for One-Time Matches

```csharp
// If you only need to match once, use static method
if (ColorerRegex.IsMatch(input, @"\d+"))
{
    // ...
}

// Don't create an object just to use once
// var regex = new ColorerRegex(@"\d+");  // Wasteful
// if (regex.IsMatch(input)) { }
```

#### 3. Use Span-Based Methods for Hot Paths

```csharp
// Allocates string
string value = match.GetGroupValue(1);

// Zero allocation (preferred in loops)
ReadOnlySpan<char> value = match.GetGroupSpan(1);
```

#### 4. Limit Backtracking

Avoid patterns that cause catastrophic backtracking:

```csharp
// BAD: Catastrophic backtracking
var bad = new ColorerRegex(@"(a+)+b");

// GOOD: Simple and efficient
var good = new ColorerRegex(@"a+b");
```

### Thread Safety

- **ColorerRegex instances are thread-safe** for matching operations
- Multiple threads can call `Match()` on the same instance simultaneously
- Pattern compilation happens once at construction (thread-safe)

```csharp
var regex = new ColorerRegex(@"\d+");

// Safe: Multiple threads using same instance
Parallel.ForEach(lines, line =>
{
    var match = regex.Match(line);  // Thread-safe
    // Process match...
});
```

### Memory Considerations

- **ColorerMatch** objects are lightweight but hold references to input strings
- **CaptureGroup** is a struct (value type, no heap allocation)
- Pattern compilation allocates an AST (abstract syntax tree)
- Large patterns with many capture groups use more memory

**Estimated memory usage**:
```
ColorerRegex instance: ~1-10 KB (depending on pattern complexity)
ColorerMatch instance: ~200 bytes + capture groups
CaptureGroup: 32 bytes (struct)
```

---

## Integration with HRC Syntax Highlighting

The regex API is designed specifically for HRC (Highlighting Rule Configuration) files used in syntax highlighting.

### Typical HRC Usage Pattern

```csharp
// 1. Compile start and end patterns from HRC file
var startRegex = new ColorerRegex(@"(?{StringEdge}["'])");
var endRegex = ColorerRegex.CreateWithBackRE(@"\y{StringEdge}", startRegex);

// 2. Match start pattern in source code
var startMatch = startRegex.Match(sourceCode, lineStart);

if (startMatch != null)
{
    // 3. Set backreference for end pattern
    endRegex.SetBackReference(sourceCode, startMatch);

    // 4. Find matching end delimiter
    var endMatch = endRegex.Match(sourceCode, startMatch.Index + startMatch.Length);

    if (endMatch != null)
    {
        // 5. Highlight region from start to end
        int highlightStart = startMatch.Index;
        int highlightEnd = endMatch.Index + endMatch.Length;
        ApplySyntaxHighlight(highlightStart, highlightEnd, "String");
    }
}
```

### Scheme Start Anchor

The `~` anchor matches the start of a highlighting scheme (not line start):

```csharp
var regex = new ColorerRegex(@"~\s*#include");

// Match at scheme start (position 0)
var match = regex.Match(
    "  #include <stdio.h>",
    startIndex: 0,
    endIndex: 20,
    schemeStart: 0);  // schemeStart parameter sets where ~ matches

// match will succeed because we're at schemeStart
```

### Match Position Markers

The `\m` and `\M` markers allow adjusting reported match boundaries:

```csharp
var regex = new ColorerRegex(@"\w+\m:", RegexOptions.PositionMoves);

var match = regex.Match("label:");

if (match != null)
{
    Console.WriteLine($"Match: {match.Value}");  // "label:"
    Console.WriteLine($"Effective: {match.GetEffectiveSpan().ToString()}");  // ":"
    // EffectiveStart is at the \m marker, not at start of word
}
```

---

## Error Handling

### Compilation Errors

```csharp
try
{
    var regex = new ColorerRegex(@"(?{unclosed");
}
catch (RegexSyntaxException ex)
{
    Console.WriteLine($"Pattern error: {ex.Message}");
}
```

### Runtime Errors

```csharp
// Invalid input (null)
try
{
    var regex = new ColorerRegex(@"\d+");
    regex.Match(null);  // Throws ArgumentNullException
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Input error: {ex.Message}");
}

// Invalid group number
var match = regex.Match("123");
var group = match.GetGroup(99);  // Returns failed group (doesn't throw)
if (!group.Success)
{
    Console.WriteLine("Group not found");
}
```

---

## Limitations

### Capture Group Limits

- **Maximum 10 capture groups** (0-9, where 0 is the entire match)
- Attempting to access group 10 or higher returns a failed `CaptureGroup`

```csharp
var regex = new ColorerRegex(@"(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)");
// Only groups 0-9 are valid
```

### Pattern Complexity

- Very complex patterns with deep nesting may consume significant memory
- Patterns with many alternations may be slower to match

### Unicode Support

- Supports **BMP (Basic Multilingual Plane)** characters (U+0000 to U+FFFF)
- Supplementary characters (emoji, etc.) may not be fully supported

---

## Migration from .NET Regex

If migrating from `System.Text.RegularExpressions.Regex`:

### Syntax Differences

| .NET Regex | Colorer Regex |
|------------|---------------|
| `(?<name>...)` | `(?{name}...)` |
| `\k<name>` | `\p{name}` |
| Variable-width lookbehind | Fixed-width only `(?#N)` |

### API Differences

| .NET Regex | Colorer Regex |
|------------|---------------|
| `match.Groups["name"]` | `match.GetGroup("name")` |
| `match.Groups[1].Value` | `match.GetGroupValue(1)` |
| `Regex.Matches()` returns `MatchCollection` | Returns `IEnumerable<ColorerMatch>` |

### Example Migration

**Before** (.NET):
```csharp
var regex = new Regex(@"(?<word>\w+)");
var match = regex.Match("hello");
string word = match.Groups["word"].Value;
```

**After** (Colorer):
```csharp
var regex = new ColorerRegex(@"(?{word}\w+)");
var match = regex.Match("hello");
string word = match.GetGroupValue("word");
```

---

## Debugging Tips

### 1. Inspect Match Results

```csharp
var regex = new ColorerRegex(@"(?{year}\d{4})-(?{month}\d{2})");
var match = regex.Match("2025-01-15");

if (match != null)
{
    Console.WriteLine(match);  // Uses ToString() for debugging
    // Output: Match: [0..7): "2025-01"

    foreach (var group in match.Groups)
    {
        Console.WriteLine(group);
        // Output:
        // Group 0: [0..7)
        // Group 1 (year): [0..4)
        // Group 2 (month): [5..7)
    }
}
```

### 2. Test Patterns Interactively

```csharp
void TestPattern(string pattern, string input)
{
    try
    {
        var regex = new ColorerRegex(pattern);
        var match = regex.Match(input);

        if (match != null)
        {
            Console.WriteLine($"✓ Match: '{match.Value}' at position {match.Index}");

            foreach (var name in match.GetGroupNames())
            {
                Console.WriteLine($"  {name}: '{match.GetGroupValue(name)}'");
            }
        }
        else
        {
            Console.WriteLine("✗ No match");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Error: {ex.Message}");
    }
}

// Usage
TestPattern(@"(?{word}\w+)", "hello");
TestPattern(@"\d{3}-\d{4}", "555-1234");
```

### 3. Validate Pattern Compilation

```csharp
bool IsValidPattern(string pattern)
{
    try
    {
        var regex = new ColorerRegex(pattern);
        return true;
    }
    catch
    {
        return false;
    }
}
```

---

## See Also

- **REGEX_LANGUAGE.md** - Complete regex syntax reference with Colorer-specific features
- **UNSAFE_CODE_REGEX.md** - Technical implementation details and performance analysis
- **CLAUDE.md** - Overall project architecture
- [Colorer Project](https://github.com/colorer/Colorer-library) - C++ original implementation
- [HRC Files](https://colorer.github.io/hrc/) - Syntax definition format

---

**Document Maintenance**: Update this document when:
- Public API changes
- New methods are added
- Usage patterns evolve
- Performance characteristics change
