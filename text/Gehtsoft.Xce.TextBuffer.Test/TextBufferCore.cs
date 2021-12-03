using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBufferCore
    {
        [Fact]
        public void Access_NewBuffer()
        {
            using var buffer = new TextBuffer();
            buffer.LinesCount.Should().Be(0);
            buffer.Status.CursorPosition.EqualsTo(0, 0).Should().Be(true);
            buffer.Status.BlockStart.EqualsTo(0, 0).Should().Be(true);
            buffer.Status.BlockEnd.EqualsTo(0, 0).Should().Be(true);
            buffer.Status.BlockMode.Should().Be(BlockMode.None);

            buffer.SavedPositions.Should().HaveCount(10);
            buffer.SavedPositions.Should().ContainOnly(pm => pm.Line == -1 && pm.Column == -1);
        }

        [Fact]
        public void Modify_AppendLineToEmpty()
        {
            using var buffer = new TextBuffer();
            buffer.AppendLine("abc");
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("abc");
        }

        [Fact]
        public void Modify_AppendLineToNonEmpty()
        {
            using var buffer = new TextBuffer();
            buffer.AppendLine("abc");
            buffer.AppendLine("def");
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("abc");
            buffer.GetLine(1).Should().Be("def");
        }

        [Fact]
        public void Modify_InsertLineIntoEmpty()
        {
            using var buffer = new TextBuffer();
            buffer.InsertLine(0, "abc");
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("abc");
        }

        [Fact]
        public void Modify_InsertLineNegativePosition()
        {
            using var buffer = new TextBuffer();
            ((Action)(() => buffer.InsertLine(-1, Array.Empty<char>()))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Modify_InsertLineAfterEnd()
        {
            using var buffer = new TextBuffer();
            buffer.InsertLine(3, "abc");
            buffer.LinesCount.Should().Be(4);
            buffer.GetLine(0).Should().Be("");
            buffer.GetLine(1).Should().Be("");
            buffer.GetLine(2).Should().Be("");
            buffer.GetLine(3).Should().Be("abc");
        }

        [Fact]
        public void Modify_InsertLineIntoBeginning()
        {
            using var buffer = new TextBuffer();
            buffer.InsertLine(0, "abc");
            buffer.InsertLine(0, "def");
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("def");
            buffer.GetLine(1).Should().Be("abc");
        }

        [Fact]
        public void Modify_InsertLineIntoMiddle()
        {
            using var buffer = new TextBuffer();
            buffer.AppendLine("abc");
            buffer.AppendLine("def");
            buffer.InsertLine(1, "ijk");
            buffer.LinesCount.Should().Be(3);

            buffer.GetLine(0).Should().Be("abc");
            buffer.GetLine(1).Should().Be("ijk");
            buffer.GetLine(2).Should().Be("def");
        }

        private static TextBuffer CreateTestBuffer()
        {
            var buffer = new TextBuffer();
            buffer.AppendLine("line 1");
            buffer.AppendLine("line 2");
            buffer.AppendLine("line 3");
            buffer.AppendLine("line 4");
            buffer.AppendLine("line 5");
            return buffer;
        }

        [Fact]
        public void Access_GetLineAsString()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetLine(1).Should().Be("line 2");
        }

        [Fact]
        public void Access_GetLineAsArray()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetLine(1, out var target);
            target.Should().BeEquivalentTo(new char[] { 'l', 'i', 'n', 'e', ' ', '2' });
        }

        [Fact]
        public void Access_GetLineIntoArray_ExactLength()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[6];
            buffer.GetLine(1, target, out var length);
            length.Should().Be(6);
            target.Should().BeEquivalentTo(new char[] { 'l', 'i', 'n', 'e', ' ', '2' });
        }

        [Fact]
        public void Access_GetLineIntoArray_Longer()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[10];
            buffer.GetLine(1, target, out var length);
            length.Should().Be(6);
            target.Should().BeEquivalentTo(new char[] { 'l', 'i', 'n', 'e', ' ', '2', '\0', '\0', '\0', '\0'});
        }

        [Fact]
        public void Access_GetLineIntoArray_Shorter()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[4];
            buffer.GetLine(1, target, out var length);
            length.Should().Be(4);
            target.Should().BeEquivalentTo(new char[] { 'l', 'i', 'n', 'e' });
        }

        [Fact]
        public void Access_GetLine_LineOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetLine(-1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Access_GetLine_LineBeyondEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetLine(buffer.LinesCount).Should().Be("");
            buffer.GetLine(buffer.LinesCount + 1).Should().Be("");
        }

        [Fact]
        public void Access_GetLineAsArray_LineOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetLine(-1, out _))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Access_GetLineAsArray_LineBeyondEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetLine(buffer.LinesCount, out var target);
            target.Should().BeEquivalentTo(Array.Empty<char>());
            buffer.GetLine(buffer.LinesCount + 1, out target);
            target.Should().BeEquivalentTo(Array.Empty<char>());
        }

        [Fact]
        public void Access_GetLineToArray_LineOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[1];
            ((Action)(() => buffer.GetLine(-1, target, out _))).Should().Throw<ArgumentOutOfRangeException>();
        }
        
        [Fact]
        public void Access_GetLineToArray_LineBeyondEnd()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[1];
            buffer.GetLine(buffer.LinesCount, target, out var length);
            length.Should().Be(0);
            buffer.GetLine(buffer.LinesCount + 1, target, out length);
            length.Should().Be(0);
        }

        [Fact]
        public void Access_GetLineToArray_NullArray()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetLine(1, null, out _))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Access_GetSubstring_AsString_InRange()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetSubstring(0, 1, 2).Should().Be("in");
        }

        [Fact]
        public void Access_GetSubstring_AsString_ByeondFileEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetSubstring(10, 1, 2).Should().Be("");
        }

        [Fact]
        public void Access_GetSubstring_AsString_TooManyCharacters()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetSubstring(0, 5, 10).Should().Be("1");
        }

        [Fact]
        public void Access_GetSubstring_AsString_BeyondLineEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetSubstring(0, 10, 2).Should().Be("");
        }

        [Fact]
        public void Access_GetSubstring_AsString_LineOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetSubstring(-1, 1, 2))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Access_GetSubstring_AsString_ColumnOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetSubstring(1, -1, 2))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Access_GetSubstring_AsArray_InRange()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetSubstring(0, 1, 2, out var target);
            target.Should().BeEquivalentTo(new char[] { 'i', 'n' });

        }

        [Fact]
        public void Access_GetSubstring_AsArray_BeyondFileEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetSubstring(10, 1, 2, out var target);
            target.Should().BeEquivalentTo(Array.Empty<char>());
        }

        [Fact]
        public void Access_GetSubstring_AsArray_TooManyCharacters()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetSubstring(0, 5, 10, out var target);
            target.Should().BeEquivalentTo(new char[] { '1' });
        }

        [Fact]
        public void Access_GetSubstring_AsArray_BeyondLineEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.GetSubstring(0, 10, 2, out var target);
            target.Should().BeEquivalentTo(Array.Empty<char>());
        }

        [Fact]
        public void Access_GetSubstring_AsArray_LineOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetSubstring(-1, 1, 2, out _))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Access_GetSubstring_AsArray_ColumnOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetSubstring(1, -1, 2, out _))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Access_GetSubstring_ToArray_InRange()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[4];
            buffer.GetSubstring(0, 1, 2, target, out var length);
            length.Should().Be(2);
            target[0].Should().Be('i');
            target[1].Should().Be('n');
        }

        [Fact]
        public void Access_GetSubstring_ToArray_BeyondFileEnd()
        {
            using var buffer = CreateTestBuffer();

            char[] target = new char[4];
            buffer.GetSubstring(10, 1, 2, target, out var length);
            length.Should().Be(0);
        }

        [Fact]
        public void Access_GetSubstring_ToArray_TooManyCharacters()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[4];
            buffer.GetSubstring(0, 5, 10, target, out var length);
            length.Should().Be(1);
            target[0].Should().Be('1');
        }

        [Fact]
        public void Access_GetSubstring_ToArray_TargetTooShort()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[4];
            buffer.GetSubstring(0, 1, 10, target, out var length);
            length.Should().Be(4);
            target[0].Should().Be('i');
            target[1].Should().Be('n');
            target[2].Should().Be('e');
            target[3].Should().Be(' ');
        }

        [Fact]
        public void Access_GetSubstring_ToArray_BeyondLineEnd()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[4];
            buffer.GetSubstring(0, 10, 2, target, out var length);
            length.Should().Be(0);
        }

        [Fact]
        public void Access_GetSubstring_ToArray_LineOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[4];
            ((Action)(() => buffer.GetSubstring(-1, 1, 2, target, out _))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Access_GetSubstring_ToArray_ColumnOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            char[] target = new char[4];
            ((Action)(() => buffer.GetSubstring(1, -1, 2, target, out _))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Access_GetSubstring_ToArray_NullTarget()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetSubstring(1, 1, 2, null, out _))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Modify_InsertSubstring_AtTheBeginning()
        {
            using var buffer = CreateTestBuffer();
            buffer.InsertSubstring(0, 0, "prefix");
            buffer.GetLine(0).Should().Be("prefixline 1");
        }

        [Fact]
        public void Modify_InsertCharacter()
        {
            using var buffer = CreateTestBuffer();
            buffer.InsertCharacter(0, 1, 'p');
            buffer.GetLine(0).Should().Be("lpine 1");
        }

        [Fact]
        public void Modify_InsertSubstring_AtTheEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.InsertSubstring(0, buffer.GetLine(0).Length, "suffix");
            buffer.GetLine(0).Should().Be("line 1suffix");
        }

        [Fact]
        public void Modify_InsertSubstring_AfterEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.InsertSubstring(0, buffer.GetLine(0).Length + 1, "suffix");
            buffer.GetLine(0).Should().Be("line 1 suffix");
        }

        [Fact]
        public void Modify_InsertSubstring_LineAfterEnd_AfterEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.InsertSubstring(6, 4, "suffix");
            buffer.LinesCount.Should().Be(7);
            buffer.GetLine(6).Should().Be("    suffix");
        }

        [Fact]
        public void Modify_InsertSubstring_InMiddle()
        {
            using var buffer = CreateTestBuffer();
            buffer.InsertSubstring(0, 2, "substring");
            buffer.GetLine(0).Should().Be("lisubstringne 1");
        }

        [Fact]
        public void Modify_RemoveOneLine_Correct()
        {
            using var buffer = CreateTestBuffer();
            buffer.RemoveLine(1);
            buffer.LinesCount.Should().Be(4);
            buffer.GetLine(0).Should().Be("line 1");
            buffer.GetLine(1).Should().Be("line 3");
            buffer.GetLine(2).Should().Be("line 4");
            buffer.GetLine(3).Should().Be("line 5");
        }

        [Fact]
        public void Modify_RemoveOneLine_Correct_Last()
        {
            using var buffer = CreateTestBuffer();
            buffer.RemoveLine(4);
            buffer.LinesCount.Should().Be(4);
            buffer.GetLine(0).Should().Be("line 1");
            buffer.GetLine(1).Should().Be("line 2");
            buffer.GetLine(2).Should().Be("line 3");
            buffer.GetLine(3).Should().Be("line 4");
        }

        [Fact]
        public void Modify_RemoveMultipleLine_Correct()
        {
            using var buffer = CreateTestBuffer();
            buffer.RemoveLines(1, 3);
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("line 1");
            buffer.GetLine(1).Should().Be("line 5");
        }

        [Fact]
        public void Modify_RemoveMultipleLine_TooManyLines()
        {
            using var buffer = CreateTestBuffer();
            buffer.RemoveLines(1, 10);
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("line 1");
        }

        [Fact]
        public void Modify_RemoveLine_BeyondEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.RemoveLine(10);
            buffer.LinesCount.Should().Be(5);
            buffer.GetLine(0).Should().Be("line 1");
            buffer.GetLine(1).Should().Be("line 2");
            buffer.GetLine(2).Should().Be("line 3");
            buffer.GetLine(3).Should().Be("line 4");
            buffer.GetLine(4).Should().Be("line 5");
        }

        [Fact]
        public void Modify_RemoveLine_OutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.RemoveLine(-1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Modify_RemoveSubstring_OK()
        {
            using var buffer = CreateTestBuffer();
            buffer.RemoveSubstring(0, 1, 2);
            buffer.LinesCount.Should().Be(5);
            buffer.GetLine(0).Should().Be("le 1");
            buffer.GetLine(1).Should().Be("line 2");
        }

        [Fact]
        public void Modify_RemoveSubstring_LineOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.RemoveSubstring(-1, 0, 1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Modify_RemoveSubstring_ColumnOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.RemoveSubstring(1, -1, 1))).Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void Modify_RemoveSubstring_BeyondFileEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.RemoveSubstring(10, 1, 2);
            buffer.LinesCount.Should().Be(5);
            buffer.GetLine(0).Should().Be("line 1");
            buffer.GetLine(1).Should().Be("line 2");
            buffer.GetLine(2).Should().Be("line 3");
            buffer.GetLine(3).Should().Be("line 4");
            buffer.GetLine(4).Should().Be("line 5");
        }

        [Fact]
        public void Modify_RemoveSubstring_BeyondStringEnd()
        {
            using var buffer = CreateTestBuffer();
            buffer.RemoveSubstring(0, 10, 2);
            buffer.LinesCount.Should().Be(5);
            buffer.GetLine(0).Should().Be("line 1");
            buffer.GetLine(1).Should().Be("line 2");
            buffer.GetLine(2).Should().Be("line 3");
            buffer.GetLine(3).Should().Be("line 4");
            buffer.GetLine(4).Should().Be("line 5");
        }

        [Fact]
        public void Modify_AppendLine_NullString()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.AppendLine((string)null))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Modify_AppendLine_NullArray()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.AppendLine((char[])null))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Modify_InsertLine_NullString()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.InsertLine(0, (string)null))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Modify_InsertLine_NullArray()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.InsertLine(0, (char[])null))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Modify_InsertSubstring_NullString()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.InsertSubstring(0, 0, (string)null))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Modify_InsertSubstring_NullArray()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.InsertSubstring(0, 0, (char[])null))).Should().Throw<ArgumentNullException>();
        }
    }
}
