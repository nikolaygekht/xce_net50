# Colorer Regex Feature Examples - Code Snippets

This document provides exact code snippets extracted from HRC files showing each regex feature in context.

## File 1: Lua Comments with Dynamic Delimiters

**File**: `data/hrc/base/lua.hrc` (Lines 217-218)

```xml
<!-- Multi-line block comments with balanced equals -->
<block start="/(\-\-(\[(\=*)\[))/" 
       end="/((\]\y3\]))/"
       scheme="Comment" 
       region="Comment"/>
```

**Explanation**: Matches `--[=[` comments where the number of equals can vary.
- Capture group 3: `(\=*)` captures 0 or more equals
- `\y3` in end pattern matches the exact same number of equals
- Matches: `--[[...]]`, `--[=[...]=]`, `--[==[...]==]`

---

## File 2: C Raw String Literals

**File**: `data/hrc/base/c.hrc` (Lines 226-227)

```xml
<!-- C++11 raw string literals: R"delim(content)delim" -->
<block start="/(?{def:StringEdge}(L|U|u8?)?R&#34;(%DelimChars;{0,16})\()/"
       end="/(?{def:StringEdge}\)\y2&#34;)/"
       scheme="StringContent" 
       region="String"/>
```

**Explanation**: 
- Group 1: optional prefix `(L|U|u8?)?`
- Group 2: delimiter chars `(%DelimChars;{0,16})` 
- End pattern: `\y2` matches same delimiter
- Examples: `R"xyz(raw\nstring)xyz"`, `R"(normal)"`

---

## File 3: Python Triple-quoted Strings

**File**: `data/hrc/base/python.hrc` (Lines 117-153)

```xml
<!-- Triple-quoted strings with named backreferences -->
<block start="/(u?(?{Delim}&apos;{3}|&quot;{3}))/i" 
       end="/(\y{Delim})/"
       scheme="StringContent" 
       region="pyString"
       inner-region="yes"/>

<!-- Raw triple-quoted strings -->
<block start="/(r(?{Delim}&apos;{3}|&quot;{3}))/i" 
       end="/(\y{Delim})/"
       scheme="RawStringContent" 
       region="pyString"
       inner-region="yes"/>

<!-- Bytes triple-quoted strings -->
<block start="/(b(?{Delim}&apos;{3}|&quot;{3}))/i" 
       end="/(\y{Delim})/"
       scheme="BytesContent" 
       region="pyBytes"
       inner-region="yes"/>

<!-- F-strings (formatted string literals) -->
<block start="/(f(?{Delim}&apos;{3}|&quot;{3}))/i" 
       end="/(\y{Delim})/"
       scheme="FStringContent" 
       region="pyString"
       inner-region="yes"/>

<!-- Combined raw-f-strings -->
<block start="/((?:fr|rf)(?{Delim}&apos;{3}|&quot;{3}))/i" 
       end="/(\y{Delim})/"
       scheme="RawFStringContent" 
       region="pyString"
       inner-region="yes"/>
```

**Explanation**: Uses named backreferences `(?{Delim}...)` and `\y{Delim}` for cleaner patterns with multiple variants.

---

## File 4: Perl Substitution Operators

**File**: `data/hrc/base/gen/perl-brackets.ent.hrc` (Lines 40-49)

```xml
<!-- Perl s/// substitution with variable delimiters -->
<block start="/~\b(\bs\s*)([^\w\s])/" 
       end="/\y2/" 
       scheme="RegExpString"
       region="String"
       region01="ReType"
       region02="StringEdge"
       region10="StringEdge"/>

<!-- Perl tr/// transliteration operator -->
<block start="/~\b(\b(?{}tr|y)\s*)([^\w\s])/" 
       end="/\y2/" 
       scheme="ClassString"
       region="String"
       region01="ReType"
       region02="StringEdge"
       region10="StringEdge"/>

<!-- More complex form with modifiers -->
<block start="/\M((\b(?{}tr|y)\s*)([^\w\s]))/" 
       end="/(\y3)([dsc]*)/"
       scheme="tr.core"
       region="TranslationOp"
       region01="def:PairStart"
       region10="def:PairEnd"
       region11="StringEdge"
       region12="ReModif"/>
```

**Key features**:
- `~` marks start of operator
- Capture group 2 or 3: delimiter character
- `\y2` or `\y3` matches same delimiter at end
- Empty named group `(?{})` marks operator type for syntax highlighting

---

## File 5: VB.NET Block Structure with Negative Backreferences

**File**: `data/hrc/base/vbasic.hrc` (Lines 71-79)

```xml
<!-- IF...END IF block structure -->
<block start="/\b(if)\b/i" 
       end="/\b((then\s+exit\s+(function|sub|property|for|do))|
              ((then)(\s+[^'\s]+.*$)?=)|(end([\s])+\Y1))\b/i"
       scheme="IfBlock"
       region="Block"/>

<!-- WITH...END WITH block structure -->
<block start="/\b(with|function|sub|class|property)\b/i" 
       end="/\b(end([\s])+\Y1)\b/i"
       scheme="WithBlock"
       region="Block"/>

<!-- SELECT...END SELECT block structure -->
<block start="/((\bselect\b)\s+\bcase\b)/i" 
       end="/\b(end([\s])+\Y2)\b/i"
       scheme="SelectBlock"
       region="Block"/>
```

**Explanation**:
- `\Y1` is NEGATIVE backreference
- Ensures `end` keyword is NOT followed by different keyword
- For `if` block: only `end if` closes it, not `end with` or `end select`
- Prevents mismatching of block types

---

## File 6: HTML Paired Tags

**File**: `data/hrc/inet/html.hrc` (Lines ~640-645)

```xml
<!-- HTML paired tags like <div>...</div> -->
<block start="/((&lt;) \s* \b(%pairedtags;)\M\b (\s* &gt;)?)/ixs" 
       end="/((\/&gt;)|(&lt;\/)(\Y3)(&gt;)|(&lt;\/(%pairedtags;)\b)?=)/ixs"
       scheme="html-content"
       region="HtmlTag"
       region00="TagStart"
       region10="TagEnd"/>
```

**Key features**:
- Capture group 3: tag name
- `\Y3` in end pattern: ensures closing tag does NOT have wrong name
- Matches: `<div>...</div>` correctly
- Rejects: `<div>...</span>` as mismatch
- `?=` at end: optional lookahead for self-closing tags

---

## File 7: C Preprocessor Directives

**File**: `data/hrc/base/c.hrc` (Lines 218-225)

```xml
<!-- Preprocessor include directive -->
<regexp match="/~ \s* \M include \s* ([\w_&lt;&gt;\x22\x27\\\/\.]+) /x" 
        region1="IncludeOutline"/>

<!-- Preprocessor define directive -->
<regexp match="/~ \s* \M define \s+ ([\w_]+) /x" 
        region1="DefineOutline"/>

<!-- Preprocessor pragma/error/warning -->
<block start="/~ \s* (pragma | error | warning) \b/x" 
       end="/$/" 
       scheme="PragmaText" 
       region="PreprocSpec" 
       region00="PreprocWord"/>

<!-- Ifdef/endif pairs -->
<regexp match="/~\s*\M(if((n)?def)?)/" region1="def:PairStart"/>
<regexp match="/~\s*\M(endif)/" region1="def:PairEnd"/>
```

**Explanation**:
- `~` marks start of preprocessor directive
- `\M` moves match start past leading whitespace
- Prevents false positives for `#define` inside strings

---

## File 8: C++ Function Signature Detection

**File**: `data/hrc/base/cpp.hrc` (Lines ~330-350)

```xml
<!-- Complex function signature with decorators -->
<regexp match='/^ \M \s* (typedef)?! (\w[\w_*&amp;&lt;&gt;,~:]+ \s
    [\s\w_*&amp;&lt;&gt;,~\":]*?)? (([\:\w_*&amp;~]+?)|(operator..?))
    (\sfor)?~4 (\sif)?~3 (\swhile)?~6 (\sswitch)?~7 (\scatch)?~6
    ((;)?~1$)|(\{)/x' region8="cFunc"/>

<!-- Template handling (not shift operator) -->
<block start="/(template)\s*(&lt;):?!/" 
       end="/(&gt;)&gt;?!/" 
       scheme="Expression"/>
```

**Key features**:
- `\M` moves match start past leading whitespace
- `?!` negative lookahead ensures NOT `::` operator
- `?~N` quantifier removes N groups from calculation
- `?~1`, `?~3`, `?~4`, `?~6` skip ahead in group counting

---

## File 9: Markdown Code Fences

**File**: `data/hrc/misc/markdown.hrc` (Lines ~65-80)

```xml
<!-- Inline code with balanced backticks -->
<block start="/(?{start}`+)/" 
       end="/(?{end}\y{start})|%LEnd;/"
       scheme="InlineCode"
       region="Code"
       region00="def:PairStart"
       region10="def:PairEnd"/>

<!-- Fenced code blocks with fence marker -->
<block start="/(?{start}%x12;%FenceMark;)/" 
       end="/(?{end}%x12;(?{FenceMark}\y{FenceMark})\s*$)/"
       scheme="CodeBlock"
       region="Code"/>

<!-- Bold with balanced markers -->
<block start="/(?{start}\*{2,3})[^\s*]?=/" 
       end="/[^\s*]?#1(?{end}\y{start})|%LEnd;/"
       scheme="def:empty"
       region="Bold"/>

<!-- Italic with balanced underscores -->
<block start="/(?:^|\s?#1)(?{start}_{2,3})[^\s_]?=/" 
       end="/[^\s_]?#1(?{end}\y{start})(?:$|\W?=)|%LEnd;/"
       scheme="def:empty"
       region="Italic"/>
```

**Explanation**:
- Named groups for clarity in complex patterns
- Backticks/stars can repeat: `` ` ``, ``` `` ```, etc.
- `\y{start}` ensures balanced delimiters
- `?#1` ensures only one space/no-break character

---

## File 10: C# String Interpolation

**File**: `data/hrc/base/csharp.hrc` (Lines 136-152)

```xml
<!-- Triple-quoted string with variable prefix -->
<block scheme="def:empty" 
       start="/(?{start}\$*(?{Quot}&quot;{3,}))/" 
       end="/(?{end}(?{Quot}\y{Quot}))/"
       region="String"
       region00="def:PairStart"
       region10="def:PairEnd"
       region01="StringEdge"
       region11="StringEdge"/>

<!-- Interpolated string literal @"..." -->
<block scheme="IStringContent" 
       start="/(?{start}(?{Quot}\$@&quot;))/" 
       end="/(?{end}(?{Quot}&quot;))/"
       region="String"
       region00="def:PairStart"
       region10="def:PairEnd"
       region01="StringEdge"
       region11="StringEdge"/>

<!-- Verbatim string @"..." -->
<block scheme="VStringContent" 
       start="/(?{start}(?{Quot}@&quot;))/" 
       end="/(?{end}(?{Quot}&quot;))/"
       region="String"
       region00="def:PairStart"
       region10="def:PairEnd"
       region01="StringEdge"
       region11="StringEdge"/>
```

**Features**:
- Multiple named groups: `start`, `Quot`, `end`
- `(?{Quot}...)` captures exact quote sequence
- `\y{Quot}` matches same quotes at end
- Supports `$"`, `@"`, `$@"` prefixes

---

## File 11: Perl Heredoc Documents

**File**: `data/hrc/base/perl-heredoc.ent.hrc` (Lines 10-18)

```xml
<!-- HERE-DOC with optional quotes -->
<block start="/\M&lt;&lt;([&quot;]?)((?{def:PairStart}[a-zA-Z_][\w_]*))\1/" 
       end="/^((?{def:PairEnd}\y2))$/"
       scheme="HereDocText"
       region="String"
       region00="def:PairStart"
       region01="StringEdge"
       region02="def:PairStart"
       region10="def:PairEnd"
       region11="StringEdge"/>

<!-- HERE-DOC with backticks -->
<block start="/\M&lt;&lt;(`)((?{def:PairStart}[a-zA-Z_][\w_]*))\1/" 
       end="/^((?{def:PairEnd}\y2))$/"
       scheme="HereDocText"
       region="String"
       region00="def:PairStart"
       region01="StringEdge"
       region02="def:PairStart"
       region10="def:PairEnd"
       region11="StringEdge"/>

<!-- Simplified HERE-DOC (no quotes) -->
<block start="/^\M[^#]*&lt;&lt;([&quot;]?)([a-zA-Z_][\w_]*)\1/" 
       end="/^(\y2)$/"
       scheme="HereDocText"
       region="String"/>
```

**Key features**:
- `\M` moves match start to skip leading code
- `\1` standard backreference for optional quote
- `\y2` Colorer backreference to HERE-DOC identifier
- Allows flexible delimiters: `<<TERM`, `<<"TERM"`, `<<'TERM'`

---

## File 12: Assembly Labels and Jumps

**File**: `data/hrc/base/asm.hrc` (Lines 268-280)

```xml
<!-- Procedure definition with leading whitespace handling -->
<regexp match="/^ \s* \M proc\s+([\w_\@]+)/ix" region1="Function"/>
<regexp match="/^ \s* \M ([\w_\@]+) \s+ proc/ix" region1="Function"/>

<!-- Labels with possible double @@ prefix -->
<regexp match="/(^\s*?\@?\@?[\w\$\#\%\@\~\.\?]+?\s*:)/"/>
<regexp match="/\B(\@\@[\w\$\#\%\@\~\.\?]*)/" region0="asmLabel"/>

<!-- Comment block with balanced delimiters -->
<block start="/(COMMENT) (.)/i" 
       end="/\y2/" 
       scheme="Comment" 
       region="asmComment"
       region01="asmDefinition"
       region02="asmDefinition"
       region00="PairStart"
       region10="PairEnd"/>
```

**Explanation**:
- `\M` positions match start after whitespace
- `\y2` references captured delimiter in group 2
- Supports multiple label formats with prefixes

---

## Pattern Complexity Ranking

### Simplest (Easy to implement):
1. Basic lookahead `?!`
2. Named groups `(?{name})`
3. Simple backreferences `\y1`, `\y2`

### Medium Complexity:
1. Named backreferences `\y{name}`
2. Position markers `\M`
3. Multiple named groups

### Complex (Hard to implement):
1. Negative backreferences `\Y1`, `\Y2`
2. Quantifier offsets `?~N`
3. Scheme boundaries with `~`
4. Combined features in single pattern

### Very Complex (Requires full engine rewrite):
1. Cross-pattern backreferences (start vs end pattern)
2. Variable-position matching
3. Multiple interdependent features

---

## Testing Strategy

To verify .NET port regex engine:

1. **Extract 20-30 representative patterns** from examples above
2. **Create unit tests** matching expected behavior
3. **Test with real Colorer** to get expected highlights
4. **Verify against .NET port output** 
5. **Check edge cases** like empty matches, multiple groups

Key test files:
- `/data/hrc/base/c.hrc` - high complexity
- `/data/hrc/base/python.hrc` - moderate complexity
- `/data/hrc/base/lua.hrc` - balanced features
- `/data/hrc/base/gen/perl-brackets.ent.hrc` - complex edge cases

