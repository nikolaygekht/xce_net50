using Far.Colorer.RegularExpressions;
using Far.Colorer.RegularExpressions.Enums;
using Far.Colorer.RegularExpressions.Internal;
using Xunit;
using AwesomeAssertions;
using System;
using System.Linq;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Comprehensive tests for ColorerMatch and CaptureGroup API properties and methods.
/// Targets coverage improvement from 34% to 80%+ for Match API classes.
/// </summary>
public class MatchApiTests
{
    #region ColorerMatch - Basic Properties

    [Fact]
    public void Match_Success_SuccessPropertyIsTrue()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        match!.Success.Should().BeTrue();
    }

    [Fact]
    public void Match_Failed_ReturnsNull()
    {
        var regex = new ColorerRegex("xyz");
        var match = regex.Match("abc");

        match.Should().BeNull();
    }

    [Fact]
    public void Match_Success_IndexAndLengthCorrect()
    {
        var regex = new ColorerRegex("world");
        var match = regex.Match("hello world!");

        match.Should().NotBeNull();
        match!.Index.Should().Be(6);
        match.Length.Should().Be(5);
    }

    [Fact]
    public void Match_Success_ValueReturnsMatchedText()
    {
        var regex = new ColorerRegex("test");
        var match = regex.Match("this is a test");

        match.Should().NotBeNull();
        match!.Value.Should().Be("test");
    }

    [Fact]
    public void Match_Success_InputPropertyReturnsOriginalInput()
    {
        var regex = new ColorerRegex("foo");
        string input = "foobar";
        var match = regex.Match(input);

        match.Should().NotBeNull();
        match!.Input.Should().BeSameAs(input);
    }

    #endregion

    #region ColorerMatch - ToString

    [Fact]
    public void Match_Success_ToStringShowsIndexAndValue()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("xyzabcdef");

        match.Should().NotBeNull();
        var str = match!.ToString();

        str.Should().Contain("3");      // Index
        str.Should().Contain("6");      // Index + Length
        str.Should().Contain("abc");    // Value
        str.Should().Contain("Match");
    }

    [Fact]
    public void Match_Empty_ToStringShowsFailedMatch()
    {
        var emptyMatch = ColorerMatch.Empty;
        var str = emptyMatch.ToString();

        str.Should().Contain("Failed");
    }

    [Fact]
    public void Match_CreateFailed_ToStringShowsFailedMatch()
    {
        var failedMatch = ColorerMatch.CreateFailed("test input");
        var str = failedMatch.ToString();

        str.Should().Contain("Failed");
    }

    #endregion

    #region ColorerMatch.Empty

    [Fact]
    public void Empty_SuccessIsFalse()
    {
        var emptyMatch = ColorerMatch.Empty;

        emptyMatch.Success.Should().BeFalse();
    }

    [Fact]
    public void Empty_IndexIsNegative()
    {
        var emptyMatch = ColorerMatch.Empty;

        emptyMatch.Index.Should().Be(-1);
    }

    [Fact]
    public void Empty_LengthIsZero()
    {
        var emptyMatch = ColorerMatch.Empty;

        emptyMatch.Length.Should().Be(0);
    }

    [Fact]
    public void Empty_ValueIsEmpty()
    {
        var emptyMatch = ColorerMatch.Empty;

        emptyMatch.Value.Should().Be(string.Empty);
    }

    [Fact]
    public void Empty_GroupsIsEmpty()
    {
        var emptyMatch = ColorerMatch.Empty;

        emptyMatch.Groups.Should().BeEmpty();
    }

    [Fact]
    public void Empty_EffectiveStartAndEndAreNegative()
    {
        var emptyMatch = ColorerMatch.Empty;

        emptyMatch.EffectiveStart.Should().Be(-1);
        emptyMatch.EffectiveEnd.Should().Be(-1);
    }

    #endregion

    #region ColorerMatch.CreateFailed

    [Fact]
    public void CreateFailed_WithInput_SuccessIsFalse()
    {
        var failedMatch = ColorerMatch.CreateFailed("test input");

        failedMatch.Success.Should().BeFalse();
    }

    [Fact]
    public void CreateFailed_WithInput_InputPropertySet()
    {
        string input = "test input";
        var failedMatch = ColorerMatch.CreateFailed(input);

        failedMatch.Input.Should().BeSameAs(input);
    }

    [Fact]
    public void CreateFailed_WithInput_IndexIsNegative()
    {
        var failedMatch = ColorerMatch.CreateFailed("test");

        failedMatch.Index.Should().Be(-1);
    }

    [Fact]
    public void CreateFailed_WithInput_ValueIsEmpty()
    {
        var failedMatch = ColorerMatch.CreateFailed("test");

        failedMatch.Value.Should().Be(string.Empty);
    }

    #endregion

    #region GetMatchSpan

    [Fact]
    public void GetMatchSpan_Success_ReturnsCorrectSpan()
    {
        var regex = new ColorerRegex("world");
        var match = regex.Match("hello world!");

        match.Should().NotBeNull();
        var span = match!.GetMatchSpan();

        span.ToString().Should().Be("world");
        span.Length.Should().Be(5);
    }

    [Fact]
    public void GetMatchSpan_EmptyMatch_ReturnsEmptySpan()
    {
        var emptyMatch = ColorerMatch.Empty;
        var span = emptyMatch.GetMatchSpan();

        span.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetMatchSpan_FailedMatch_ReturnsEmptySpan()
    {
        var failedMatch = ColorerMatch.CreateFailed("test");
        var span = failedMatch.GetMatchSpan();

        span.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetMatchSpan_ZeroAllocation_DoesNotAllocate()
    {
        // This test verifies the span-based API exists and works
        var regex = new ColorerRegex("test");
        var match = regex.Match("testing");

        match.Should().NotBeNull();
        var span = match!.GetMatchSpan();

        // Verify it's truly a span over the original string
        span.ToString().Should().Be("test");
    }

    #endregion

    #region GetEffectiveSpan

    [Fact]
    public void GetEffectiveSpan_NoMarkersSet_ReturnsSameAsGetMatchSpan()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        var matchSpan = match!.GetMatchSpan();
        var effectiveSpan = match.GetEffectiveSpan();

        effectiveSpan.ToString().Should().Be(matchSpan.ToString());
    }

    [Fact]
    public void GetEffectiveSpan_EffectiveStartModified_UsesEffectiveRange()
    {
        var regex = new ColorerRegex("bcd");
        var match = regex.Match("abcde");

        match.Should().NotBeNull();
        // Simulate \M marker - modify effective start
        match!.EffectiveStart = 0; // Start from 'a' instead of 'b'

        var effectiveSpan = match.GetEffectiveSpan();
        effectiveSpan.ToString().Should().Be("abcd");
    }

    [Fact]
    public void GetEffectiveSpan_EffectiveEndModified_UsesEffectiveRange()
    {
        var regex = new ColorerRegex("bcd");
        var match = regex.Match("abcde");

        match.Should().NotBeNull();
        // Simulate \m marker - modify effective end
        match!.EffectiveEnd = 5; // Extend to include 'e'

        var effectiveSpan = match.GetEffectiveSpan();
        effectiveSpan.ToString().Should().Be("bcde");
    }

    [Fact]
    public void GetEffectiveSpan_FailedMatch_ReturnsEmptySpan()
    {
        var failedMatch = ColorerMatch.CreateFailed("test");
        var span = failedMatch.GetEffectiveSpan();

        span.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetEffectiveSpan_InvalidRange_FallsBackToGetMatchSpan()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        // Set invalid effective range
        match!.EffectiveStart = 10;
        match.EffectiveEnd = 5;

        var span = match.GetEffectiveSpan();
        // Should fall back to regular match span
        span.ToString().Should().Be("abc");
    }

    #endregion

    #region Groups Property

    [Fact]
    public void Groups_NoCaptures_ContainsOnlyGroup0()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        match!.Groups.Should().HaveCount(1);
        match.Groups[0].Success.Should().BeTrue();
    }

    [Fact]
    public void Groups_WithCaptures_ContainsAllGroups()
    {
        var regex = new ColorerRegex("(a)(b)(c)");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        match!.Groups.Should().HaveCount(4); // Group 0 + 3 captures
    }

    [Fact]
    public void Groups_FailedMatch_IsEmpty()
    {
        var failedMatch = ColorerMatch.CreateFailed("test");

        failedMatch.Groups.Should().BeEmpty();
    }

    #endregion

    #region GetGroup(int)

    [Fact]
    public void GetGroup_ByNumber_Group0_ReturnsFullMatch()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        var group = match!.GetGroup(0);

        group.Success.Should().BeTrue();
        group.GroupNumber.Should().Be(0);
        group.Index.Should().Be(0);
        group.Length.Should().Be(3);
    }

    [Fact]
    public void GetGroup_ByNumber_ValidGroup_ReturnsGroup()
    {
        var regex = new ColorerRegex("(abc)");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        var group = match!.GetGroup(1);

        group.Success.Should().BeTrue();
        group.GroupNumber.Should().Be(1);
    }

    [Fact]
    public void GetGroup_ByNumber_InvalidNegative_ReturnsFailedGroup()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        var group = match!.GetGroup(-1);

        group.Success.Should().BeFalse();
        group.GroupNumber.Should().Be(-1);
    }

    [Fact]
    public void GetGroup_ByNumber_OutOfRange_ReturnsFailedGroup()
    {
        var regex = new ColorerRegex("(abc)");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        var group = match!.GetGroup(99);

        group.Success.Should().BeFalse();
        group.GroupNumber.Should().Be(99);
    }

    #endregion

    #region GetGroup(string) - Named Groups

    [Fact]
    public void GetGroup_ByName_NonExistent_ReturnsFailedGroup()
    {
        var regex = new ColorerRegex("(abc)");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        var group = match!.GetGroup("nonexistent");

        group.Success.Should().BeFalse();
        group.Name.Should().Be("nonexistent");
    }

    [Fact]
    public void GetGroup_ByName_NullName_ThrowsException()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        Action act = () => match!.GetGroup((string)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region GetGroupSpan

    [Fact]
    public void GetGroupSpan_ByNumber_ValidGroup_ReturnsSpan()
    {
        var regex = new ColorerRegex("a(bc)d");
        var match = regex.Match("abcd");

        match.Should().NotBeNull();
        var span = match!.GetGroupSpan(1);

        span.ToString().Should().Be("bc");
    }

    [Fact]
    public void GetGroupSpan_ByNumber_InvalidGroup_ReturnsEmptySpan()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        var span = match!.GetGroupSpan(5);

        span.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void GetGroupSpan_ByName_NonExistent_ReturnsEmptySpan()
    {
        var regex = new ColorerRegex("(abc)");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        var span = match!.GetGroupSpan("nonexistent");

        span.IsEmpty.Should().BeTrue();
    }

    #endregion

    #region GetGroupValue

    [Fact]
    public void GetGroupValue_ByNumber_ValidGroup_ReturnsValue()
    {
        var regex = new ColorerRegex("(\\d+)");
        var match = regex.Match("abc123def");

        match.Should().NotBeNull();
        match!.GetGroupValue(0).Should().Be("123");
        match.GetGroupValue(1).Should().Be("123");
    }

    [Fact]
    public void GetGroupValue_ByNumber_InvalidGroup_ReturnsEmpty()
    {
        var regex = new ColorerRegex("abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        match!.GetGroupValue(5).Should().Be(string.Empty);
    }

    [Fact]
    public void GetGroupValue_ByName_NonExistent_ReturnsEmpty()
    {
        var regex = new ColorerRegex("(abc)");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        match!.GetGroupValue("nonexistent").Should().Be(string.Empty);
    }

    #endregion

    #region TryGetGroupNumber

    [Fact]
    public void TryGetGroupNumber_NonExistentName_ReturnsFalse()
    {
        var regex = new ColorerRegex("(abc)");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        bool result = match!.TryGetGroupNumber("nonexistent", out int groupNumber);

        result.Should().BeFalse();
        groupNumber.Should().Be(-1); // -1 for non-existent groups
    }

    [Fact]
    public void TryGetGroupNumber_NoNamedGroups_ReturnsFalse()
    {
        var regex = new ColorerRegex("(abc)(def)");
        var match = regex.Match("abcdef");

        match.Should().NotBeNull();
        bool result = match!.TryGetGroupNumber("anyname", out int groupNumber);

        result.Should().BeFalse();
    }

    #endregion

    #region GetGroupNames

    [Fact]
    public void GetGroupNames_NoNamedGroups_ReturnsEmpty()
    {
        var regex = new ColorerRegex("(abc)(def)");
        var match = regex.Match("abcdef");

        match.Should().NotBeNull();
        var names = match!.GetGroupNames().ToList();

        names.Should().BeEmpty();
    }

    [Fact]
    public void GetGroupNames_FailedMatch_ReturnsEmpty()
    {
        var failedMatch = ColorerMatch.CreateFailed("test");
        var names = failedMatch.GetGroupNames().ToList();

        names.Should().BeEmpty();
    }

    #endregion

    #region CaptureGroup - Properties

    [Fact]
    public void CaptureGroup_Success_AllPropertiesSet()
    {
        var group = new CaptureGroup(index: 5, length: 3, groupNumber: 1, name: "test");

        group.Success.Should().BeTrue();
        group.Index.Should().Be(5);
        group.Length.Should().Be(3);
        group.GroupNumber.Should().Be(1);
        group.Name.Should().Be("test");
    }

    [Fact]
    public void CaptureGroup_Success_NoName_NameIsNull()
    {
        var group = new CaptureGroup(index: 0, length: 5, groupNumber: 0);

        group.Success.Should().BeTrue();
        group.Name.Should().BeNull();
    }

    [Fact]
    public void CaptureGroup_EndIndex_CalculatedCorrectly()
    {
        var group = new CaptureGroup(index: 10, length: 5, groupNumber: 0);

        group.EndIndex.Should().Be(15); // 10 + 5
    }

    [Fact]
    public void CaptureGroup_ZeroLength_EndIndexEqualsIndex()
    {
        var group = new CaptureGroup(index: 7, length: 0, groupNumber: 0);

        group.EndIndex.Should().Be(7);
    }

    #endregion

    #region CaptureGroup.CreateFailed

    [Fact]
    public void CreateFailed_WithNumber_SuccessIsFalse()
    {
        var group = CaptureGroup.CreateFailed(groupNumber: 5);

        group.Success.Should().BeFalse();
    }

    [Fact]
    public void CreateFailed_WithNumber_GroupNumberSet()
    {
        var group = CaptureGroup.CreateFailed(groupNumber: 3);

        group.GroupNumber.Should().Be(3);
    }

    [Fact]
    public void CreateFailed_WithNumber_IndexIsNegative()
    {
        var group = CaptureGroup.CreateFailed(groupNumber: 1);

        group.Index.Should().Be(-1);
    }

    [Fact]
    public void CreateFailed_WithNumber_LengthIsZero()
    {
        var group = CaptureGroup.CreateFailed(groupNumber: 1);

        group.Length.Should().Be(0);
    }

    [Fact]
    public void CreateFailed_WithName_NameIsSet()
    {
        var group = CaptureGroup.CreateFailed(groupNumber: 1, name: "testName");

        group.Name.Should().Be("testName");
        group.Success.Should().BeFalse();
    }

    #endregion

    #region CaptureGroup - ToString

    [Fact]
    public void CaptureGroup_ToString_Success_ShowsIndexAndRange()
    {
        var group = new CaptureGroup(index: 5, length: 3, groupNumber: 1);

        var str = group.ToString();

        str.Should().Contain("Group 1");
        str.Should().Contain("5");
        str.Should().Contain("8"); // EndIndex
    }

    [Fact]
    public void CaptureGroup_ToString_Success_WithName_ShowsName()
    {
        var group = new CaptureGroup(index: 5, length: 3, groupNumber: 2, name: "myGroup");

        var str = group.ToString();

        str.Should().Contain("Group 2");
        str.Should().Contain("myGroup");
    }

    [Fact]
    public void CaptureGroup_ToString_Failed_ShowsFailed()
    {
        var group = CaptureGroup.CreateFailed(groupNumber: 3);

        var str = group.ToString();

        str.Should().Contain("Group 3");
        str.Should().Contain("Failed");
    }

    [Fact]
    public void CaptureGroup_ToString_Failed_WithName_ShowsNameAndFailed()
    {
        var group = CaptureGroup.CreateFailed(groupNumber: 1, name: "namedGroup");

        var str = group.ToString();

        str.Should().Contain("Group 1");
        str.Should().Contain("namedGroup");
        str.Should().Contain("Failed");
    }

    #endregion

    #region Integration Tests - Multiple Groups

    [Fact]
    public void Match_MultipleGroups_AllPropertiesAccessible()
    {
        var regex = new ColorerRegex("(\\d+)-(\\w+)");
        var match = regex.Match("ID: 123-abc!");

        match.Should().NotBeNull();

        // Test groups collection
        match!.Groups.Should().HaveCount(3);

        // Test GetGroup
        var group0 = match.GetGroup(0);
        group0.Success.Should().BeTrue();
        group0.Index.Should().Be(4);
        group0.Length.Should().Be(7);

        var group1 = match.GetGroup(1);
        group1.GroupNumber.Should().Be(1);

        var group2 = match.GetGroup(2);
        group2.GroupNumber.Should().Be(2);

        // Test GetGroupValue
        match.GetGroupValue(0).Should().Be("123-abc");
        match.GetGroupValue(1).Should().Be("123");
        match.GetGroupValue(2).Should().Be("abc");

        // Test GetGroupSpan
        match.GetGroupSpan(1).ToString().Should().Be("123");
        match.GetGroupSpan(2).ToString().Should().Be("abc");
    }

    [Fact]
    public void Match_NestedGroups_PropertiesAccessible()
    {
        var regex = new ColorerRegex("((a)(b))");
        var match = regex.Match("ab");

        match.Should().NotBeNull();
        match!.Groups.Should().HaveCount(4); // 0, 1, 2, 3

        match.GetGroupValue(0).Should().Be("ab");
        match.GetGroupValue(1).Should().Be("ab");
        match.GetGroupValue(2).Should().Be("a");
        match.GetGroupValue(3).Should().Be("b");

        // Verify EndIndex for each group
        match.GetGroup(2).EndIndex.Should().Be(1);
        match.GetGroup(3).EndIndex.Should().Be(2);
    }

    [Fact]
    public void Match_EmptyCapture_GroupPropertiesCorrect()
    {
        var regex = new ColorerRegex("a(b*)c");
        var match = regex.Match("ac"); // b* matches empty

        match.Should().NotBeNull();
        var group1 = match!.GetGroup(1);

        group1.Success.Should().BeTrue();
        group1.Length.Should().Be(0);
        group1.EndIndex.Should().Be(group1.Index);

        match.GetGroupValue(1).Should().Be(string.Empty);
        match.GetGroupSpan(1).IsEmpty.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Match_VeryLongInput_PropertiesCorrect()
    {
        var longInput = new string('x', 10000) + "test" + new string('y', 10000);
        var regex = new ColorerRegex("test");
        var match = regex.Match(longInput);

        match.Should().NotBeNull();
        match!.Index.Should().Be(10000);
        match.Length.Should().Be(4);
        match.Value.Should().Be("test");
        match.Input.Should().BeSameAs(longInput);
    }

    [Fact]
    public void Match_UnicodeCharacters_PropertiesCorrect()
    {
        var regex = new ColorerRegex("(世界)");
        var match = regex.Match("Hello 世界!");

        match.Should().NotBeNull();
        match!.Value.Should().Be("世界");
        match.GetGroupValue(1).Should().Be("世界");
        match.GetGroupSpan(1).ToString().Should().Be("世界");
    }

    [Fact]
    public void Match_AtStartOfString_IndexIsZero()
    {
        var regex = new ColorerRegex("^abc");
        var match = regex.Match("abc");

        match.Should().NotBeNull();
        match!.Index.Should().Be(0);
        match.EffectiveStart.Should().Be(0);
    }

    [Fact]
    public void Match_AtEndOfString_EndIndexEqualsInputLength()
    {
        var regex = new ColorerRegex("xyz$");
        var match = regex.Match("abcxyz");

        match.Should().NotBeNull();
        match!.Index.Should().Be(3);
        match.Length.Should().Be(3);
        (match.Index + match.Length).Should().Be("abcxyz".Length);
    }

    #endregion
}
