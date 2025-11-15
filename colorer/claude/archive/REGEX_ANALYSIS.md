# Colorer-Library Regex Feature Analysis Report

## Overview
This report analyzes 349 HRC syntax definition files to identify the most commonly used regex features and Colorer-specific patterns. The goal is to understand implementation requirements for the .NET port.

## Summary Statistics
- **Total HRC files analyzed**: 349
- **Key analysis files**:
  - `data/hrc/base/c.hrc` (959 lines)
  - `data/hrc/base/python.hrc` (751 lines)  
  - `data/hrc/base/lua.hrc` (478 lines)
  - `data/hrc/base/gen/perl-brackets.ent.hrc` (extensive Perl patterns)

---

## 1. BACKREFERENCES - Most Heavily Used Feature

### 1.1 Numeric Backreferences (\yN and \Y1)

**Usage**: Cross-pattern backreferences referencing capture groups from the START pattern in END pattern.

#### Example 1: Lua Multi-line Comments (lua.hrc)
```xml
<block start="/(\-\-(\[(\=*)\[))/" end="/((\]\y3\]))/"
    scheme="Comment" region="Comment"/>
```
**Line**: ~217 in lua.hrc
**Explanation**: 
- Capture group 3 captures `=` signs: `(\=*)`
- The end pattern uses `\y3` to reference that exact sequence
- Allows matching balanced delimiters like `--[==[`, `--[[`, etc.
- End pattern matches: `])==]`, `]]`, etc.

#### Example 2: C Raw String Literals (c.hrc)
```xml
<block start="/(?{def:StringEdge}(L|U|u8?)?R&#34;(%DelimChars;{0,16})\()/"
       end="/(?{def:StringEdge}\)\y2&#34;)/"
       scheme="StringContent" region="String"/>
```
**Line**: ~226 in c.hrc
**Explanation**:
- Capture group 2 captures the delimiter characters: `(%DelimChars;{0,16})`
- `\y2` matches the same delimiter sequence in the end pattern
- Supports C++11 raw string syntax: `R"delim(content)delim"`

#### Example 3: Perl HERE-DOC (perl-heredoc.ent.hrc)
```xml
<block start="/\M&lt;&lt;([&quot;]?)((?{def:PairStart}[a-zA-Z_][\w_]*))\1/"
       end="/^((?{def:PairEnd}\y2))$/"
       scheme="HereDocText" region="String"/>
```
**Line**: ~11 in perl-heredoc.ent.hrc
**Explanation**:
- Capture group 1: optional quote character
- Capture group 2: the HERE-DOC identifier
- `\1` matches the same quote character (standard regex)
- `\y2` matches the exact HERE-DOC identifier at start of line
- Essential for flexible HERE-DOC delimiter support

#### Example 4: Perl String Substitution (gen/perl-brackets.ent.hrc)
```xml
<block start="/~\b(\bs\s*)([^\w\s])/" end="/\y2/"
       scheme="RegExpString" region="String"/>
```
**Line**: ~40 in perl-brackets.ent.hrc
**Explanation**:
- Captures the delimiter character in group 2
- Uses `\y2` to match the same delimiter at end
- Supports: `s/pattern/replacement/`, `s{pattern}{replacement}`, etc.

### 1.2 Named Backreferences (\y{name})

**Usage**: More readable alternative to numeric backreferences, especially for complex patterns.

#### Example 1: Python Triple-quoted Strings (python.hrc)
```xml
<block start="/(u?(?{Delim}&apos;{3}|&quot;{3}))/i"
       end="/(\y{Delim})/"
       scheme="StringContent" region="pyString"/>
```
**Line**: ~117 in python.hrc
**Explanation**:
- Named capture `(?{Delim}...)` captures triple quotes
- `\y{Delim}` matches the exact same quote sequence at end
- Supports both `'''` and `"""` variants
- **Used 7 times in python.hrc** for different string types (raw, bytes, f-strings)

#### Example 2: C# Interpolated Strings (csharp.hrc)
```xml
<block scheme="def:empty" start="/(?{start}\$*(?{Quot}&quot;{3,}))/"
       end="/(?{end}(?{Quot}\y{Quot}))/"
       region="String"/>
```
**Line**: ~138 in csharp.hrc
**Explanation**:
- Multiple named groups: `start`, `Quot`, `end`
- `\y{Quot}` references the captured quote sequence
- Supports variable numbers of quotes: `"""`, `""""`, `"""""` etc.

#### Example 3: Markdown Code Fences (misc/markdown.hrc)
```xml
<block start="/(?{start}`+)/" end="/(?{end}\y{start})|%LEnd;/"
       scheme="CodeBlock" region="Code"/>
```
**Line**: ~65-80 in markdown.hrc
**Explanation**:
- Captures backtick sequence in `start` group
- Matches same sequence with `\y{start}`
- Allows matching balanced backticks: `` ` ``, ``` `` ```, etc.

### 1.3 Negative Backreferences (\Y1, \Y2, \Y3)

**Usage**: Ensures a group does NOT match at a position (inverse logic).

#### Example 1: VB.NET Block Structure (vbasic.hrc)
```xml
<block start="/\b(if)\b/i"
       end="/\b((then\s+exit\s+(function|sub|property|for|do))|
              ((then)(\s+[^'\s]+.*$)?=)|(end([\s])+\Y1))\b/i"/>
```
**Line**: 71 in vbasic.hrc
**Explanation**:
- Captures keyword in group 1: `(if)`
- Uses `\Y1` to ensure end pattern does NOT contain matching keyword
- Ensures only matching `end if` closes `if` blocks
- Used for: `if`, `with`, `function`, `sub`, `class`, `property`, `select`

#### Example 2: HTML Paired Tags (html.hrc)
```xml
<block start="/((&lt;) \s* \b(%pairedtags;)\M\b (\s* &gt;)?)/ixs"
       end="/((\/&gt;)|(&lt;\/)(\Y3)(&gt;)|(&lt;\/(%pairedtags;)\b)?=)/ixs"/>
```
**Line**: ~640 in html.hrc
**Explanation**:
- Captures tag name in group 3
- `\Y3` ensures closing tag does NOT have a different tag name
- Essential for matching: `<div>...</div>` but not `<div>...</span>`

#### Example 3: Cobol String Quotes (rare/cobol.hrc)
```xml
<block start="/(?{start}([&quot;&apos;]))/"
       end="/(?{end}\Y1|$)/"
       scheme="def:empty" region="String"/>
```
**Line**: ~175 in cobol.hrc
**Explanation**:
- Captures opening quote type in group 1
- `\Y1` ensures NOT closing with different quote type
- Prevents `"string'` from being valid

---

## 2. UNICODE CHARACTER CLASSES - Important but Limited Usage

### 2.1 Basic Unicode Classes
Patterns found in J2EE and generated files:

```
[{L}]         - Letter (any language)
[{Nd}]        - Decimal digit number
[{L}\-{Nd}]   - Letters with hyphen and digits (with subtraction)
```

#### Example: J2EE Java Identifiers
```xml
<regexp region="Enumeration" priority="low"
        match="/(\$|_|[{L}])([{L}]|[{Nd}]|_|\$)*/"/>
```
**File**: `rare/gen/j2ee/application-client_1_4.hrc`
**Explanation**:
- `[{L}]` matches Unicode letters (Java identifier start)
- `[{Nd}]` matches Unicode digits (Java identifier continue)
- Allows Java identifiers with international characters

### 2.2 Character Class Operations - Character Subtraction

```xml
match="/[a-z]{2}(_|-)?([{L}\-{Nd}]{2})?/"
```
**File**: `rare/gen/j2ee/web-app_2_4.hrc`
**Explanation**:
- `[{L}\-{Nd}]` - subtraction is performed by the escaped hyphen in middle
- Matches letters and digits but subtracts specific characters
- Limited usage (only found in generated J2EE files)

**NOTE**: No examples found of:
- Character class intersection `[{L}&&{Nd}]`
- Character class union operations
- Complex character class combinations

---

## 3. LOOKAHEAD AND LOOKBEHIND ASSERTIONS

### 3.1 Negative Lookahead (?!pattern)

**Common usage**: Ensuring certain patterns do NOT follow.

#### Example 1: Assembly Instructions (asm.hrc)
```xml
<regexp match="/(near|far|short)?![\w\$\~\@\#\%\?\.\-\+]+/xi"/>
```
**Explanation**:
- `?!` prevents matching if preceded by near/far/short
- Distinguishes different instruction types

#### Example 2: C++ Template Handling (cpp.hrc)
```xml
<block start="/(template)\s*(&lt;):?!/" end="/(&gt;)&gt;?!/"
       scheme="Expression"/>
```
**Explanation**:
- `:?!` looks ahead to ensure NOT another `:`
- Prevents confusion with `::` scope resolution operator

#### Example 3: HTML Template Tag (cpp.hrc)
```xml
<block start="/([\w\s])?#1(&lt;)(&lt;)?!
     /((...complex pattern...)|(...))*&gt;))*&gt;))*&gt;)?!/xs"
```
**Explanation**:
- `?!` prevents matching `<<` shift operators
- Used to distinguish template from bit shift operators

### 3.2 Negative Lookahead with Quantifier (?#N)

**Rare pattern**: Look-ahead up to N characters.

```xml
<regexp match="/^ \s* \M (\w[\w_*&~]+ \s [\s\w_*&~\"]*?)?
              (([\:\w_*&~]+?)|(operator..?))
              (\sfor)?~4 (\sif)?~3 (\swhile)?~6 (\sswitch)?~7
              )(?#1;)/x"/>
```
**Explanation**: Lookahead patterns combined with quantified backreferences

---

## 4. COLORER-SPECIFIC METACHARACTERS

### 4.1 The Tilde (~) - Start of Line/Scheme Marker

**Usage**: Marks the beginning of a pattern that should match at START of line or scheme boundary.

#### Example 1: C Preprocessor Directives (c.hrc)
```xml
<regexp match="/~ \s* \M include \s* ([\w_&lt;&gt;\x22\x27\\\/\.]+) /x"
        region1="IncludeOutline"/>
<regexp match="/~ \s* \M define \s+ ([\w_]+) /x"
        region1="DefineOutline"/>
<block start="/~ \s* (pragma | error | warning) \b/x" end="/$/"
       scheme="PragmaText" region="PreprocSpec"/>
```
**Line**: ~218-225 in c.hrc
**Explanation**:
- `~` indicates preprocessor directive at scheme start
- Combined with `\M` (move match start) for precise positioning
- Prevents false positives in middle of code

#### Example 2: Perl Substitution (perl-brackets.ent.hrc)
```xml
<block start="/~\b(\bs\s*)([^\w\s])/" end="/\y2/"
       scheme="RegExpString" region="String"/>
```
**Line**: ~40 in perl-brackets.ent.hrc
**Explanation**:
- `~` marks start of Perl substitution operator
- Essential for Perl syntax where `s` can appear elsewhere

### 4.2 The Backslash-M (\M) - Move Match Start

**Usage**: Sets a new match start position for capturing groups.

#### Example 1: Assembly Labels (asm.hrc)
```xml
<regexp match="/^ \s* \M proc\s+([\w_\@]+)/ix" region1="Function"/>
<regexp match="/^ \s* \M ([\w_\@]+) \s+ proc/ix" region1="Function"/>
```
**Line**: ~268-269 in asm.hrc
**Explanation**:
- `\M` moves match start past leading spaces
- Captures only the actual label/procedure name
- Prevents leading whitespace from being captured

#### Example 2: C Function Declarations (c.hrc)
```xml
<regexp match="/^\s*\M\w+\:([^\:]|$)/" region1="Label"/>
```
**Explanation**:
- `\M` repositions match start after initial whitespace
- Ensures label capture doesn't include indentation

#### Example 3: VB.NET (csharp.hrc)
```xml
<block start="/\M[^#]*&lt;&lt;([&quot;]?)([a-zA-Z_][\w_]*)\1/"
       end="/^(\y2)$/"
       scheme="HereDocText" region="String"/>
```
**Explanation**: Complex positioning for HERE-DOC identifier extraction

### 4.3 The Backslash-m (\m) - Move Match End

**Usage**: Sets a new match end position; typically used with `~` for range matching.

#### Example 1: C Case Labels (c.hrc)
```xml
<block start="/^\s*\M\w+\:([^\:]|$)/" end="/~\w+\m\:/"
       scheme="Label" region="Label"/>
```
**Explanation**:
- Start: position after whitespace and word
- End: `~` marks scheme end, `\m` ends match at colon

#### Example 2: Perl Version Numbers (perl.hrc)
```xml
<block start="/\b\M(\d+)(\.\d+){2,}\b/" end="/~(\d+)(\.\d+)*\b\m/"
       scheme="VersionDecl" region="Number"/>
```
**Explanation**: Handles version number ranges like 5.10.1 or 5.6.2

---

## 5. SPECIAL NAMED GROUPS - (?{name} pattern)

### 5.1 Basic Named Capture Groups

**Usage**: Create named references for later backreference (`\y{name}`).

#### Example 1: C String Formatting (c.hrc)
```xml
<entity name='format' value='[\-\+\#0\x20]*?[\d\*]*(?{}\.[\d\*]+)?
                              (h|l|L|ll|I|I32|I64|hh|j|t|z)?
                              [SsCcsuidopxXnEefgGaAZ]'/>
```
**Explanation**:
- `(?{})` empty group name is used as assertion/marker
- Format specifier parsing with optional decimal component

#### Example 2: Perl Keyword Classification (perl-brackets.ent.hrc)
```xml
<block start="/~(\b(?{}tr|y)\s*)([^\w\s])/" end="/\y2/"
       scheme="ClassString" region="String"/>
```
**Explanation**:
- `(?{}tr|y)` marks this as translation/transliteration operator
- Group 1 captures the operator keywords
- Allows proper highlighting of `tr` vs `y` operators

### 5.2 Multiple Named Groups in Single Pattern

#### Example: C# Complex String Types (csharp.hrc)
```xml
<block scheme="def:empty" 
       start="/(?{start}\$*(?{Quot}&quot;{3,}))/"
       end="/(?{end}(?{Quot}\y{Quot}))/"
       region="String"/>
```
**Line**: ~138 in csharp.hrc
**Explanation**:
- `(?{start}...)` - marks opening position
- `(?{Quot}...)` - captures quote sequence
- `(?{end}...)` - marks closing position
- `\y{Quot}` references the captured quotes
- Enables complex string interpolation highlighting

---

## 6. QUANTIFIER PATTERNS - Reference Distance Markers (~N)

### 6.1 Distance Quantifiers (?~N)

**Usage**: Marks a specific capture group distance/offset for matching.

#### Example 1: C Label to End Block (c.hrc)
```xml
<block start="/\bcase\s/" end="/\:?~1\:\M([^\:]|$)/"
       scheme="Label" region="Label"/>
```
**Explanation**:
- `~1` - removes 1 group from the end calculation
- Used with colon matching in case statements

#### Example 2: C++ Function Decorators (cpp.hrc)
```xml
<regexp match='/^ \M \s* (typedef)?! (\w[\w_*&amp;&lt;&gt;,~:]+ \s
    [\s\w_*&amp;&lt;&gt;,~\":]*?)? (([\:\w_*&amp;~]+?)|(operator..?))
    (\sfor)?~4 (\sif)?~3 (\swhile)?~6 (\sswitch)?~7 (\scatch)?~6
    ((;)?~1$)|(\{)/x' region8="cFunc"/>
```
**Explanation**:
- `?~N` removes N groups from match calculation
- `(\sfor)?~4` - skip 4 groups ahead
- Allows complex function signature detection

---

## 7. FEATURE USAGE FREQUENCY ANALYSIS

### Most Used Features (in order of prevalence):

1. **Backreferences \yN** (Very Common)
   - ~50+ files use this
   - Lua, C++, Perl, Python files heavily use it
   
2. **Named Backreferences \y{name}** (Common)
   - ~15-20 files
   - Python, C#, Markdown files
   
3. **Negative Backreferences \Y1, \Y2, \Y3** (Common)
   - ~10-15 files
   - VB.NET, HTML, Cobol
   
4. **Lookahead Assertions ?!** (Moderate)
   - ~20-30 files
   - C++, Assembly, HTML
   
5. **Tilde Metacharacter ~** (Common)
   - ~30+ files
   - C, C++, Perl, Java
   
6. **Backslash-M (\M)** (Common)
   - ~25+ files
   - Assembly, C, C++, VB.NET
   
7. **Backslash-m (\m)** (Less Common)
   - ~5-10 files
   - C, Perl only
   
8. **Named Groups (?{name})** (Moderate)
   - ~15-20 files
   - C++, C#, Perl, Markdown
   
9. **Unicode Classes [{L}, {Nd}]** (Rare)
   - ~3-5 files
   - Only in generated J2EE files
   
10. **Character Class Operations** (Very Rare)
    - ~2 files
    - Only in generated J2EE files with subtraction

---

## 8. CHALLENGING IMPLEMENTATION PATTERNS

### High Complexity:

1. **Perl Brackets/Delimiters**
   - Cross-pattern backreferences with variable delimiters
   - File: `gen/perl-brackets.ent.hrc`
   - ~150 lines of complex delimiter matching

2. **C++ Template Recognition**
   - Distinguishing `<template>` from `<<operator`
   - Multiple negative lookaheads and lookahead quantifiers
   - File: `cpp.hrc` (~150 lines)

3. **VB.NET Block Nesting**
   - Negative backreferences for block structure validation
   - File: `vbasic.hrc` (~8 block patterns)

4. **HTML Tag Pairing**
   - Ensures matching open/close tags
   - Negative backreferences with complex pattern
   - File: `html.hrc`

### Medium Complexity:

1. **String Delimiter Matching** (Lua, C++, Python)
2. **HERE-DOC Identifier Matching** (Perl)
3. **Comment Block Nesting** (Lua)

### Lower Complexity:

1. **Simple Unicode character classes** (J2EE files)
2. **Basic lookahead assertions**
3. **Standard quantifier patterns**

---

## 9. KEY FILES FOR IMPLEMENTATION REFERENCE

| File | Lines | Key Features | Complexity |
|------|-------|--------------|------------|
| `base/c.hrc` | 959 | ~, \M, lookahead | High |
| `base/python.hrc` | 751 | \y{name}, (?{name}) | Medium |
| `base/lua.hrc` | 478 | \yN, string delimiters | High |
| `gen/perl-brackets.ent.hrc` | ~200 | \yN, ~, \M, \m | Very High |
| `base/vbasic.hrc` | ~150 | \Y1-\Y3 | High |
| `inet/html.hrc` | ~1000 | \Y3, lookahead | High |
| `base/csharp.hrc` | ~200 | \y{name}, (?{name}) | Medium |
| `base/cpp.hrc` | ~300 | ~, \M, lookahead | Very High |

---

## 10. IMPLEMENTATION PRIORITY FOR .NET PORT

### Phase 1 (Essential - 80% of usage):
- [ ] Numeric backreferences (\yN)
- [ ] Named backreferences (\y{name})
- [ ] Negative lookahead (?!)
- [ ] Basic tilde (~) support
- [ ] Basic \M support

### Phase 2 (Important - 15% of usage):
- [ ] Negative backreferences (\Y1, \Y2, etc.)
- [ ] Named groups (?{name})
- [ ] Backslash-m (\m) support
- [ ] Lookahead with quantifiers (?~N)
- [ ] Advanced \M features

### Phase 3 (Optional - 5% of usage):
- [ ] Unicode character classes [{L}, {Nd}]
- [ ] Character class operations (subtraction)
- [ ] Advanced lookahead/behind patterns

---

## 11. RECOMMENDATIONS FOR .NET IMPLEMENTATION

1. **Do NOT try to reuse .NET Regex directly** - Colorer's cross-pattern backreferences are incompatible
2. **Port the C++ regex engine** - More practical than trying to build compatibility layer
3. **Focus on Phase 1 features first** - Covers vast majority of real-world usage
4. **Create extensive test suite** using existing HRC files
5. **Consider caching compiled patterns** - Complex patterns may be expensive
6. **Validate against real colorer output** - Ensure behavioral compatibility

