# Test Cases for Uncovered Lines in CRegExpMatcher

## Overview
This document contains test cases for lines in `CRegExpMatcher.cs` that are not currently covered by tests.
Each test case includes: pattern, input, expected result, explanation, and references to HRC file usage where applicable.

---

## Category 1: LowParse Entry with null re parameter (Lines 198-201)

**Code:**
```csharp
if (re == null)
{
    re = prev->parent;
    leftenter = false;
}
```

**Context:** This handles when `LowParse` is called with `re == null`, which happens during backtracking.

### Test Case 1.1: Alternation with backtracking
**Pattern:** `(a|b)c`
**Input:** `"bc"`
**Expected:** Match `"bc"` (index 0, length 2)
**Explanation:** When trying first alternative `a`, it fails. Backtracking calls LowParse with `re=null` to try second alternative `b`.
**HRC Reference:** Common in language definitions with alternatives

---

## Category 2: Empty Match Edge Case (Lines 238, 255, 262)

**Code:**
```csharp
if (eArr[idx] < sArr[idx])
    sArr[idx] = eArr[idx];
```

**Context:** Handles empty capture groups where end position < start position.

### Test Case 2.1: Empty capture group
**Pattern:** `(a*)`
**Input:** `"b"`
**Expected:** Match `""` at position 0, group 1 captures empty string
**Explanation:** `a*` matches zero `a`s, creating empty match where `e < s` initially
**HRC Reference:** Used in optional matching patterns

### Test Case 2.2: Empty named capture group
**Pattern:** `(?{name}b*)`
**Input:** `"a"`
**Expected:** Match `""` at position 0, named group captures empty string
**Explanation:** Named group with zero matches
**HRC Reference:** Named groups in syntax definitions

---

## Category 3: COLORERMODE Backreferences (Lines 329-439)

These are cross-pattern backreferences used in Colorer syntax highlighting.

### Test Case 3.1: ReBkTrace - \yN (numeric backreference to another RE)
**Pattern (RE1):** `(COMMENT) (.)`
**Pattern (RE2):** `\y2`
**Setup:** First pattern captures delimiter in group 2
**Input (RE1):** `"COMMENT #"`
**Input (RE2):** `"#"`
**Expected (RE2):** Match `"#"`
**Explanation:** `\y2` references group 2 from the *previous* regex execution (backTrace)
**HRC Reference:** `asm.hrc` line 10: `<block start="/(COMMENT) (.)/i" end="/\y2/"`
**Note:** Requires `SetBackTrace()` to be called

### Test Case 3.2: ReBkTrace - Multi-character delimiter
**Pattern (RE1):** `(BEGIN) (.*)`
**Pattern (RE2):** `\y2`
**Input (RE1):** `"BEGIN END"`
**Input (RE2):** `"END"`
**Expected (RE2):** Match `"END"`
**Explanation:** Backreference to multi-character capture
**HRC Reference:** Common in block comment patterns

### Test Case 3.3: ReBkTraceN - \YN (case-insensitive cross-pattern backreference)
**Pattern (RE1):** `(COMMENT) (.)`
**Pattern (RE2):** `\Y2`
**Input (RE1):** `"COMMENT #"`
**Input (RE2):** `"#"` (should match case-insensitively)
**Expected (RE2):** Match `"#"`
**Explanation:** Case-insensitive version of `\y`
**Note:** Requires `SetBackTrace()`

### Test Case 3.4: ReBkTraceName - \y{name} (named cross-pattern backreference)
**Pattern (RE1):** `(?{delim}.)`
**Pattern (RE2):** `\y{delim}`
**Input (RE1):** `"#"`
**Input (RE2):** `"#"`
**Expected (RE2):** Match `"#"`
**Explanation:** Named group backreference across patterns
**Note:** Requires `SetBackTrace()` with named group storage

### Test Case 3.5: ReBkTraceNName - \Y{name} (case-insensitive named cross-pattern)
**Pattern (RE1):** `(?{delim}.)`
**Pattern (RE2):** `\Y{delim}`
**Input (RE1):** `"A"`
**Input (RE2):** `"a"` (case-insensitive)
**Expected (RE2):** Match `"a"`
**Explanation:** Case-insensitive named group backreference
**Note:** Requires `SetBackTrace()`

### Test Case 3.6: ReBkTrace - Failure case (no backTrace set)
**Pattern:** `\y1`
**Input:** `"test"`
**Expected:** No match
**Explanation:** When `SetBackTrace()` not called, backreference fails

### Test Case 3.7: ReBkTrace - Failure case (invalid group)
**Pattern (RE1):** `(a)`
**Pattern (RE2):** `\y9`
**Input (RE2):** `"a"`
**Expected:** No match
**Explanation:** Referenced group doesn't exist in backTrace

---

## Category 4: Named Group Backreferences (Lines 468-493)

**Code:** `ReBkBrackName` - backreferences to named groups within same pattern

### Test Case 4.1: Named backreference - basic
**Pattern:** `(?{word}\w+)\s+\k{word}`
**Input:** `"hello hello"`
**Expected:** Match `"hello hello"`
**Explanation:** Named group captured, then referenced
**Note:** This uses deprecated storage (ns/ne arrays)

### Test Case 4.2: Named backreference - no match
**Pattern:** `(?{word}\w+)\s+\k{word}`
**Input:** `"hello world"`
**Expected:** No match
**Explanation:** Backreference doesn't match different word

### Test Case 4.3: Named backreference - empty group
**Pattern:** `(?{opt}a?)\k{opt}`
**Input:** `""`
**Expected:** Match `""`
**Explanation:** Empty named group matches empty backreference

---

## Category 5: Lookbehind Assertions (Lines 520-548)

### Test Case 5.1: ReBehind - Positive lookbehind (?<=...)
**Pattern:** `(?<=@)\w+`
**Input:** `"@username"`
**Expected:** Match `"username"` at index 1
**Explanation:** Matches word preceded by `@`
**HRC Reference:** Used for context-sensitive matching

### Test Case 5.2: ReBehind - At start (should fail)
**Pattern:** `(?<=a)b`
**Input:** `"b"`
**Expected:** No match
**Explanation:** Lookbehind requires character before position 0, fails

### Test Case 5.3: ReBehind - Multi-character lookbehind
**Pattern:** `(?<=abc)\d+`
**Input:** `"abc123"`
**Expected:** Match `"123"` at index 3
**Explanation:** Lookbehind with fixed width

### Test Case 5.4: ReBehind - Failure due to insufficient length
**Pattern:** `(?<=abc)x`
**Input:** `"abx"`
**Expected:** No match
**Explanation:** `toParse - param0 < 0` condition (line 525-528)

### Test Case 5.5: ReNBehind - Negative lookbehind (?<!...)
**Pattern:** `(?<!@)\w+`
**Input:** `"username"`
**Expected:** Match `"username"` at index 0
**Explanation:** Matches word NOT preceded by `@`

### Test Case 5.6: ReNBehind - At start (should match)
**Pattern:** `(?<!a)b`
**Input:** `"b"`
**Expected:** Match `"b"`
**Explanation:** At position 0, lookbehind offset < 0, so negative lookbehind succeeds (line 541-547)

### Test Case 5.7: ReNBehind - Should fail when pattern found
**Pattern:** `(?<!@)\w+`
**Input:** `"@user"`
**Expected:** Match `"user"` (not `"@user"`)
**Explanation:** First position fails lookbehind, matches later

---

## Category 6: Multiline Mode EOL (Lines 791-792)

**Code:**
```csharp
if (multiLine)
{
    if (toParse > 0 && toParse < end && IsLineTerminator(globalPattern![toParse - 1]))
        return true;
}
```

### Test Case 6.1: $ in multiline mode - mid-string after newline
**Pattern:** `a$` (with multiline flag)
**Input:** `"a\nb"`
**Expected:** Match `"a"` at index 0
**Explanation:** In multiline mode, `$` matches after line terminators
**HRC Reference:** Used extensively in line-based syntax rules

### Test Case 6.2: $ in multiline mode - internal newline
**Pattern:** `.$` (with multiline flag)
**Input:** `"x\ny"`
**Expected:** Match `"x"` at index 0, then `"y"` at index 2
**Explanation:** Tests `toParse > 0 && toParse < end` condition

---

## Category 7: Non-Digit Metacharacter (Lines 803-806)

**Code:** `ReNDigit` - matches non-digits

### Test Case 7.1: \D matches letter
**Pattern:** `\D+`
**Input:** `"abc"`
**Expected:** Match `"abc"`
**Explanation:** All letters are non-digits

### Test Case 7.2: \D stops at digit
**Pattern:** `\D+`
**Input:** `"abc123"`
**Expected:** Match `"abc"`
**Explanation:** Non-digit sequence stops at first digit

### Test Case 7.3: \D at end of string
**Pattern:** `\D`
**Input:** `"1"`
**Expected:** No match (toParse >= end after consuming '1')
**Explanation:** Tests end-of-string condition

---

## Category 8: Non-Word Character Metacharacter (Lines 814-818)

**Code:** `ReNWordSymb` - matches non-word characters

### Test Case 8.1: \W matches space
**Pattern:** `\W+`
**Input:** `"hello world"`
**Expected:** Match `" "` at index 5
**Explanation:** Space is not a word character

### Test Case 8.2: \W matches punctuation
**Pattern:** `\W+`
**Input:** `"hello, world"`
**Expected:** Match `", "` at index 5
**Explanation:** Comma and space are non-word chars

### Test Case 8.3: \W should not match underscore
**Pattern:** `\W`
**Input:** `"_"`
**Expected:** No match
**Explanation:** Underscore IS a word character (tests `|| globalPattern[toParse] == '_'`)

### Test Case 8.4: \W at end of string
**Pattern:** `a\W`
**Input:** `"a"`
**Expected:** No match
**Explanation:** Tests `toParse >= end` condition

---

## Category 9: Non-Whitespace Metacharacter (Lines 827-830)

**Code:** `ReNWSpace` - matches non-whitespace

### Test Case 9.1: \S matches letters
**Pattern:** `\S+`
**Input:** `"hello world"`
**Expected:** Match `"hello"` at index 0
**Explanation:** Letters are non-whitespace

### Test Case 9.2: \S matches digits
**Pattern:** `\S+`
**Input:** `"123 456"`
**Expected:** Match `"123"` at index 0
**Explanation:** Digits are non-whitespace

### Test Case 9.3: \S at end
**Pattern:** `\s\S`
**Input:** `" "`
**Expected:** No match
**Explanation:** Tests `toParse >= end` after whitespace

---

## Category 10: Uppercase/Lowercase Metacharacters (Lines 832-841)

### Test Case 10.1: \u matches uppercase
**Pattern:** `\u+`
**Input:** `"HELLO"`
**Expected:** Match `"HELLO"`
**Explanation:** `ReUCase` matches uppercase letters
**Note:** Not standard regex, Colorer-specific

### Test Case 10.2: \u stops at lowercase
**Pattern:** `\u+`
**Input:** `"HELLOworld"`
**Expected:** Match `"HELLO"`
**Explanation:** Uppercase sequence stops

### Test Case 10.3: \U matches lowercase (ReNUCase)
**Pattern:** `\U+`
**Input:** `"hello"`
**Expected:** Match `"hello"`
**Explanation:** `ReNUCase` matches lowercase (note: inverted logic in code)
**Note:** Line 839 has `!Character.IsLowerCase` which seems like a bug - should be IsUpperCase

### Test Case 10.4: \u at end
**Pattern:** `a\u`
**Input:** `"a"`
**Expected:** No match
**Explanation:** Tests end condition

---

## Category 11: COLORERMODE Metacharacters (Lines 856-869)

### Test Case 11.1: ~ (tilde) - Start of scheme (ReSoScheme)
**Pattern:** `~`
**Input:** `"test"`
**schemeStart:** 0
**Expected:** Match at position 0
**Explanation:** `~` matches the scheme start position
**HRC Reference:** `c.hrc` extensively: `/~ \s* \M include/`

### Test Case 11.2: ~ mid-string
**Pattern:** `test~`
**Input:** `"test"`
**schemeStart:** 4
**Expected:** Match `"test"` (~ matches at position 4)
**Explanation:** Scheme can start mid-string
**HRC Reference:** Used in preprocessor directives

### Test Case 11.3: \m - Set new start marker (ReStart)
**Pattern:** `a\mb`
**Input:** `"ab"`
**Expected:** Match `"ab"`, but match[0].start is set to position 1 (at 'b')
**Explanation:** `\m` modifies where the match "starts"
**HRC Reference:** `c.hrc`: `/~\w+\m\:/` (for labels)

### Test Case 11.4: \M - Set new end marker (ReEnd)
**Pattern:** `a\Mb`
**Input:** `"ab"`
**Expected:** Match `"ab"`, but match[0].end is set to position 1 (at 'a')
**Explanation:** `\M` modifies where the match "ends"
**HRC Reference:** `c.hrc`: `/\M include/`, `/\M define/`

### Test Case 11.5: Combined \m and \M
**Pattern:** `a\mb\Mc`
**Input:** `"abc"`
**Expected:** Match `"abc"`, start at 1, end at 2
**Explanation:** Both start and end are modified
**HRC Reference:** Common in C labels and keywords

---

## Category 12: QuickCheck Optimization (Lines 973-1001)

### Test Case 12.1: QuickCheck with firstChar (case-sensitive)
**Pattern:** `abc`
**Input:** `"abc"`
**positionMoves:** false
**Expected:** Match `"abc"`
**Explanation:** First char 'a' matches, quick check passes

### Test Case 12.2: QuickCheck with firstChar (case-insensitive)
**Pattern:** `abc` (ignoreCase=true)
**Input:** `"ABC"`
**positionMoves:** false
**Expected:** Match `"ABC"`
**Explanation:** Tests case-insensitive quick check (lines 977-980)

### Test Case 12.3: QuickCheck with firstChar fails
**Pattern:** `abc`
**Input:** `"xyz"`
**positionMoves:** false
**Expected:** No match
**Explanation:** Quick rejection without parsing

### Test Case 12.4: QuickCheck at end of string
**Pattern:** `abc`
**Input:** `""`
**positionMoves:** false
**Expected:** No match
**Explanation:** Tests `toParse >= end` (lines 975-976)

### Test Case 12.5: QuickCheck with firstMetaChar (ReSoL)
**Pattern:** `^abc`
**positionMoves:** false
**toParse:** 0
**Expected:** Match if at position 0
**Explanation:** Tests metachar optimization (lines 993-994)

### Test Case 12.6: QuickCheck with ReSoScheme
**Pattern:** `~abc`
**schemeStart:** 5
**positionMoves:** false
**toParse:** 5
**Expected:** Match if at schemeStart
**Explanation:** Tests scheme start optimization (lines 995-996)

---

## Category 13: Stack Expansion (Lines 929-940)

### Test Case 13.1: Deep backtracking requiring stack expansion
**Pattern:** `(a|b)(c|d)(e|f)(g|h)(i|j)(k|l)(m|n)(o|p)(q|r)(s|t)`
**Input:** `"zttttttttt"` (all alternatives fail)
**Expected:** No match, but should expand stack during backtracking
**Explanation:** Tests stack reallocation when `regExpStackSize == countElem`
**Note:** Initial stack is 512, need deep backtracking to trigger expansion

### Test Case 13.2: Catastrophic backtracking pattern (bounded)
**Pattern:** `(a+)+b`
**Input:** `"aaaaaaaaaaaaaaaaaaaaaaaac"` (no 'b' at end)
**Expected:** No match
**Explanation:** Extreme backtracking that fills stack
**Note:** Should complete without crash, may need stack expansion

---

## Summary of Test Categories

1. **Null re parameter** (198-201): 1 test
2. **Empty capture groups** (238, 255, 262): 2 tests
3. **COLORERMODE backreferences** (329-439): 7 tests
4. **Named group backreferences** (468-493): 3 tests
5. **Lookbehind** (520-548): 7 tests
6. **Multiline EOL** (791-792): 2 tests
7. **Non-digit** (803-806): 3 tests
8. **Non-word char** (814-818): 4 tests
9. **Non-whitespace** (827-830): 3 tests
10. **Upper/lowercase** (832-841): 4 tests
11. **COLORERMODE metacharacters** (856-869): 5 tests
12. **QuickCheck optimization** (973-1001): 6 tests
13. **Stack expansion** (929-940): 2 tests

**Total: 49 test cases**

---

## Priority Notes

**High Priority:**
- COLORERMODE features (\y, \Y, ~, \m, \M) - These are unique to Colorer and heavily used in HRC files
- Lookbehind assertions - Standard regex feature
- Metacharacters (\D, \W, \S, \u, \U) - Core functionality

**Medium Priority:**
- QuickCheck optimization - Performance feature
- Empty captures - Edge case handling
- Named backreferences (deprecated storage)

**Low Priority (but still test):**
- Stack expansion - Stress testing
- Null re parameter - Internal implementation detail

---

## Implementation Notes

1. **SetBackTrace() method**: Tests for \y, \Y, \y{name}, \Y{name} require calling `SetBackTrace()` with a previous match result
2. **RegexOptions**: Some tests need custom options (multiline, ignoreCase, singleLine)
3. **positionMoves**: QuickCheck tests need `posMovesOverride=false` parameter
4. **schemeStart**: COLORERMODE tests need non-zero schemeStart parameter
5. **Thread safety**: Consider concurrent tests for matcher lock behavior

---

## Potential Bugs Found

**Line 839:** `ReNUCase` implementation
```csharp
case EMetaSymbols.ReNUCase:
    if (toParse >= end || !Character.IsLowerCase(globalPattern![toParse]))
        return false;
```

This checks `!IsLowerCase` for `ReNUCase` (not uppercase). The naming suggests:
- `ReUCase` = uppercase
- `ReNUCase` = NOT uppercase (i.e., everything except uppercase)

But the implementation checks `!IsLowerCase` which would match uppercase + non-letters.
This seems suspicious and should be verified against C++ source.

**Expected behavior:**
- `\u` should match uppercase letters only
- `\U` should match anything that is NOT uppercase (lowercase + non-letters)

Current implementation may be correct for Colorer semantics, but needs verification.
