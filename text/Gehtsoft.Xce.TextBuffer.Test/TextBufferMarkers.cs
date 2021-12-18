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

        [Theory]
        [InlineData("name", 1, 2, "name", 1, 2, true)]
        [InlineData("name", 1, 2, "name1", 1, 2, false)]
        [InlineData("name", 1, 2, "name", 2, 2, false)]
        [InlineData("name", 1, 2, "name", 1, 3, false)]
        public void Equality(string n1, int line1, int position1, string n2, int line2, int position2, bool expected)
        {
            var m1 = new PositionMarker(n1, line1, position1);
            var m2 = new PositionMarker(n2, line2, position2);

            m1.Equals(m2).Should().Be(expected);
        }

#pragma warning disable RCS1098 // Constant values should be placed on right side of comparisons.
        [Fact]
        public void Equality_WidthNull()
        {
            var m1 = new PositionMarker("1", 1, 2);
            PositionMarker m2 = null;
            PositionMarker m3 = null;

            m1.Equals(null).Should().BeFalse();
            m1.Equals(m2).Should().BeFalse();

            (m1 == null).Should().BeFalse();
            (null == m1).Should().BeFalse();

            (m1 == m2).Should().BeFalse();
            (m2 == m1).Should().BeFalse();
            (m2 == m3).Should().BeTrue();

            (m1 != null).Should().BeTrue();
            (null != m1).Should().BeTrue();
            (m1 != m2).Should().BeTrue();
            (m2 != m1).Should().BeTrue();
            (m2 != m3).Should().BeFalse();
        }
#pragma warning restore RCS1098 // Constant values should be placed on right side of comparisons.

        [Theory]
        [InlineData(2, 3, 2, 3, 0)]
        [InlineData(1, 4, 2, 3, -1)]
        [InlineData(2, 1, 2, 3, -1)]
        [InlineData(2, 4, 2, 3, 1)]
        [InlineData(3, 1, 2, 3, 1)]
        public void Compare(int line1, int position1, int line2, int position2, int expected)
        {
            var m1 = new PositionMarker("1", line1, position1);
            var m2 = new PositionMarker("2", line2, position2);

            m1.CompareTo(m2).Should().Be(expected);
        }

        [Theory]
        [InlineData(2, 3, 2, 3, true)]
        [InlineData(1, 4, 2, 3, false)]
        [InlineData(2, 1, 2, 3, false)]
        [InlineData(2, 4, 2, 3, false)]
        [InlineData(3, 1, 2, 3, false)]
        public void OpEq(int line1, int position1, int line2, int position2, bool expected)
        {
            var m1 = new PositionMarker("n1", line1, position1);
            var m2 = new PositionMarker("n2", line2, position2);

            (m1 == m2).Should().Be(expected);
        }

        [Theory]
        [InlineData(2, 3, 2, 3, false)]
        [InlineData(1, 4, 2, 3, true)]
        [InlineData(2, 1, 2, 3, true)]
        [InlineData(2, 4, 2, 3, true)]
        [InlineData(3, 1, 2, 3, true)]
        public void OpNeq(int line1, int position1, int line2, int position2, bool expected)
        {
            var m1 = new PositionMarker("n1", line1, position1);
            var m2 = new PositionMarker("n2", line2, position2);

            (m1 != m2).Should().Be(expected);
        }

        [Theory]
        [InlineData(2, 3, 2, 3, false)]
        [InlineData(1, 4, 2, 3, false)]
        [InlineData(2, 1, 2, 3, false)]
        [InlineData(2, 4, 2, 3, true)]
        [InlineData(3, 1, 2, 3, true)]
        public void OpGt(int line1, int position1, int line2, int position2, bool expected)
        {
            var m1 = new PositionMarker("n1", line1, position1);
            var m2 = new PositionMarker("n2", line2, position2);

            (m1 > m2).Should().Be(expected);
        }

        [Theory]
        [InlineData(2, 3, 2, 3, true)]
        [InlineData(1, 4, 2, 3, false)]
        [InlineData(2, 1, 2, 3, false)]
        [InlineData(2, 4, 2, 3, true)]
        [InlineData(3, 1, 2, 3, true)]
        public void OpGe(int line1, int position1, int line2, int position2, bool expected)
        {
            var m1 = new PositionMarker("n1", line1, position1);
            var m2 = new PositionMarker("n2", line2, position2);

            (m1 >= m2).Should().Be(expected);
        }

        [Theory]
        [InlineData(2, 3, 2, 3, false)]
        [InlineData(1, 4, 2, 3, true)]
        [InlineData(2, 1, 2, 3, true)]
        [InlineData(2, 4, 2, 3, false)]
        [InlineData(3, 1, 2, 3, false)]
        public void OpLt(int line1, int position1, int line2, int position2, bool expected)
        {
            var m1 = new PositionMarker("n1", line1, position1);
            var m2 = new PositionMarker("n2", line2, position2);

            (m1 < m2).Should().Be(expected);
        }

        [Theory]
        [InlineData(2, 3, 2, 3, true)]
        [InlineData(1, 4, 2, 3, true)]
        [InlineData(2, 1, 2, 3, true)]
        [InlineData(2, 4, 2, 3, false)]
        [InlineData(3, 1, 2, 3, false)]
        public void OpLe(int line1, int position1, int line2, int position2, bool expected)
        {
            var m1 = new PositionMarker("n1", line1, position1);
            var m2 = new PositionMarker("n2", line2, position2);

            (m1 <= m2).Should().Be(expected);
        }
    }
}
