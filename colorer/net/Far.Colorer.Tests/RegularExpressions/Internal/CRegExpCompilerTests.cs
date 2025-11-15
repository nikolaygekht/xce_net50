using AwesomeAssertions;
using Far.Colorer.RegularExpressions.Enums;
using Far.Colorer.RegularExpressions.Internal;

namespace Far.Colorer.Tests.RegularExpressions.Internal;

/// <summary>
/// Unit tests for CRegExpCompiler to verify correct pattern compilation
/// Tests the compiler in isolation before moving to matcher
/// </summary>
public unsafe class CRegExpCompilerTests : IDisposable
{
    private List<CRegExpCompiler> compilersToDispose = new();

    public void Dispose()
    {
        foreach (var compiler in compilersToDispose)
        {
            compiler?.Dispose();
        }
        compilersToDispose.Clear();
    }

    private CRegExpCompiler CreateCompiler(string pattern, RegexOptions options = RegexOptions.None)
    {
        var compiler = new CRegExpCompiler(pattern, options);
        compilersToDispose.Add(compiler);
        return compiler;
    }

    #region Basic Literal Tests

    [Fact]
    public void Compile_SingleLiteral_CreatesCorrectTree()
    {
        // Arrange
        var compiler = CreateCompiler("a");

        // Act
        var root = compiler.Compile();

        // Assert
        ((IntPtr)root).Should().NotBe((IntPtr)null);
        root->op.Should().Be(EOps.ReBrackets);
        root->param0.Should().Be(0); // Group 0
        ((IntPtr)root->param).Should().NotBe((IntPtr)null);

        // Check first child is literal 'a'
        var firstChild = root->param;
        firstChild->op.Should().Be(EOps.ReSymb);
        firstChild->symbol.Should().Be('a');
    }

    [Fact]
    public void Compile_MultipleLiterals_CreatesLinkedList()
    {
        // Arrange
        var compiler = CreateCompiler("abc");

        // Act
        var root = compiler.Compile();

        // Assert
        var node = root->param;
        node->op.Should().Be(EOps.ReSymb);
        node->symbol.Should().Be('a');

        node = node->next;
        ((IntPtr)node).Should().NotBe((IntPtr)null);
        node->op.Should().Be(EOps.ReSymb);
        node->symbol.Should().Be('b');

        node = node->next;
        ((IntPtr)node).Should().NotBe((IntPtr)null);
        node->op.Should().Be(EOps.ReSymb);
        node->symbol.Should().Be('c');

        ((IntPtr)node->next).Should().Be((IntPtr)null);
    }

    #endregion

    #region Metacharacter Tests

    [Fact]
    public void Compile_Dot_CreatesAnyChrMetaSymbol()
    {
        var compiler = CreateCompiler(".");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReAnyChr);
    }

    [Fact]
    public void Compile_Caret_CreatesSoLMetaSymbol()
    {
        var compiler = CreateCompiler("^");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReSoScheme);
    }

    [Fact]
    public void Compile_Dollar_CreatesEoLMetaSymbol()
    {
        var compiler = CreateCompiler("$");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReEoL);
    }

    [Fact]
    public void Compile_Tilde_CreatesSoSchemeMetaSymbol()
    {
        var compiler = CreateCompiler("~");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReSoScheme);
    }

    #endregion

    #region Escape Sequence Tests

    [Fact]
    public void Compile_EscapeD_CreatesDigitMetaSymbol()
    {
        var compiler = CreateCompiler(@"\d");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReDigit);
    }

    [Fact]
    public void Compile_EscapeCapitalD_CreatesNonDigitMetaSymbol()
    {
        var compiler = CreateCompiler(@"\D");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReNDigit);
    }

    [Fact]
    public void Compile_EscapeW_CreatesWordMetaSymbol()
    {
        var compiler = CreateCompiler(@"\w");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReWordSymb);
    }

    [Fact]
    public void Compile_EscapeCapitalW_CreatesNonWordMetaSymbol()
    {
        var compiler = CreateCompiler(@"\W");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReNWordSymb);
    }

    [Fact]
    public void Compile_EscapeS_CreatesWhitespaceMetaSymbol()
    {
        var compiler = CreateCompiler(@"\s");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReWSpace);
    }

    [Fact]
    public void Compile_EscapeCapitalS_CreatesNonWhitespaceMetaSymbol()
    {
        var compiler = CreateCompiler(@"\S");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReNWSpace);
    }

    [Fact]
    public void Compile_EscapeB_CreatesWordBoundaryMetaSymbol()
    {
        var compiler = CreateCompiler(@"\b");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReWBound);
    }

    [Fact]
    public void Compile_EscapeCapitalB_CreatesNonWordBoundaryMetaSymbol()
    {
        var compiler = CreateCompiler(@"\B");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReNWBound);
    }

    [Fact]
    public void Compile_EscapeM_CreatesStartMetaSymbol()
    {
        var compiler = CreateCompiler(@"\m");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReStart);
    }

    [Fact]
    public void Compile_EscapeCapitalM_CreatesEndMetaSymbol()
    {
        var compiler = CreateCompiler(@"\M");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReMetaSymb);
        node->metaSymbol.Should().Be(EMetaSymbols.ReEnd);
    }

    [Fact]
    public void Compile_EscapeT_CreatesTabLiteral()
    {
        var compiler = CreateCompiler(@"\t");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReSymb);
        node->symbol.Should().Be('\t');
    }

    [Fact]
    public void Compile_EscapeN_CreatesNewlineLiteral()
    {
        var compiler = CreateCompiler(@"\n");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReSymb);
        node->symbol.Should().Be('\n');
    }

    [Fact]
    public void Compile_EscapedSpecialChar_CreatesLiteral()
    {
        var compiler = CreateCompiler(@"\.");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReSymb);
        node->symbol.Should().Be('.');
    }

    #endregion

    #region Quantifier Tests

    [Fact]
    public void Compile_Star_CreatesRangeNQuantifier()
    {
        var compiler = CreateCompiler("a*");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReRangeN);
        node->s.Should().Be(0); // min 0
        node->e.Should().Be(-1); // max unbounded

        // Check wrapped atom
        ((IntPtr)node->param).Should().NotBe((IntPtr)null);
        node->param->op.Should().Be(EOps.ReSymb);
        node->param->symbol.Should().Be('a');
    }

    [Fact]
    public void Compile_Plus_CreatesRangeNQuantifier()
    {
        var compiler = CreateCompiler("a+");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReRangeN);
        node->s.Should().Be(1); // min 1
        node->e.Should().Be(-1); // max unbounded
    }

    [Fact]
    public void Compile_Question_CreatesRangeNMQuantifier()
    {
        var compiler = CreateCompiler("a?");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReRangeNM);
        node->s.Should().Be(0); // min 0
        node->e.Should().Be(1); // max 1
    }

    [Fact]
    public void Compile_StarNonGreedy_CreatesNonGreedyQuantifier()
    {
        var compiler = CreateCompiler("a*?");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReNGRangeN);
        node->s.Should().Be(0);
        node->e.Should().Be(-1);
    }

    [Fact]
    public void Compile_PlusNonGreedy_CreatesNonGreedyQuantifier()
    {
        var compiler = CreateCompiler("a+?");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReNGRangeN);
        node->s.Should().Be(1);
        node->e.Should().Be(-1);
    }

    [Fact]
    public void Compile_QuestionNonGreedy_CreatesNonGreedyQuantifier()
    {
        var compiler = CreateCompiler("a??");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReNGRangeNM);
        node->s.Should().Be(0);
        node->e.Should().Be(1);
    }

    [Fact]
    public void Compile_RangeExact_CreatesCorrectQuantifier()
    {
        var compiler = CreateCompiler("a{3}");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReRangeNM);
        node->s.Should().Be(3);
        node->e.Should().Be(3);
    }

    [Fact]
    public void Compile_RangeMin_CreatesUnboundedQuantifier()
    {
        var compiler = CreateCompiler("a{2,}");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReRangeN);
        node->s.Should().Be(2);
        node->e.Should().Be(-1);
    }

    [Fact]
    public void Compile_RangeMinMax_CreatesRangeQuantifier()
    {
        var compiler = CreateCompiler("a{2,4}");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReRangeNM);
        node->s.Should().Be(2);
        node->e.Should().Be(4);
    }

    [Fact]
    public void Compile_RangeNonGreedy_CreatesNonGreedyQuantifier()
    {
        var compiler = CreateCompiler("a{2,4}?");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReNGRangeNM);
        node->s.Should().Be(2);
        node->e.Should().Be(4);
    }

    #endregion

    #region Group Tests

    [Fact]
    public void Compile_SimpleGroup_CreatesGroupNode()
    {
        var compiler = CreateCompiler("(a)");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReBrackets);
        node->param0.Should().Be(1); // Group 1 (group 0 is implicit root)

        // Check group content
        ((IntPtr)node->param).Should().NotBe((IntPtr)null);
        node->param->op.Should().Be(EOps.ReSymb);
        node->param->symbol.Should().Be('a');
    }

    [Fact]
    public void Compile_NonCapturingGroup_CreatesNamedBracketsWithNegativeIndex()
    {
        var compiler = CreateCompiler("(?:a)");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReNamedBrackets);
        node->param0.Should().Be(-1); // Non-capturing
    }

    [Fact]
    public void Compile_NamedGroup_CreatesNamedBrackets()
    {
        var compiler = CreateCompiler("(?{test}a)");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReNamedBrackets);
        (node->param0 >= 0).Should().Be(true); // Has group number
    }

    [Fact]
    public void Compile_PositiveLookahead_CreatesAheadNode()
    {
        var compiler = CreateCompiler("(?=a)");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReAhead);
        node->param0.Should().Be(-1);
    }

    [Fact]
    public void Compile_NegativeLookahead_CreatesNAheadNode()
    {
        var compiler = CreateCompiler("(?!a)");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReNAhead);
        node->param0.Should().Be(-1);
    }

    [Fact]
    public void Compile_MultipleGroups_IncreasesGroupCount()
    {
        var compiler = CreateCompiler("(a)(b)(c)");
        var root = compiler.Compile();

        compiler.TotalGroups.Should().Be(4); // 0 (root) + 3 groups
    }

    #endregion

    #region Backreference Tests

    [Fact]
    public void Compile_NumericBackreference_CreatesBkBrackNode()
    {
        var compiler = CreateCompiler(@"(a)\1");
        var root = compiler.Compile();

        // Find the backreference node (second in sequence after group)
        var node = root->param;
        node->op.Should().Be(EOps.ReBrackets);

        node = node->next;
        node->op.Should().Be(EOps.ReBkBrack);
        node->param0.Should().Be(1);
    }

    [Fact]
    public void Compile_NamedBackreferenceY_CreatesBkTraceNode()
    {
        var compiler = CreateCompiler(@"(?{test}a)\y{test}");
        var root = compiler.Compile();

        // Find the backreference node
        var node = root->param;
        node->op.Should().Be(EOps.ReNamedBrackets);

        node = node->next;
        node->op.Should().Be(EOps.ReBkTraceName);
        (node->param0 >= 0).Should().Be(true);
    }

    #endregion

    #region Character Class Tests

    [Fact]
    public void Compile_CharacterClass_CreatesEnumNode()
    {
        var compiler = CreateCompiler("[abc]");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReEnum);
        ((IntPtr)node->charclass).Should().NotBe((IntPtr)null);
    }

    [Fact]
    public void Compile_NegatedCharacterClass_CreatesNEnumNode()
    {
        var compiler = CreateCompiler("[^abc]");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReNEnum);
        ((IntPtr)node->charclass).Should().NotBe((IntPtr)null);
    }

    #endregion

    #region Alternation Tests

    [Fact]
    public void Compile_Alternation_CreatesOrNode()
    {
        var compiler = CreateCompiler("a|b");
        var root = compiler.Compile();

        // Root contains the alternation
        var node = root->param;

        // After alternation processing, should have ReOr node
        // with 'a' in param and 'b' in next
        node->op.Should().Be(EOps.ReOr);

        // Left alternative (param)
        ((IntPtr)node->param).Should().NotBe((IntPtr)null);
        node->param->op.Should().Be(EOps.ReSymb);
        node->param->symbol.Should().Be('a');

        // Right alternative (next)
        ((IntPtr)node->next).Should().NotBe((IntPtr)null);
        node->next->op.Should().Be(EOps.ReSymb);
        node->next->symbol.Should().Be('b');
    }

    #endregion

    #region Error Cases

    [Fact]
    public void Compile_UnclosedGroup_ThrowsException()
    {
        // Arrange
        var compiler = CreateCompiler("(abc");

        // Act
        Action act = () => compiler.Compile();

        // Assert
        act.Should().Throw<RegexSyntaxException>();
    }

    [Fact]
    public void Compile_UnclosedRange_ThrowsException()
    {
        // Arrange
        var compiler = CreateCompiler("a{2,");

        // Act
        Action act = () => compiler.Compile();

        // Assert
        act.Should().Throw<RegexSyntaxException>();
    }

    [Fact]
    public void Compile_CrossPatternBackreference_AllowsUnknownName()
    {
        // Arrange - \y{unknown} where "unknown" doesn't exist in current pattern
        // This is valid - it's a cross-pattern backreference that will be resolved at match time
        var compiler = CreateCompiler(@"\y{unknown}");

        // Act
        var root = compiler.Compile();

        // Assert - Should compile successfully with param0 = -1
        var node = root->param;
        node->op.Should().Be(EOps.ReBkTraceName);
        node->param0.Should().Be(-1); // Cross-pattern - resolve at runtime
    }

    // Note: Empty patterns are now ALLOWED per integration test requirements
    // They compile to ReEmpty node and match at any position (zero-width match)
    // See ColorerRegexTests.Match_EmptyPattern_MatchesAtStart

    #endregion

    #region Complex Pattern Tests

    [Fact]
    public void Compile_ComplexPattern_BuildsCorrectTree()
    {
        var compiler = CreateCompiler("a+b*c?");
        var root = compiler.Compile();

        // a+
        var node = root->param;
        node->op.Should().Be(EOps.ReRangeN);
        node->s.Should().Be(1);

        // b*
        node = node->next;
        ((IntPtr)node).Should().NotBe((IntPtr)null);
        node->op.Should().Be(EOps.ReRangeN);
        node->s.Should().Be(0);

        // c?
        node = node->next;
        ((IntPtr)node).Should().NotBe((IntPtr)null);
        node->op.Should().Be(EOps.ReRangeNM);
        node->s.Should().Be(0);
        node->e.Should().Be(1);
    }

    [Fact]
    public void Compile_GroupWithQuantifier_WrapsCorrectly()
    {
        var compiler = CreateCompiler("(ab)+");
        var root = compiler.Compile();

        var node = root->param;
        node->op.Should().Be(EOps.ReRangeN);
        node->s.Should().Be(1);

        // Check wrapped group
        ((IntPtr)node->param).Should().NotBe((IntPtr)null);
        node->param->op.Should().Be(EOps.ReBrackets);
    }

    #endregion
}
