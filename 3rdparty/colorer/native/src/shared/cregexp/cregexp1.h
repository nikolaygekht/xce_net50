#ifndef __CREGEXP__
#define __CREGEXP__

#include<unicode/String.h>
#include<unicode/CharacterClass.h>

/**
    @addtogroup cregexp Regular Expressions
      Colorer Regular Expressions (cregexp) class implementation.
*/

/// with this define class uses extended command set for
/// colorer compatibility mode
/// if you undef it, it will compile stantard set for
/// regexp compatibility mode
#define COLORERMODE

/// use hashes for saving named brackets
//#define NAMED_MATCHES_IN_HASH

/// check duplicate brackets
//#define CHECKNAMES

#if defined COLORERMODE && defined NAMED_MATCHES_IN_HASH
#error COLORERMODE && NAMED_MATCHES_IN_HASH not realyzed yet
#endif

/// numeric matches num
#define MATCHES_NUM 0x10

#if !defined NAMED_MATCHES_IN_HASH
// number of named brackets (access through SMatches1.ns)
#define NAMED_MATCHES_NUM 0x10
#endif

#ifdef NAMED_MATCHES_IN_HASH
struct SMatch1{
  int s,e;
};
// you can redefine this class
typedef class SMatchHash1 {
public:
  SMatch1 *setItem(const String *name, SMatch1 &SMatch1){return null;};
  SMatch1 *getItem(const String *name){return null;};
}*PMatchHash;
#endif


enum EOps1
{
  ReBlockOps,
  ReMul,              // *
  RePlus,             // +
  ReQuest,            // ?
  ReNGMul,            // *?
  ReNGPlus,           // +?
  ReNGQuest,          // ??
  ReRangeN,           // {n,}
  ReRangeNM,          // {n,m}
  ReNGRangeN,         // {n,}?
  ReNGRangeNM,        // {n,m}?
  ReOr,               // |
  ReBehind,           // ?#n
  ReNBehind,          // ?~n
  ReAhead,            // ?=
  ReNAhead,           // ?!

  ReSymbolOps,
  ReEmpty,
  ReMetaSymb,         // \W \s \d ...
  ReSymb,             // a b c ...
  ReWord,             // word...
  ReEnum,             // []
  ReNEnum,            // [^]
  ReBrackets,         // (...)
  ReNamedBrackets,    // (?{name} ...)
#ifdef COLORERMODE
  ReBkTrace,          // \yN
  ReBkTraceN,         // \YN
  ReBkTraceName,      // \y{name}
  ReBkTraceNName,     // \Y{name}
#endif
  ReBkBrack,          // \N
  ReBkBrackName       // \p{name}
};

enum EMetaSymbols1
{
  ReBadMeta,
  ReAnyChr,           // .
  ReSoL,              // ^
#ifdef COLORERMODE
  ReSoScheme,         // ~
#endif
  ReEoL,              // $
  ReDigit,            // \d
  ReNDigit,           // \D
  ReWordSymb,         // \w
  ReNWordSymb,        // \W
  ReWSpace,           // \s isWhiteSpace()
  ReNWSpace,          // \S
  ReUCase,            // \u
  ReNUCase,           // \l
  ReWBound,           // \b
  ReNWBound,          // \B
  RePreNW,            // \c
  ReCrLf,             // \n
  ReNCrLf,            // \N
#ifdef COLORERMODE
  ReStart,            // \m
  ReEnd,              // \M
#endif

  ReChrLast,
};

enum EError1
{
  EOK = 0, EError, ESYNTAX, EBRACKETS, EENUM, EOP
};


/// @ingroup cregexp
struct SMatches1
{
  int s[MATCHES_NUM];
  int e[MATCHES_NUM];
  int cMatch;
#if !defined NAMED_MATCHES_IN_HASH
  int ns[NAMED_MATCHES_NUM];
  int ne[NAMED_MATCHES_NUM];
  int cnMatch;
#endif
};

/** Regular expressions internal tree node.
    @ingroup cregexp
*/
class SRegInfo1
{
public:
  SRegInfo1();
  ~SRegInfo1();

#include<common/MemoryOperator.h>

  union{
    EMetaSymbols1 metaSymbol;
    wchar symbol;
    String *word;
    CharacterClass *charclass;
    SRegInfo1 *param;
  }un;
#if defined NAMED_MATCHES_IN_HASH
  String *namedata;
#endif
  SRegInfo1 *parent;
  SRegInfo1 *next;
  SRegInfo1 *prev;
  int oldParse;
  int param0, param1;
  int s, e;

  EOps1 op;
};

struct StackElem1{
  //local variable
  SRegInfo1 *re;
  SRegInfo1 *prev;
  int toParse;
  bool leftenter;
  // step if function return true
  int ifTrueReturn;
  // step if function return false
  int ifFalseReturn;
};

extern StackElem1 *RegExpStack1;
extern int RegExpStack_Size1;

#define INIT_MEM_SIZE 512
#define MEM_INC 128

enum ReAction1 {
  rea_False=0,
  rea_True=1,
  rea_Break,
  rea_RangeNM_step2,
  rea_RangeNM_step3,
  rea_RangeN_step2,
  rea_NGRangeN_step2,
  rea_NGRangeNM_step2,
  rea_NGRangeNM_step3
};
/** Regular Expression compiler and matcher.
    Colorer regular expressions library cregexp.

\par 1. Features.

\par 1.1. Colorer Unicode classes.
   - Unicode Consortium regexp level 1 support.
     All characters are treated as independent 16-bit units.
     The result of RE is independent of current locale.
   - Unicode syntax extensions:
     - Unicode general category char class:
         - [{L}{Nd}] - all letters and decimal digits,
         - [{ALL}]   - as '.',
         - [{ASSIGNED}] - all assigned unicode characters,
         - [{UNASSIGNED}] - all unassigned unicode characters.
     - Char classes substraction unicode extension:
         - [{ASSIGNED}-[{Lu}]-[{Ll}]] - all assigned characters except,
         - upper and lower case characters.
     - Char classes connection syntax:
         - [{Lu}[{Ll}]] - upper and lower case characters.
     - Char classes intersection syntax:
         - [{ALL}&&[{L}]] - only Letter characters.
     - Character reference syntax: \\x{2028} \\x0A as in Perl.
     - Unicode form \\u2028 is unused (\\u - upper case char).

\par 1.2. Extensions.
   - Bracket extensions:
     - (?{name} pattern ) - named bracket,
     - \\p{name} - named bracket reference.
     - (?{} pattern ) - no capturing bracket as (?: pattern ) in Perl.
   - Look Ahead/Backward:
     - pattern?=  as Perl's (?=pattern)
     - pattern?!  as Perl's (?!pattern)
     - pattern?#N - N symbols backward look for pattern
     - pattern?~N - N symbols backward look for no pattern
   - Colorer library extensions:
     - \\m \\M - sets new start and end of zero(default) bracket.
     - \\yN \\YN \\y{name} \\Y{name} - back reference into another RE's bracket.

\par 1.3. Perl compatibility.
   - Modifiers //ismx
   - \\ p{name} - back reference to named bracket (but not named property as in Perl!)
   - No POSIX character classes support.



\par 2. Dislikes:

\par 2.1. According to Unicode RE level 1 support:
   - No surrogate symbols support,
   - No string length changes on case mappings (only 1 <-> 1 mappings),
\par 2.2. Algorithmic problems:
   - Stack recursion implementation.

    @ingroup cregexp
*/
class CRegExp1
{
public:
  /**
    Empty constructor. No RE tree is builded with this constructor.
    Use #setRE method to change pattern.
  */
  CRegExp1();
  /**
    Constructs regular expression and compile it with @c text pattern.
  */
  CRegExp1(const String *text);
  ~CRegExp1();

  /**
    Is compilied RE well-formed.
  */
  bool isOk();

  /**
    Returns information about RE compilation error.
  */
  EError1 getError();

  /**
    Tells RE parser, that it must make moves on tested string while RE matching.
  */
  bool setPositionMoves(bool moves);
  /**
    Returns count of named brackets.
  */
  int getBracketNo(const String *brname);
  /**
    Returns named bracked name by it's index.
  */
  String *getBracketName(int no);
#ifdef COLORERMODE
  bool setBackRE(CRegExp1 *bkre);
  /**
    Changes RE object, used for backreferences with named \y{} \Y{} operators.
  */
  bool setBackTrace(const String *str, SMatches1 *trace);
  /**
    Returns current RE object, used for backreferences with \y \Y operators.
  */
  bool getBackTrace(const String **str, SMatches1 **trace);
#endif
  /**
    Compiles specified regular expression and drops all
    previous structures.
  */
  bool setRE(const String *re);
#ifdef NAMED_MATCHES_IN_HASH
  /** Runs RE parser against input string @c str
  */
  bool parse(const String *str, SMatches1 *mtch, SMatchHash1 *nmtch = null);
  /** Runs RE parser against input string @c str
  */
  bool parse(const String *str, int pos, int eol, SMatches1 *mtch, SMatchHash1 *nmtch = null, int soscheme = 0, int moves = -1);
#else
  /** Runs RE parser against input string @c str
  */
  bool parse(const String *str, SMatches1 *mtch);
  /** Runs RE parser against input string @c str
  */
  bool parse(const String *str, int pos, int eol, SMatches1 *mtch, int soscheme = 0, int moves = -1);
#endif

private:
  bool ignoreCase, extend, positionMoves, singleLine, multiLine;
  SRegInfo1 *tree_root;
  EError1 error;
  wchar firstChar;
  EMetaSymbols1 firstMetaChar;
#ifdef COLORERMODE
  CRegExp1 *backRE;
  const String *backStr;
  SMatches1 *backTrace;
  int schemeStart;
#endif
  bool startChange, endChange;
  const String *global_pattern;
  int end;

  SMatches1 *matches;
  int cMatch;
#if !defined NAMED_MATCHES_IN_HASH
  String *(brnames[NAMED_MATCHES_NUM]);
  int cnMatch;
#else
  SMatchHash1 *namedMatches;
#endif

  void init();
  EError1 setRELow(const String &re);
  EError1 setStructs(SRegInfo1 *&, const String &expr, int &endPos);

  void optimize();
  bool quickCheck(int toParse);
  bool isWordBoundary(int &toParse);
  bool isNWordBoundary(int &toParse);
  bool checkMetaSymbol(EMetaSymbols1 metaSymbol, int &toParse);
  bool lowParse(SRegInfo1 *re, SRegInfo1 *prev, int toParse);
  bool parseRE(int toParse);

  int count_elem;
  void check_stack(bool res,SRegInfo1 **re, SRegInfo1 **prev, int *toParse,bool *leftenter, int *action);
  void insert_stack(SRegInfo1 **re, SRegInfo1 **prev, int *toParse, bool *leftenter, int ifTrueReturn, int ifFalseReturn, SRegInfo1 **re2, SRegInfo1 **prev2, int toParse2);

};



#endif
/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0/LGPL 2.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is the Colorer Library.
 *
 * The Initial Developer of the Original Code is
 * Cail Lomecb <cail@nm.ru>.
 * Portions created by the Initial Developer are Copyright (C) 1999-2005
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *
 * Alternatively, the contents of this file may be used under the terms of
 * either the GNU General Public License Version 2 or later (the "GPL"), or
 * the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
 * in which case the provisions of the GPL or the LGPL are applicable instead
 * of those above. If you wish to allow use of your version of this file only
 * under the terms of either the GPL or the LGPL, and not to allow others to
 * use your version of this file under the terms of the MPL, indicate your
 * decision by deleting the provisions above and replace them with the notice
 * and other provisions required by the GPL or the LGPL. If you do not delete
 * the provisions above, a recipient may use your version of this file under
 * the terms of any one of the MPL, the GPL or the LGPL.
 *
 * ***** END LICENSE BLOCK ***** */
