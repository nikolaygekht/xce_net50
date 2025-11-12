using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBuffer_UndoAutoExtend
    {
        #region Insert Line Beyond Buffer End Tests

        [Fact]
        public void InsertLineBeyondEnd_Undo_RemovesAutoAddedLines()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0", "line1" });

            // Act - insert at line 5 (will auto-add lines 2, 3, 4)
            buffer.InsertLine(5, "line5");

            // Assert - buffer should have 6 lines
            buffer.LinesCount.Should().Be(6);
            buffer.GetLine(0).Should().Be("line0");
            buffer.GetLine(1).Should().Be("line1");
            buffer.GetLine(2).Should().Be("");
            buffer.GetLine(3).Should().Be("");
            buffer.GetLine(4).Should().Be("");
            buffer.GetLine(5).Should().Be("line5");

            // Undo
            buffer.Undo();

            // Assert - should be back to original 2 lines
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("line0");
            buffer.GetLine(1).Should().Be("line1");
        }

        [Fact]
        public void InsertLineBeyondEnd_RedoAfterUndo_RestoresAllLines()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0" });

            // Act
            buffer.InsertLine(3, "line3");
            buffer.Undo();
            buffer.Redo();

            // Assert
            buffer.LinesCount.Should().Be(4);
            buffer.GetLine(0).Should().Be("line0");
            buffer.GetLine(1).Should().Be("");
            buffer.GetLine(2).Should().Be("");
            buffer.GetLine(3).Should().Be("line3");
        }

        #endregion

        #region Insert Substring Beyond Line End Tests

        [Fact]
        public void InsertSubstringBeyondLineEnd_Undo_RemovesAutoAddedSpaces()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hi" });

            // Act - insert at column 10 (will auto-add spaces from 2 to 9)
            buffer.InsertSubstring(0, 10, "World");

            // Assert
            buffer.GetLine(0).Should().Be("Hi        World");

            // Undo
            buffer.Undo();

            // Assert - should be back to original
            buffer.GetLine(0).Should().Be("Hi");
        }

        [Fact]
        public void InsertSubstringBeyondLineEnd_RedoAfterUndo_RestoresSpaces()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "A" });

            // Act
            buffer.InsertSubstring(0, 5, "B");
            buffer.Undo();
            buffer.Redo();

            // Assert
            buffer.GetLine(0).Should().Be("A    B");
        }

        #endregion

        #region Insert Substring Beyond Buffer End Tests

        [Fact]
        public void InsertSubstringBeyondBufferEnd_Undo_RemovesAutoAddedLines()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0" });

            // Act - insert substring on line 3
            buffer.InsertSubstring(3, 0, "text");

            // Assert
            buffer.LinesCount.Should().Be(4);
            buffer.GetLine(3).Should().Be("text");

            // Undo
            buffer.Undo();

            // Assert - back to 1 line
            buffer.LinesCount.Should().Be(1);
        }

        [Fact]
        public void InsertSubstringBeyondBufferAndLineEnd_Undo_RemovesEverything()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "A" });

            // Act - insert at line 2, column 5
            buffer.InsertSubstring(2, 5, "X");

            // Assert
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(0).Should().Be("A");
            buffer.GetLine(1).Should().Be("");
            buffer.GetLine(2).Should().Be("     X");

            // Undo
            buffer.Undo();

            // Assert - back to original
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("A");
        }

        #endregion

        #region Transaction Tests with Auto-Extend

        [Fact]
        public void Transaction_WithAutoExtend_UndoesAsOne()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line0" });

            // Act - transaction with auto-extend
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertSubstring(2, 5, "X");
                buffer.InsertLine(4, "line4");
            }

            // Assert
            buffer.LinesCount.Should().Be(5);

            // Undo
            buffer.Undo();

            // Assert - back to original
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("line0");
        }

        #endregion
    }
}
