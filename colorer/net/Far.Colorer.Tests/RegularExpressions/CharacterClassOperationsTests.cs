using Far.Colorer.RegularExpressions.Internal;
using Xunit;
using AwesomeAssertions;
using System;

namespace Far.Colorer.Tests.RegularExpressions;

/// <summary>
/// Tests for CharacterClass set operations (Union, Intersect, Subtract)
/// and Character utility methods to improve coverage.
/// Targets CharacterClass coverage from 48.3% to 75%+.
/// </summary>
public unsafe class CharacterClassOperationsTests
{
    #region Union Operation Tests

    [Fact]
    public void Union_TwoDisjointSets_ContainsBoth()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddChar('a');
        class1.AddChar('b');

        class2.AddChar('x');
        class2.AddChar('y');

        // Act
        class1.Union(&class2);

        // Assert
        class1.Contains('a').Should().BeTrue();
        class1.Contains('b').Should().BeTrue();
        class1.Contains('x').Should().BeTrue();
        class1.Contains('y').Should().BeTrue();
        class1.Contains('c').Should().BeFalse();
    }

    [Fact]
    public void Union_OverlappingSets_ContainsAll()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'e'); // a,b,c,d,e
        class2.AddRange('c', 'g'); // c,d,e,f,g

        // Act
        class1.Union(&class2);

        // Assert
        class1.Contains('a').Should().BeTrue();
        class1.Contains('c').Should().BeTrue(); // Overlap
        class1.Contains('e').Should().BeTrue(); // Overlap
        class1.Contains('g').Should().BeTrue();
        class1.Contains('h').Should().BeFalse();
    }

    [Fact]
    public void Union_WithEmptySet_RemainsUnchanged()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddChar('a');
        class1.AddChar('b');

        // Act
        class1.Union(&class2);

        // Assert
        class1.Contains('a').Should().BeTrue();
        class1.Contains('b').Should().BeTrue();
        class1.Contains('c').Should().BeFalse();
    }

    [Fact]
    public void Union_EmptyWithNonEmpty_ContainsNonEmpty()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class2.AddChar('x');
        class2.AddChar('y');

        // Act
        class1.Union(&class2);

        // Assert
        class1.Contains('x').Should().BeTrue();
        class1.Contains('y').Should().BeTrue();
        class1.Contains('z').Should().BeFalse();
    }

    [Fact]
    public void Union_LargeRanges_WorksCorrectly()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('0', '9');  // Digits
        class2.AddRange('a', 'z');  // Lowercase letters

        // Act
        class1.Union(&class2);

        // Assert
        class1.Contains('0').Should().BeTrue();
        class1.Contains('5').Should().BeTrue();
        class1.Contains('9').Should().BeTrue();
        class1.Contains('a').Should().BeTrue();
        class1.Contains('m').Should().BeTrue();
        class1.Contains('z').Should().BeTrue();
        class1.Contains('A').Should().BeFalse();
    }

    #endregion

    #region Intersect Operation Tests

    [Fact]
    public void Intersect_OverlappingSets_ContainsOnlyCommon()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'e'); // a,b,c,d,e
        class2.AddRange('c', 'g'); // c,d,e,f,g

        // Act
        class1.Intersect(&class2);

        // Assert
        class1.Contains('a').Should().BeFalse();
        class1.Contains('b').Should().BeFalse();
        class1.Contains('c').Should().BeTrue();  // Common
        class1.Contains('d').Should().BeTrue();  // Common
        class1.Contains('e').Should().BeTrue();  // Common
        class1.Contains('f').Should().BeFalse();
        class1.Contains('g').Should().BeFalse();
    }

    [Fact]
    public void Intersect_DisjointSets_BecomesEmpty()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'c');
        class2.AddRange('x', 'z');

        // Act
        class1.Intersect(&class2);

        // Assert
        class1.Contains('a').Should().BeFalse();
        class1.Contains('b').Should().BeFalse();
        class1.Contains('c').Should().BeFalse();
        class1.Contains('x').Should().BeFalse();
        class1.Contains('y').Should().BeFalse();
        class1.Contains('z').Should().BeFalse();
    }

    [Fact]
    public void Intersect_WithEmpty_BecomesEmpty()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'z');

        // Act
        class1.Intersect(&class2);

        // Assert
        class1.Contains('a').Should().BeFalse();
        class1.Contains('m').Should().BeFalse();
        class1.Contains('z').Should().BeFalse();
    }

    [Fact]
    public void Intersect_IdenticalSets_RemainsUnchanged()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddChar('a');
        class1.AddChar('b');
        class1.AddChar('c');

        class2.AddChar('a');
        class2.AddChar('b');
        class2.AddChar('c');

        // Act
        class1.Intersect(&class2);

        // Assert
        class1.Contains('a').Should().BeTrue();
        class1.Contains('b').Should().BeTrue();
        class1.Contains('c').Should().BeTrue();
        class1.Contains('d').Should().BeFalse();
    }

    [Fact]
    public void Intersect_SubsetRelation_ContainsSubset()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'z'); // Full alphabet
        class2.AddRange('m', 'p'); // Subset

        // Act
        class1.Intersect(&class2);

        // Assert
        class1.Contains('l').Should().BeFalse();
        class1.Contains('m').Should().BeTrue();
        class1.Contains('n').Should().BeTrue();
        class1.Contains('o').Should().BeTrue();
        class1.Contains('p').Should().BeTrue();
        class1.Contains('q').Should().BeFalse();
    }

    #endregion

    #region Subtract Operation Tests

    [Fact]
    public void Subtract_RemovesCommonElements()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'e'); // a,b,c,d,e
        class2.AddRange('c', 'g'); // c,d,e,f,g

        // Act
        class1.Subtract(&class2);

        // Assert
        class1.Contains('a').Should().BeTrue();  // Kept
        class1.Contains('b').Should().BeTrue();  // Kept
        class1.Contains('c').Should().BeFalse(); // Removed
        class1.Contains('d').Should().BeFalse(); // Removed
        class1.Contains('e').Should().BeFalse(); // Removed
        class1.Contains('f').Should().BeFalse(); // Never in class1
    }

    [Fact]
    public void Subtract_DisjointSets_RemainsUnchanged()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'c');
        class2.AddRange('x', 'z');

        // Act
        class1.Subtract(&class2);

        // Assert
        class1.Contains('a').Should().BeTrue();
        class1.Contains('b').Should().BeTrue();
        class1.Contains('c').Should().BeTrue();
        class1.Contains('x').Should().BeFalse();
    }

    [Fact]
    public void Subtract_IdenticalSets_BecomesEmpty()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'e');
        class2.AddRange('a', 'e');

        // Act
        class1.Subtract(&class2);

        // Assert
        class1.Contains('a').Should().BeFalse();
        class1.Contains('c').Should().BeFalse();
        class1.Contains('e').Should().BeFalse();
    }

    [Fact]
    public void Subtract_EmptySet_RemainsUnchanged()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('a', 'z');

        // Act
        class1.Subtract(&class2);

        // Assert
        class1.Contains('a').Should().BeTrue();
        class1.Contains('m').Should().BeTrue();
        class1.Contains('z').Should().BeTrue();
    }

    [Fact]
    public void Subtract_FromEmpty_RemainsEmpty()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class2.AddRange('a', 'z');

        // Act
        class1.Subtract(&class2);

        // Assert
        class1.Contains('a').Should().BeFalse();
        class1.Contains('m').Should().BeFalse();
        class1.Contains('z').Should().BeFalse();
    }

    [Fact]
    public void Subtract_PartialOverlap_KeepsNonOverlapping()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddRange('0', '9');  // Digits
        class1.AddRange('a', 'z');  // Letters

        class2.AddRange('a', 'z');  // Only letters

        // Act
        class1.Subtract(&class2);

        // Assert
        // Digits should remain
        class1.Contains('0').Should().BeTrue();
        class1.Contains('5').Should().BeTrue();
        class1.Contains('9').Should().BeTrue();

        // Letters should be removed
        class1.Contains('a').Should().BeFalse();
        class1.Contains('m').Should().BeFalse();
        class1.Contains('z').Should().BeFalse();
    }

    #endregion

    #region Clear Operation Tests

    [Fact]
    public void Clear_RemovesAllCharacters()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();
        charClass.AddRange('a', 'z');
        charClass.AddRange('0', '9');

        // Act
        charClass.Clear();

        // Assert
        charClass.Contains('a').Should().BeFalse();
        charClass.Contains('m').Should().BeFalse();
        charClass.Contains('z').Should().BeFalse();
        charClass.Contains('0').Should().BeFalse();
        charClass.Contains('5').Should().BeFalse();
    }

    [Fact]
    public void Clear_OnEmptySet_RemainsEmpty()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();

        // Act
        charClass.Clear();

        // Assert
        charClass.Contains('a').Should().BeFalse();
        charClass.Contains('0').Should().BeFalse();
    }

    [Fact]
    public void Clear_ThenAdd_WorksCorrectly()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();
        charClass.AddRange('a', 'z');

        // Act
        charClass.Clear();
        charClass.AddChar('x');

        // Assert
        charClass.Contains('x').Should().BeTrue();
        charClass.Contains('a').Should().BeFalse();
        charClass.Contains('z').Should().BeFalse();
    }

    #endregion

    #region Invert Operation Tests

    [Fact]
    public void Invert_SingleChar_ExcludesThatChar()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();
        charClass.AddChar('a');

        // Act
        charClass.Invert();

        // Assert
        charClass.Contains('a').Should().BeFalse();
        charClass.Contains('b').Should().BeTrue();
        charClass.Contains('z').Should().BeTrue();
        charClass.Contains('0').Should().BeTrue();
    }

    [Fact]
    public void Invert_Range_ExcludesRange()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();
        charClass.AddRange('a', 'z');

        // Act
        charClass.Invert();

        // Assert
        charClass.Contains('a').Should().BeFalse();
        charClass.Contains('m').Should().BeFalse();
        charClass.Contains('z').Should().BeFalse();
        charClass.Contains('A').Should().BeTrue();
        charClass.Contains('0').Should().BeTrue();
        charClass.Contains('!').Should().BeTrue();
    }

    [Fact]
    public void Invert_TwiceReturnsToOriginal()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();
        charClass.AddChar('a');
        charClass.AddChar('b');
        charClass.AddChar('c');

        // Act
        charClass.Invert();
        charClass.Invert();

        // Assert
        charClass.Contains('a').Should().BeTrue();
        charClass.Contains('b').Should().BeTrue();
        charClass.Contains('c').Should().BeTrue();
        charClass.Contains('d').Should().BeFalse();
    }

    [Fact]
    public void Invert_EmptySet_BecomesFullSet()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();

        // Act
        charClass.Invert();

        // Assert
        // Should contain everything
        charClass.Contains('a').Should().BeTrue();
        charClass.Contains('Z').Should().BeTrue();
        charClass.Contains('0').Should().BeTrue();
        charClass.Contains('!').Should().BeTrue();
        charClass.Contains('\x00').Should().BeTrue();
        charClass.Contains('\xFF').Should().BeTrue();
    }

    #endregion

    #region AddRange Edge Cases

    [Fact]
    public void AddRange_BoundaryCharacters_WorksCorrectly()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();

        // Act
        charClass.AddRange('\x00', '\x00'); // Null character
        charClass.AddRange('!', '!');        // Single character range

        // Assert
        charClass.Contains('\x00').Should().BeTrue();
        charClass.Contains('!').Should().BeTrue();
        charClass.Contains('"').Should().BeFalse();
    }

    [Fact]
    public void AddRange_FullUnicodeRange_WorksCorrectly()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();

        // Act - Add a large range
        charClass.AddRange('\x0000', '\x00FF'); // First 256 characters

        // Assert
        charClass.Contains('\x00').Should().BeTrue();
        charClass.Contains('\x80').Should().BeTrue();
        charClass.Contains('\xFF').Should().BeTrue();
        charClass.Contains('\x100').Should().BeFalse();
    }

    [Fact]
    public void AddRange_MaxChar_HandlesOverflow()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();

        // Act - Range ending at char.MaxValue
        charClass.AddRange('\xFFFE', '\xFFFF');

        // Assert
        charClass.Contains('\xFFFE').Should().BeTrue();
        charClass.Contains('\xFFFF').Should().BeTrue();
        charClass.Contains('\xFFFD').Should().BeFalse();
    }

    #endregion

    #region Complex Operation Combinations

    [Fact]
    public void ComplexOperations_UnionThenSubtract_WorksCorrectly()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();
        CharacterClass class3 = new CharacterClass();

        class1.AddRange('a', 'e'); // a,b,c,d,e
        class2.AddRange('d', 'h'); // d,e,f,g,h
        class3.AddRange('b', 'f'); // b,c,d,e,f

        // Act
        class1.Union(&class2);     // Now: a,b,c,d,e,f,g,h
        class1.Subtract(&class3);  // Remove: b,c,d,e,f; Keep: a,g,h

        // Assert
        class1.Contains('a').Should().BeTrue();
        class1.Contains('b').Should().BeFalse();
        class1.Contains('c').Should().BeFalse();
        class1.Contains('d').Should().BeFalse();
        class1.Contains('e').Should().BeFalse();
        class1.Contains('f').Should().BeFalse();
        class1.Contains('g').Should().BeTrue();
        class1.Contains('h').Should().BeTrue();
    }

    [Fact]
    public void ComplexOperations_IntersectThenUnion_WorksCorrectly()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();
        CharacterClass class3 = new CharacterClass();

        class1.AddRange('a', 'f'); // a,b,c,d,e,f
        class2.AddRange('d', 'h'); // d,e,f,g,h
        class3.AddRange('a', 'b'); // a,b

        // Act
        class1.Intersect(&class2); // Now: d,e,f
        class1.Union(&class3);     // Now: a,b,d,e,f

        // Assert
        class1.Contains('a').Should().BeTrue();
        class1.Contains('b').Should().BeTrue();
        class1.Contains('c').Should().BeFalse();
        class1.Contains('d').Should().BeTrue();
        class1.Contains('e').Should().BeTrue();
        class1.Contains('f').Should().BeTrue();
        class1.Contains('g').Should().BeFalse();
    }

    [Fact]
    public void ComplexOperations_InvertAfterUnion_WorksCorrectly()
    {
        // Arrange
        CharacterClass class1 = new CharacterClass();
        CharacterClass class2 = new CharacterClass();

        class1.AddChar('a');
        class2.AddChar('b');

        // Act
        class1.Union(&class2); // Now: a,b
        class1.Invert();       // Now: everything except a,b

        // Assert
        class1.Contains('a').Should().BeFalse();
        class1.Contains('b').Should().BeFalse();
        class1.Contains('c').Should().BeTrue();
        class1.Contains('z').Should().BeTrue();
    }

    #endregion

    #region Character Class Tests

    [Fact]
    public void Character_IsWhitespace_IdentifiesWhitespace()
    {
        Character.IsWhitespace(' ').Should().BeTrue();
        Character.IsWhitespace('\t').Should().BeTrue();
        Character.IsWhitespace('\n').Should().BeTrue();
        Character.IsWhitespace('\r').Should().BeTrue();
        Character.IsWhitespace('a').Should().BeFalse();
    }

    [Fact]
    public void Character_IsLowerCase_IdentifiesLowercase()
    {
        Character.IsLowerCase('a').Should().BeTrue();
        Character.IsLowerCase('z').Should().BeTrue();
        Character.IsLowerCase('A').Should().BeFalse();
        Character.IsLowerCase('1').Should().BeFalse();
    }

    [Fact]
    public void Character_IsUpperCase_IdentifiesUppercase()
    {
        Character.IsUpperCase('A').Should().BeTrue();
        Character.IsUpperCase('Z').Should().BeTrue();
        Character.IsUpperCase('a').Should().BeFalse();
        Character.IsUpperCase('1').Should().BeFalse();
    }

    [Fact]
    public void Character_IsLetter_IdentifiesLetters()
    {
        Character.IsLetter('a').Should().BeTrue();
        Character.IsLetter('Z').Should().BeTrue();
        Character.IsLetter('1').Should().BeFalse();
        Character.IsLetter('!').Should().BeFalse();
    }

    [Fact]
    public void Character_IsLetterOrDigit_IdentifiesAlphanumeric()
    {
        Character.IsLetterOrDigit('a').Should().BeTrue();
        Character.IsLetterOrDigit('Z').Should().BeTrue();
        Character.IsLetterOrDigit('0').Should().BeTrue();
        Character.IsLetterOrDigit('9').Should().BeTrue();
        Character.IsLetterOrDigit('!').Should().BeFalse();
    }

    [Fact]
    public void Character_IsDigit_IdentifiesDigits()
    {
        Character.IsDigit('0').Should().BeTrue();
        Character.IsDigit('9').Should().BeTrue();
        Character.IsDigit('a').Should().BeFalse();
        Character.IsDigit('A').Should().BeFalse();
    }

    [Fact]
    public void Character_ToLowerCase_ConvertsCorrectly()
    {
        Character.ToLowerCase('A').Should().Be('a');
        Character.ToLowerCase('Z').Should().Be('z');
        Character.ToLowerCase('a').Should().Be('a');
        Character.ToLowerCase('1').Should().Be('1');
    }

    [Fact]
    public void Character_ToUpperCase_ConvertsCorrectly()
    {
        Character.ToUpperCase('a').Should().Be('A');
        Character.ToUpperCase('z').Should().Be('Z');
        Character.ToUpperCase('A').Should().Be('A');
        Character.ToUpperCase('1').Should().Be('1');
    }

    [Fact]
    public void Character_ToTitleCase_ConvertsCorrectly()
    {
        Character.ToTitleCase('a').Should().Be('A');
        Character.ToTitleCase('z').Should().Be('Z');
        Character.ToTitleCase('A').Should().Be('A');
    }

    #endregion

    #region High ASCII and Unicode Tests

    [Fact]
    public void CharacterClass_HighAscii_StoresCorrectly()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();

        // Act
        charClass.AddRange('\x80', '\x9F'); // High ASCII range

        // Assert
        charClass.Contains('\x80').Should().BeTrue();
        charClass.Contains('\x8F').Should().BeTrue();
        charClass.Contains('\x9F').Should().BeTrue();
        charClass.Contains('\x7F').Should().BeFalse();
        charClass.Contains('\xA0').Should().BeFalse();
    }

    [Fact]
    public void CharacterClass_UnicodeCharacters_WorksCorrectly()
    {
        // Arrange
        CharacterClass charClass = new CharacterClass();

        // Act
        charClass.AddChar('\u00E9'); // é
        charClass.AddChar('\u4E2D'); // 中 (Chinese)
        charClass.AddChar('\u0414'); // Д (Cyrillic)

        // Assert
        charClass.Contains('\u00E9').Should().BeTrue();
        charClass.Contains('\u4E2D').Should().BeTrue();
        charClass.Contains('\u0414').Should().BeTrue();
        charClass.Contains('a').Should().BeFalse();
    }

    [Fact]
    public void CharacterClass_BitBoundaries_WorkCorrectly()
    {
        // Test characters that cross 64-bit boundaries in the bitmap
        CharacterClass charClass = new CharacterClass();

        // Act - Add characters around bit boundaries
        charClass.AddChar('\x003F'); // 63 - last char in first ulong
        charClass.AddChar('\x0040'); // 64 - first char in second ulong
        charClass.AddChar('\x007F'); // 127 - last char in second ulong
        charClass.AddChar('\x0080'); // 128 - first char in third ulong

        // Assert
        charClass.Contains('\x003F').Should().BeTrue();
        charClass.Contains('\x0040').Should().BeTrue();
        charClass.Contains('\x007F').Should().BeTrue();
        charClass.Contains('\x0080').Should().BeTrue();
        charClass.Contains('\x003E').Should().BeFalse();
        charClass.Contains('\x0041').Should().BeFalse();
    }

    #endregion
}
