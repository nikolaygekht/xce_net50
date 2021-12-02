using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TestTextBuffer
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
            buffer.GetLine(buffer.LinesCount).Should().Be("");
            buffer.GetLine(buffer.LinesCount + 1).Should().Be("");
        }

        [Fact]
        public void Access_GetLineAsArray_LineOutOfRange()
        {
            using var buffer = CreateTestBuffer();
            ((Action)(() => buffer.GetLine(-1, out _))).Should().Throw<ArgumentOutOfRangeException>();

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
        public void Modify_InsertSubstring_AtTheBeginning()
        {
            using var buffer = CreateTestBuffer();
            buffer.InsertSubstring(0, 0, "prefix");
            buffer.GetLine(0).Should().Be("prefixline 1");
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
        public void Modify_InsertSubstring_InMiddle()
        {
            using var buffer = CreateTestBuffer();
            buffer.InsertSubstring(0, 2, "substring");
            buffer.GetLine(0).Should().Be("lisubstringne 1");
        }
    }
}
