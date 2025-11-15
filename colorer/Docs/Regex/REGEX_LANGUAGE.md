# Colorer Regular Expression Language Reference

**Document Version**: 1.0
**Date**: 2025-01-15
**Status**: Active

## Table of Contents

1. [Introduction](#introduction)
2. [Standard Regular Expression Features](#standard-regular-expression-features)
3. [Colorer-Specific Extensions](#colorer-specific-extensions)
4. [Complete Syntax Reference](#complete-syntax-reference)
5. [Usage in HRC Files](#usage-in-hrc-files)
6. [Examples](#examples)
7. [Differences from .NET Regex](#differences-from-net-regex)
8. [Best Practices](#best-practices)

---

## Introduction

Colorer uses a **custom regular expression engine** that extends standard regex syntax with specialized features for syntax highlighting. The engine is designed specifically for:

- **Cross-pattern backreferences**: Match text captured by a different regex
- **Scheme-aware matching**: Special anchors for highlighting contexts
- **Named capture groups**: Colorer-specific syntax for named groups
- **Unicode character classes**: Full Unicode support with category matching

### Why a Custom Engine?

The Colorer regex engine cannot be replaced with .NET's `System.Text.RegularExpressions` because:

1. **Cross-pattern backreferences** (`\y`, `\Y`) - essential for matching paired delimiters
2. **Scheme anchors** (`~`, `\m`, `\M`) - required for incremental parsing
3. **Named group syntax** (`(?{name}...)`) - different from .NET's `(?<name>...)`
4. **HRC file compatibility** - must support thousands of existing syntax definitions

---

## Standard Regular Expression Features

### Basic Metacharacters

| Character | Meaning | Example | Matches |
|-----------|---------|---------|---------|
| `.` | Any character (except newline in non-singleline mode) | `a.c` | `abc`, `a1c`, `a-c` |
| `^` | Start of line (or string in non-multiline mode) | `^Hello` | `Hello world` (at start) |
| `$` | End of line (or string in non-multiline mode) | `world$` | `Hello world` (at end) |
| `\|` | Alternation (OR) | `cat\|dog` | `cat` or `dog` |

### Quantifiers

| Quantifier | Meaning | Example | Matches |
|------------|---------|---------|---------|
| `*` | Zero or more (greedy) | `ab*c` | `ac`, `abc`, `abbc` |
| `+` | One or more (greedy) | `ab+c` | `abc`, `abbc` (not `ac`) |
| `?` | Zero or one (greedy) | `ab?c` | `ac`, `abc` |
| `{n}` | Exactly n times | `a{3}` | `aaa` |
| `{n,}` | At least n times | `a{2,}` | `aa`, `aaa`, `aaaa` |
| `{n,m}` | Between n and m times | `a{2,4}` | `aa`, `aaa`, `aaaa` |
| `*?` | Zero or more (non-greedy) | `a.*?b` | `ab` in `axxxbxxxb` |
| `+?` | One or more (non-greedy) | `a.+?b` | `axb` in `axbxb` |
| `??` | Zero or one (non-greedy) | `ab??c` | `ac` before `abc` |
| `{n,}?` | At least n (non-greedy) | `a{2,}?` | `aa` in `aaaa` |
| `{n,m}?` | Between n and m (non-greedy) | `a{2,4}?` | `aa` in `aaaa` |

### Character Classes

| Syntax | Meaning | Example |
|--------|---------|---------|
| `[abc]` | Any of a, b, or c | `[aeiou]` matches vowels |
| `[^abc]` | Not a, b, or c | `[^0-9]` matches non-digits |
| `[a-z]` | Range from a to z | `[A-Za-z]` matches letters |
| `[a-z0-9]` | Multiple ranges/chars | `[a-zA-Z0-9_]` matches word chars |

### Predefined Character Classes

| Escape | Meaning | Equivalent |
|--------|---------|------------|
| `\d` | Digit | `[0-9]` |
| `\D` | Non-digit | `[^0-9]` |
| `\w` | Word character | `[a-zA-Z0-9_]` |
| `\W` | Non-word character | `[^a-zA-Z0-9_]` |
| `\s` | Whitespace | `[ \t\n\r\f]` |
| `\S` | Non-whitespace | `[^ \t\n\r\f]` |
| `\u` | Uppercase letter | Matches uppercase characters |
| `\l` | Lowercase letter | Matches lowercase characters |

### Word Boundaries

| Escape | Meaning | Example |
|--------|---------|---------|
| `\b` | Word boundary | `\bword\b` matches `word` but not `sword` |
| `\B` | Non-word boundary | `\Bword\B` matches `sword` but not `word` |
| `\c` | Precedes non-word | Matches start of word |

### Escape Sequences

| Escape | Character | ASCII Code |
|--------|-----------|------------|
| `\t` | Tab | 0x09 |
| `\n` | Newline | 0x0A |
| `\r` | Carriage return | 0x0D |
| `\f` | Form feed | 0x0C |
| `\a` | Alert/bell | 0x07 |
| `\e` | Escape | 0x1B |
| `\xNN` | Hex character (2 digits) | `\x41` = 'A' |
| `\x{NNNN}` | Hex character (variable) | `\x{1F600}` = 😀 |
| `\NNN` | Octal character (1-3 digits, max 377 = 255) | `\101` = 'A' |

### Grouping and Capturing

| Syntax | Meaning | Example |
|--------|---------|---------|
| `(...)` | Capture group | `(abc)` captures `abc` as group 1 |
| `(?:...)` | Non-capturing group | `(?:abc)` matches but doesn't capture |
| `\1` - `\9` | Backreference to group | `(.)\\1` matches `aa`, `bb`, etc. |

### Lookahead and Lookbehind

| Syntax | Meaning | Example |
|--------|---------|---------|
| `(?=...)` | Positive lookahead | `foo(?=bar)` matches `foo` only if followed by `bar` |
| `(?!...)` | Negative lookahead | `foo(?!bar)` matches `foo` not followed by `bar` |
| `(?#N)` | Positive lookbehind (N chars) | `(?#3)bar` matches `bar` if preceded by 3 chars |
| `(?~N)` | Negative lookbehind (N chars) | `(?~3)bar` matches `bar` not preceded by 3 chars |

**Note**: Colorer's lookbehind requires a **fixed width** (N characters), unlike .NET which supports variable-width lookbehind.

---

## Colorer-Specific Extensions

### 1. Named Capture Groups - `(?{name}...)`

**Syntax**: `(?{name}pattern)`

**Description**: Captures matched text and assigns it a name. Different from .NET's `(?<name>...)` syntax.

**Examples**:

```regex
(?{keyword}if|while|for)       # Captures keyword name
(?{delim}["'])                 # Captures quote delimiter
(?{indent}\s*)                 # Captures indentation
```

**Usage in code**:

```csharp
var regex = new ColorerRegex(@"(?{word}\w+)");
var match = regex.Match("hello");
string captured = match.GetGroupValue("word");  // "hello"
```

**Empty name**: `(?{}pattern)` is treated as non-capturing group `(?:pattern)`

### 2. Cross-Pattern Backreferences - `\yN` and `\y{name}`

**Purpose**: Match text captured by a **different regex** (essential for paired delimiters in syntax highlighting)

#### Numeric Cross-Pattern Backreferences

**Syntax**:
- `\yN` - Match group N from reference regex (case-sensitive)
- `\YN` - Match group N from reference regex (case-insensitive)

**Example**: Matching comment delimiters

```csharp
// HRC example: asm.hrc
// <block start="/(COMMENT) (.)/i" end="/\y2/"/>

var startRegex = new ColorerRegex(@"(COMMENT) (.)", RegexOptions.IgnoreCase);
var endRegex = new ColorerRegex(@"\y2");  // Matches group 2 from start

var startMatch = startRegex.Match("COMMENT #");
// startMatch.Groups[2] = "#"

endRegex.SetBackReference("COMMENT #", startMatch);
var endMatch = endRegex.Match("#");  // Matches!
```

#### Named Cross-Pattern Backreferences

**Syntax**:
- `\y{name}` - Match named group from reference regex (case-sensitive)
- `\Y{name}` - Match named group from reference regex (case-insensitive)

**Example**: Matching string delimiters

```csharp
// HRC example: c.hrc string matching
// <block start="/(?{StringEdge}[\"'])/i" end="/\y{StringEdge}/"/>

var startRegex = new ColorerRegex(@"(?{delim}["'])");
var endRegex = ColorerRegex.CreateWithBackRE(@"\y{delim}", startRegex);

var startMatch = startRegex.Match("'hello");
// startMatch named group "delim" = "'"

endRegex.SetBackReference("'hello", startMatch);
var endMatch = endRegex.Match("'");  // Matches the same quote type
```

**Important**: Named cross-pattern backreferences require using `CreateWithBackRE()` to resolve group numbers at compile time.

### 3. Scheme Anchors - `~`, `\m`, `\M`

These metacharacters are specific to Colorer's incremental parsing system.

#### Scheme Start Anchor - `~`

**Syntax**: `~`

**Description**: Matches the **start of the current scheme** (not line start). Used for patterns that should only match at the beginning of a highlighting region.

**Example**:

```regex
~\s*#include       # Matches #include only at scheme start
```

**Difference from `^`**:
- `^` matches start of line
- `~` matches start of current parsing scheme (may be mid-line)

#### Match Start Marker - `\m`

**Syntax**: `\m`

**Description**: Sets the **start position** of the zero-bracket match (group 0). Allows the match to begin before this position but reports the match starting here.

**Example**:

```regex
\w+\m:              # Match word followed by :, but report match starting at :
# Input: "label:" → Match reports ":label:" but \m sets start at ":"
```

**Use case**: Matching keywords that require lookahead context but should highlight from a specific position.

#### Match End Marker - `\M`

**Syntax**: `\M`

**Description**: Sets the **end position** of the zero-bracket match (group 0).

**Example**:

```regex
:\M\w+              # Match : followed by word, but report match ending at :
```

**Use case**: Highlighting up to a delimiter without including following content.

### 4. Named Backreferences Within Pattern - `\p{name}`

**Syntax**: `\p{name}`

**Description**: Backreference to a **named group within the same pattern** (not cross-pattern).

**Example**:

```regex
(?{quote}["'])[^"']*\p{quote}   # Matches "..." or '...' with same quotes
```

**Equivalent to**:

```regex
(["'])[^"']*\1                  # Numeric backreference version
```

---

## Complete Syntax Reference

### Operator Precedence (Highest to Lowest)

1. **Escape sequences**: `\`, `\d`, `\w`, `\y{name}`, etc.
2. **Character classes**: `[...]`, `[^...]`
3. **Grouping**: `(...)`, `(?{name}...)`, `(?:...)`
4. **Quantifiers**: `*`, `+`, `?`, `{n,m}`, `*?`, `+?`, etc.
5. **Concatenation**: `abc` (implicit)
6. **Alternation**: `|`

### Special Characters That Need Escaping

When used literally, escape these characters: `\ . * + ? | ( ) [ ] { } ^ $ ~`

**Example**:

```regex
\(literal parenthesis\)         # Matches "(literal parenthesis)"
\\backslash                      # Matches "\backslash"
\$dollar                         # Matches "$dollar"
\~tilde                          # Matches "~tilde"
```

### Regular Expression Options

Options can be specified when creating a regex:

```csharp
public enum RegexOptions
{
    None = 0,
    IgnoreCase = 1,      // Case-insensitive matching
    Multiline = 2,       // ^ and $ match line boundaries
    Singleline = 4,      // . matches newlines
    Extended = 8         // Ignore whitespace and allow comments (not fully implemented)
}
```

**Example**:

```csharp
var regex = new ColorerRegex(@"\w+", RegexOptions.IgnoreCase);
```

### Maximum Limits

- **Capture groups**: 10 groups (0-9)
- **Named groups**: 10 named groups
- **Backreference depth**: Unlimited (limited by stack size)
- **Pattern length**: Unlimited (limited by memory)

---

## Usage in HRC Files

HRC (Highlighting Rule Configuration) files use regular expressions extensively to define syntax highlighting rules.

### Basic Block Structure

```xml
<scheme name="MyLanguage">
  <block start="/pattern1/" end="/pattern2/" scheme="Content" region="String"/>
</scheme>
```

### Example: String Highlighting

```xml
<scheme name="StringMatching">
  <!-- Match strings with " or ' delimiters -->
  <block start="/(?{StringEdge}[&quot;&apos;])/"
         end="/\y{StringEdge}/"
         scheme="StringContent"
         region="def:String"/>
</scheme>
```

**Explanation**:
- `start` captures the quote character as named group `StringEdge`
- `end` uses `\y{StringEdge}` to match the **same** quote type
- This ensures `"hello"` and `'world'` match correctly, but not `"mixed'`

### Example: C-Style Comments

```xml
<scheme name="Comment">
  <!-- Single-line comment -->
  <regexp match="~//.*$" region="def:Comment"/>

  <!-- Multi-line comment -->
  <block start="/\/\*/" end="/\*\//"
         scheme="def:Comment"
         region="def:Comment"/>
</scheme>
```

**Explanation**:
- `~` ensures `//` only matches at scheme start (not inside strings)
- `\/\*` escapes `/*` (start of multi-line comment)
- `\*\/` escapes `*/` (end of multi-line comment)

### Example: Heredoc with Custom Delimiter

```xml
<scheme name="Heredoc">
  <!-- PHP heredoc: <<<DELIMITER ... DELIMITER -->
  <block start="/&lt;&lt;&lt;(?{HeredocDelim}\w+)/"
         end="/^~\y{HeredocDelim}$/"
         scheme="StringContent"
         region="def:String"/>
</scheme>
```

**Explanation**:
- `start` captures delimiter name (e.g., `EOF`)
- `end` matches same delimiter at start of line (`~`)
- `\y{HeredocDelim}` ensures matching delimiters

### Entity References in HRC Files

Since HRC files are XML, special characters must be escaped:

| Character | XML Entity | Regex Usage |
|-----------|------------|-------------|
| `<` | `&lt;` | `&lt;` for `<` |
| `>` | `&gt;` | `&gt;` for `>` |
| `&` | `&amp;` | `&amp;` for `&` |
| `"` | `&quot;` | `&quot;` or `&#34;` for `"` |
| `'` | `&apos;` | `&apos;` or `&#39;` for `'` |

**Example**:

```xml
<!-- Match < and > -->
<regexp match="/&lt;tag&gt;/" region="Tag"/>

<!-- Match quotes -->
<regexp match="/(?{quote}[&quot;&apos;]).*?\y{quote}/" region="String"/>
```

---

## Examples

### Example 1: Simple Keyword Matching

**Pattern**: `\b(if|while|for|return)\b`

**Matches**:
- `if (condition)` → matches `if`
- `while (true)` → matches `while`
- `forloop` → no match (word boundary)

```csharp
var regex = new ColorerRegex(@"\b(if|while|for|return)\b");
var match = regex.Match("if (x > 0)");
// match.Value = "if"
```

### Example 2: Email Validation

**Pattern**: `\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b`

**Matches**:
- `user@example.com`
- `john.doe+tag@mail.co.uk`

```csharp
var regex = new ColorerRegex(@"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}\b");
var match = regex.Match("Contact: user@example.com");
// match.Value = "user@example.com"
```

### Example 3: Named Capture Groups

**Pattern**: `(?{year}\d{4})-(?{month}\d{2})-(?{day}\d{2})`

**Matches**: `2025-01-15`

```csharp
var regex = new ColorerRegex(@"(?{year}\d{4})-(?{month}\d{2})-(?{day}\d{2})");
var match = regex.Match("Date: 2025-01-15");

string year = match.GetGroupValue("year");    // "2025"
string month = match.GetGroupValue("month");  // "01"
string day = match.GetGroupValue("day");      // "15"
```

### Example 4: Cross-Pattern Backreferences (String Matching)

**Use case**: Match strings with either `"` or `'` delimiters

```csharp
// Define start pattern that captures the quote
var startRegex = new ColorerRegex(@"(?{quote}["'])");

// Define end pattern that matches the same quote
var endRegex = ColorerRegex.CreateWithBackRE(@"\y{quote}", startRegex);

// Match a double-quoted string
var startMatch = startRegex.Match("\"hello world\"");
// startMatch named group "quote" = '"'

endRegex.SetBackReference("\"hello world\"", startMatch);
var endMatch = endRegex.Match("\"", startIndex: 12);  // Position after "world"
// endMatch.Value = '"' (matches!)

// Try with mismatched quote
var endMismatch = endRegex.Match("'", startIndex: 12);
// endMismatch = null (no match, different quote type)
```

### Example 5: Scheme Anchors

**Pattern**: `~\s*#\w+`

**Matches**: Preprocessor directives only at scheme start

```csharp
var regex = new ColorerRegex(@"~\s*#\w+");

// At scheme start (position 0)
var match1 = regex.Match("  #include <stdio.h>",
                         startIndex: 0,
                         schemeStart: 0);
// match1.Value = "  #include"

// Not at scheme start (position 10)
var match2 = regex.Match("  #include <stdio.h>",
                         startIndex: 10,
                         schemeStart: 0);
// match2 = null (no match, not at scheme start)
```

### Example 6: Greedy vs Non-Greedy

**Pattern comparison**:

```csharp
// Greedy: matches as much as possible
var greedy = new ColorerRegex(@"<.*>");
var greedyMatch = greedy.Match("<tag>content</tag>");
// greedyMatch.Value = "<tag>content</tag>" (entire string)

// Non-greedy: matches as little as possible
var nonGreedy = new ColorerRegex(@"<.*?>");
var nonGreedyMatch = nonGreedy.Match("<tag>content</tag>");
// nonGreedyMatch.Value = "<tag>" (first tag only)
```

### Example 7: Lookahead

**Pattern**: `\w+(?=:)`

**Matches**: Words followed by `:` (but doesn't include `:`)

```csharp
var regex = new ColorerRegex(@"\w+(?=:)");
var match = regex.Match("label: value");
// match.Value = "label" (doesn't include :)
```

### Example 8: Lookbehind

**Pattern**: `(?#4)\w+`

**Matches**: Word preceded by exactly 4 characters

```csharp
var regex = new ColorerRegex(@"(?#4)\w+");
var match = regex.Match("PREFIX_word");
// match.Value = "word" (preceded by "PREF" + "IX_" = 4+3 chars, but match starts after 4)
```

**Note**: Colorer's lookbehind is **fixed-width only**.

### Example 9: Nested Named Groups

**Pattern**: `(?{outer}a(?{inner}b+)c)`

```csharp
var regex = new ColorerRegex(@"(?{outer}a(?{inner}b+)c)");
var match = regex.Match("abbbc");

string outer = match.GetGroupValue("outer");  // "abbbc"
string inner = match.GetGroupValue("inner");  // "bbb"
```

### Example 10: Case-Insensitive Backreferences

**Pattern**: Matching HTML tags (case-insensitive)

```csharp
var startRegex = new ColorerRegex(@"<(?{tag}\w+)>", RegexOptions.IgnoreCase);
var endRegex = ColorerRegex.CreateWithBackRE(@"</\Y{tag}>", startRegex, RegexOptions.IgnoreCase);

var startMatch = startRegex.Match("<DIV>");
// startMatch named group "tag" = "DIV"

endRegex.SetBackReference("<DIV>content</div>", startMatch);
var endMatch = endRegex.Match("</div>", startIndex: 12);
// endMatch.Value = "</div>" (matches despite different case, thanks to \Y)
```

---

## Differences from .NET Regex

### Syntax Differences

| Feature | Colorer Regex | .NET Regex |
|---------|---------------|------------|
| Named groups | `(?{name}...)` | `(?<name>...)` or `(?'name'...)` |
| Named backreference | `\p{name}` | `\k<name>` or `\k'name'` |
| Cross-pattern backreference | `\y{name}`, `\Y{name}` | **Not supported** |
| Scheme anchor | `~` | **Not supported** |
| Match markers | `\m`, `\M` | **Not supported** |
| Lookbehind | `(?#N)`, `(?~N)` (fixed width) | `(?<=...)`, `(?<!...)` (variable width) |
| Inline options | **Not supported** | `(?i)`, `(?-i)`, etc. |
| Comments | **Not supported** | `(?#comment)`, `(?x)` mode |
| Atomic groups | **Not supported** | `(?>...)` |
| Conditionals | **Not supported** | `(?(condition)yes\|no)` |

### Behavior Differences

1. **Capture group limit**: Colorer supports 10 groups (0-9), .NET supports 2 billion+
2. **Unicode support**: Colorer uses 16-bit Unicode (BMP only), .NET supports full Unicode
3. **Performance**: Colorer is optimized for syntax highlighting (millions of matches/sec), .NET is general-purpose
4. **Thread safety**: Colorer regex instances are thread-safe for matching, .NET requires `RegexOptions.Compiled` or static methods
5. **Compilation**: Colorer compiles patterns at construction, .NET can use interpreted or compiled modes

### Features NOT Supported

Colorer does **not** support these .NET regex features:

- ❌ Inline options: `(?i:pattern)`
- ❌ Comments in patterns: `(?#comment)` (in extended mode)
- ❌ Conditional expressions: `(?(condition)yes|no)`
- ❌ Atomic groups: `(?>pattern)`
- ❌ Variable-width lookbehind: `(?<=pattern)`
- ❌ Balancing groups: `(?<name1-name2>pattern)`
- ❌ Unicode categories: `\p{L}`, `\p{Nd}` (different syntax)
- ❌ Right-to-left matching: `RegexOptions.RightToLeft`

---

## Best Practices

### 1. Use Named Groups for Clarity

**Bad**:
```regex
(\w+)@(\w+)\.(\w+)
// Hard to remember: group 1 = user, group 2 = domain, group 3 = TLD
```

**Good**:
```regex
(?{user}\w+)@(?{domain}\w+)\.(?{tld}\w+)
// Clear intent, self-documenting
```

### 2. Prefer Non-Greedy Quantifiers for Delimited Content

**Bad**:
```regex
<tag>.*</tag>
// Matches <tag>foo</tag><tag>bar</tag> as single match
```

**Good**:
```regex
<tag>.*?</tag>
// Matches <tag>foo</tag> and <tag>bar</tag> separately
```

### 3. Use Word Boundaries for Keywords

**Bad**:
```regex
if|while|for
// Matches "if" in "iffy", "while" in "awhile"
```

**Good**:
```regex
\b(if|while|for)\b
// Only matches complete words
```

### 4. Escape Special Characters in HRC Files

**Bad**:
```xml
<regexp match="/<tag>/" region="Tag"/>
<!-- XML parsing error: unescaped < -->
```

**Good**:
```xml
<regexp match="/&lt;tag&gt;/" region="Tag"/>
<!-- Correctly escaped -->
```

### 5. Use Cross-Pattern Backreferences for Paired Delimiters

**Bad** (without backreferences):
```regex
["'].*?["']
// Matches "hello' (mismatched quotes)
```

**Good** (with backreferences):
```csharp
var start = new ColorerRegex(@"(?{q}["'])");
var end = ColorerRegex.CreateWithBackRE(@"\y{q}", start);
// Only matches matching quote pairs
```

### 6. Anchor Patterns When Appropriate

**Bad**:
```regex
#include
// Matches #include anywhere, even in strings or comments
```

**Good**:
```regex
~\s*#include
// Only matches at scheme start
```

### 7. Test Patterns with Edge Cases

Always test regex patterns with:
- Empty strings
- Single characters
- Maximum length input
- Special characters
- Unicode characters
- Nested structures

**Example**:
```csharp
var regex = new ColorerRegex(@"(?{word}\w+)");

// Test cases
Assert.NotNull(regex.Match("a"));           // Single char
Assert.NotNull(regex.Match("Test123"));     // Alphanumeric
Assert.Null(regex.Match(""));               // Empty
Assert.Null(regex.Match("!@#"));            // No word chars
```

### 8. Document Complex Patterns

For HRC files, add XML comments:

```xml
<!-- Matches Python f-strings: f"..." or f'...'
     Captures quote type to ensure matching delimiters -->
<block start="/f(?{quote}[&quot;&apos;])/"
       end="/\y{quote}/"
       scheme="StringContent"
       region="def:String"/>
```

### 9. Performance Considerations

**Avoid catastrophic backtracking**:

```regex
(a+)+b
// Catastrophic backtracking on input "aaaaaaaaac"
```

**Better**:

```regex
a+b
// Simple, efficient
```

**Use anchors to fail fast**:

```regex
~#include
// Fails immediately if not at scheme start
```

### 10. Limit Alternations

**Slow**:
```regex
keyword1|keyword2|keyword3|...|keyword100
```

**Better** (if possible):
```regex
\b[a-z]+\b
// Then check in code if it's a keyword
```

Or use optimized alternation:
```regex
\b(if|for|while)\b
// Only 3 alternatives, okay
```

---

## Appendix: Quick Reference Card

### Metacharacters
```
.       Any character
^       Start of line/string
$       End of line/string
~       Start of scheme (Colorer)
|       Alternation
\       Escape character
```

### Quantifiers
```
*       0 or more (greedy)
+       1 or more (greedy)
?       0 or 1 (greedy)
{n,m}   Between n and m
*?      0 or more (non-greedy)
+?      1 or more (non-greedy)
??      0 or 1 (non-greedy)
```

### Character Classes
```
[abc]   Any of a, b, c
[^abc]  Not a, b, c
[a-z]   Range a to z
\d      Digit
\D      Non-digit
\w      Word character
\W      Non-word character
\s      Whitespace
\S      Non-whitespace
\u      Uppercase
\l      Lowercase
```

### Grouping
```
(...)           Capture group
(?:...)         Non-capturing group
(?{name}...)    Named group (Colorer)
\1-\9           Backreference
\p{name}        Named backreference (Colorer)
```

### Colorer-Specific
```
\yN             Cross-pattern backref (case-sensitive)
\YN             Cross-pattern backref (case-insensitive)
\y{name}        Named cross-pattern backref (case-sensitive)
\Y{name}        Named cross-pattern backref (case-insensitive)
~               Scheme start anchor
\m              Match start marker
\M              Match end marker
```

### Lookaround
```
(?=...)   Positive lookahead
(?!...)   Negative lookahead
(?#N)     Positive lookbehind (N chars)
(?~N)     Negative lookbehind (N chars)
```

### Escapes
```
\t      Tab
\n      Newline
\r      Carriage return
\xNN    Hex character (2 digits)
\x{...} Hex character (variable)
\NNN    Octal character (1-3 digits)
```

---

## See Also

- **UNSAFE_CODE_REGEX.md** - Technical details about the regex engine implementation
- **CLAUDE.md** - Overall project architecture
- [HRC File Format Specification](https://colorer.github.io/hrc/) - Official HRC documentation
- [Colorer Project Homepage](https://github.com/colorer/Colorer-library) - C++ original implementation

---

**Document Maintenance**: This document should be updated when:
- New regex features are added
- Syntax changes are made
- Examples are found to be incorrect
- User feedback identifies unclear sections
