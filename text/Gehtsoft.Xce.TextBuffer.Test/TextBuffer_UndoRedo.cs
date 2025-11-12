using System;
using AwesomeAssertions;
using Moq;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBuffer_UndoRedo
    {
        #region Insert Line Undo/Redo Tests

        [Fact]
        public void InsertLine_Undo_RemovesLine()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line3" });

            // Act
            buffer.InsertLine(1, "line2");
            buffer.Undo();

            // Assert
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line3");
        }

        [Fact]
        public void InsertLine_Redo_RestoresLine()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line3" });

            // Act
            buffer.InsertLine(1, "line2");
            buffer.Undo();
            buffer.Redo();

            // Assert
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
        }

        [Fact]
        public void InsertLine_UndoRedo_CallsCallbacks()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1" });
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act
            buffer.InsertLine(1, "line2");
            mock.Reset();
            buffer.Undo();

            // Assert - undo should trigger delete callback
            mock.Verify(cb => cb.OnLinesDeleted(1, 1), Times.Once);

            // Act
            mock.Reset();
            buffer.Redo();

            // Assert - redo should trigger insert callback
            mock.Verify(cb => cb.OnLinesInserted(1, 1), Times.Once);
        }

        #endregion

        #region Delete Line Undo/Redo Tests

        [Fact]
        public void DeleteLine_Undo_RestoresLine()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });

            // Act
            buffer.DeleteLine(1);
            buffer.Undo();

            // Assert
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
        }

        [Fact]
        public void DeleteLine_Redo_RemovesLineAgain()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });

            // Act
            buffer.DeleteLine(1);
            buffer.Undo();
            buffer.Redo();

            // Assert
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line3");
        }

        [Fact]
        public void DeleteLine_UndoRedo_CallsCallbacks()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "line1", "line2", "line3" });
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act
            buffer.DeleteLine(1);
            mock.Reset();
            buffer.Undo();

            // Assert - undo should trigger insert callback
            mock.Verify(cb => cb.OnLinesInserted(1, 1), Times.Once);

            // Act
            mock.Reset();
            buffer.Redo();

            // Assert - redo should trigger delete callback
            mock.Verify(cb => cb.OnLinesDeleted(1, 1), Times.Once);
        }

        #endregion

        #region Insert Substring Undo/Redo Tests

        [Fact]
        public void InsertSubstring_Undo_RemovesSubstring()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello World" });

            // Act
            buffer.InsertSubstring(0, 6, "Beautiful ");
            buffer.Undo();

            // Assert
            buffer.GetLine(0).Should().Be("Hello World");
        }

        [Fact]
        public void InsertSubstring_Redo_RestoresSubstring()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello World" });

            // Act
            buffer.InsertSubstring(0, 6, "Beautiful ");
            buffer.Undo();
            buffer.Redo();

            // Assert
            buffer.GetLine(0).Should().Be("Hello Beautiful World");
        }

        [Fact]
        public void InsertSubstring_UndoRedo_CallsCallbacks()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello World" });
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act
            buffer.InsertSubstring(0, 6, "Beautiful ");
            mock.Reset();
            buffer.Undo();

            // Assert - undo should trigger delete callback
            mock.Verify(cb => cb.OnSubstringDeleted(0, 6, 10), Times.Once);

            // Act
            mock.Reset();
            buffer.Redo();

            // Assert - redo should trigger insert callback
            mock.Verify(cb => cb.OnSubstringInserted(0, 6, 10), Times.Once);
        }

        #endregion

        #region Delete Substring Undo/Redo Tests

        [Fact]
        public void DeleteSubstring_Undo_RestoresSubstring()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello Beautiful World" });

            // Act
            buffer.DeleteSubstring(0, 6, 10);
            buffer.Undo();

            // Assert
            buffer.GetLine(0).Should().Be("Hello Beautiful World");
        }

        [Fact]
        public void DeleteSubstring_Redo_RemovesSubstringAgain()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello Beautiful World" });

            // Act
            buffer.DeleteSubstring(0, 6, 10);
            buffer.Undo();
            buffer.Redo();

            // Assert
            buffer.GetLine(0).Should().Be("Hello World");
        }

        [Fact]
        public void DeleteSubstring_UndoRedo_CallsCallbacks()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello Beautiful World" });
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act
            buffer.DeleteSubstring(0, 6, 10);
            mock.Reset();
            buffer.Undo();

            // Assert - undo should trigger insert callback
            mock.Verify(cb => cb.OnSubstringInserted(0, 6, 10), Times.Once);

            // Act
            mock.Reset();
            buffer.Redo();

            // Assert - redo should trigger delete callback
            mock.Verify(cb => cb.OnSubstringDeleted(0, 6, 10), Times.Once);
        }

        #endregion

        #region Multiple Undo/Redo Tests

        [Fact]
        public void MultipleOperations_Undo_UndoesInReverseOrder()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act - perform multiple operations
            buffer.InsertLine(0, "line1");
            buffer.InsertLine(1, "line2");
            buffer.InsertLine(2, "line3");

            // Undo all operations
            buffer.Undo(); // Removes line3
            buffer.Undo(); // Removes line2
            buffer.Undo(); // Removes line1

            // Assert
            buffer.LinesCount.Should().Be(0);
        }

        [Fact]
        public void MultipleOperations_Redo_RedoesInOriginalOrder()
        {
            // Arrange
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "line1");
            buffer.InsertLine(1, "line2");
            buffer.InsertLine(2, "line3");

            // Undo all
            buffer.Undo();
            buffer.Undo();
            buffer.Undo();

            // Act - redo all
            buffer.Redo(); // Restores line1
            buffer.Redo(); // Restores line2
            buffer.Redo(); // Restores line3

            // Assert
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
        }

        [Fact]
        public void MixedOperations_UndoRedo_WorksCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello" });

            // Act - mix of line and substring operations
            buffer.InsertSubstring(0, 5, " World");
            buffer.InsertLine(1, "Second Line");
            buffer.DeleteSubstring(0, 0, 6);

            // Current state: line0="World", line1="Second Line"

            // Undo last operation
            buffer.Undo();

            // Assert
            buffer.GetLine(0).Should().Be("Hello World");
            buffer.GetLine(1).Should().Be("Second Line");

            // Undo another
            buffer.Undo();

            // Assert
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("Hello World");

            // Redo
            buffer.Redo();

            // Assert
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(1).Should().Be("Second Line");
        }

        [Fact]
        public void NewOperation_ClearsRedoStack()
        {
            // Arrange
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "line1");
            buffer.InsertLine(1, "line2");

            // Undo one operation
            buffer.Undo();

            // Act - perform a new operation
            buffer.InsertLine(1, "different");

            // Assert - redo should no longer be available
            buffer.CanRedo.Should().Be(false);
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("different");
        }

        #endregion

        #region CanUndo/CanRedo Tests

        [Fact]
        public void CanUndo_EmptyBuffer_ReturnsFalse()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Assert
            buffer.CanUndo.Should().Be(false);
        }

        [Fact]
        public void CanUndo_AfterOperation_ReturnsTrue()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act
            buffer.InsertLine(0, "line1");

            // Assert
            buffer.CanUndo.Should().Be(true);
        }

        [Fact]
        public void CanRedo_InitiallyFalse()
        {
            // Arrange
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "line1");

            // Assert
            buffer.CanRedo.Should().Be(false);
        }

        [Fact]
        public void CanRedo_AfterUndo_ReturnsTrue()
        {
            // Arrange
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "line1");

            // Act
            buffer.Undo();

            // Assert
            buffer.CanRedo.Should().Be(true);
        }

        [Fact]
        public void Undo_WhenCannotUndo_Throws()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => buffer.Undo());
        }

        [Fact]
        public void Redo_WhenCannotRedo_Throws()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => buffer.Redo());
        }

        #endregion
    }
}
