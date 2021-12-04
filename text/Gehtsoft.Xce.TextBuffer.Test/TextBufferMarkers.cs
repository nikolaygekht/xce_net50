using FluentAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBufferMarkers
    {
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
        public void LineInserted_MarkerBeforeLine()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Line = 1;
            buffer.InsertLine(2, "");
            buffer.SavedPositions[0].Line.Should().Be(1);
        }

        [Fact]
        public void LineInserted_MarkerAtLine()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Line = 2;
            buffer.InsertLine(2, "");
            buffer.SavedPositions[0].Line.Should().Be(3);
        }

        [Fact]
        public void LineInserted_MarkerAfterLine()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Line = 3;
            buffer.InsertLine(2, "");
            buffer.SavedPositions[0].Line.Should().Be(4);
        }

        [Fact]
        public void LineRemoved_MarkerBeforeLine()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Line = 1;
            buffer.RemoveLine(2);
            buffer.SavedPositions[0].Line.Should().Be(1);
        }

        [Fact]
        public void LineRemoved_MarkerAtLine()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Line = 2;
            buffer.RemoveLine(2);
            buffer.SavedPositions[0].Line.Should().Be(-1);
        }

        [Fact]
        public void LineRemoved_MarkerAtLine_Cursor()
        {
            using var buffer = CreateTestBuffer();
            buffer.Status.CursorPosition.Line = 2;
            buffer.RemoveLine(2);
            buffer.Status.CursorPosition.Line.Should().Be(2);
        }

        [Fact]
        public void LineRemoved_MarkerAfterLine()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Line = 4;
            buffer.RemoveLine(2);
            buffer.SavedPositions[0].Line.Should().Be(3);
        }

        [Fact]
        public void LineRemoved_MarkerAfterLine_MultipleLines()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Line = 4;
            buffer.RemoveLines(1, 2);
            buffer.SavedPositions[0].Line.Should().Be(2);
        }

        [Fact]
        public void LineRemoved_MarkerWithinRemovedLines()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Line = 2;
            buffer.RemoveLines(1, 2);
            buffer.SavedPositions[0].Line.Should().Be(-1);
        }

        [Fact]
        public void LineRemoved_MarkerWithinRemovedLines_Cursor()
        {
            using var buffer = CreateTestBuffer();
            buffer.Status.CursorPosition.Line = 2;
            buffer.RemoveLines(1, 2);
            buffer.Status.CursorPosition.Line.Should().Be(1);
        }

        [Fact]
        public void SubstringInserted_MarkerBeforeColumn()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Set(4, 1);
            buffer.InsertSubstring(4, 2, "123");
            buffer.SavedPositions[0].EqualsTo(4, 1).Should().Be(true);
        }

        [Fact]
        public void SubstringInserted_MarkerAtColumn()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Set(4, 2);
            buffer.InsertSubstring(4, 2, "123");
            buffer.SavedPositions[0].EqualsTo(4, 5).Should().Be(true);
        }

        [Fact]
        public void SubstringInserted_MarkerAfterColumn()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Set(4, 3);
            buffer.InsertSubstring(4, 2, "123");
            buffer.SavedPositions[0].EqualsTo(4, 6).Should().Be(true);
        }

        [Fact]
        public void SubstringRemoved_MarkerBeforeColumn()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Set(4, 1);
            buffer.RemoveSubstring(4, 2, 2);
            buffer.SavedPositions[0].EqualsTo(4, 1).Should().Be(true);
        }

        [Fact]
        public void SubstringRemoved_MarkerAtColumn()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Set(4, 2);
            buffer.RemoveSubstring(4, 2, 2);
            buffer.SavedPositions[0].Line.Should().Be(-1);
        }

        [Fact]
        public void SubstringRemoved_MarkerAtColumn_Cursor()
        {
            using var buffer = CreateTestBuffer();
            buffer.Status.CursorPosition.Set(4, 3);
            buffer.RemoveSubstring(4, 2, 2);
            buffer.Status.CursorPosition.Line.Should().Be(4);
            buffer.Status.CursorPosition.Column.Should().Be(2);
        }

        [Fact]
        public void SubstringRemoved_MarkerAfterColumn()
        {
            using var buffer = CreateTestBuffer();
            buffer.SavedPositions[0].Set(4, 8);
            buffer.RemoveSubstring(4, 2, 2);
            buffer.SavedPositions[0].EqualsTo(4, 6).Should().Be(true);
        }
    }
}
