using System;
using AwesomeAssertions;
using Moq;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    public class TextBuffer_Transactions
    {
        #region Basic Transaction Tests

        [Fact]
        public void Transaction_MultipleOperations_UndoesAsOne()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act - perform multiple operations within a transaction
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "line1");
                buffer.InsertLine(1, "line2");
                buffer.InsertLine(2, "line3");
            }

            // Assert - buffer has all lines
            buffer.LinesCount.Should().Be(3);

            // Undo the entire transaction
            buffer.Undo();

            // Assert - all lines removed in one undo
            buffer.LinesCount.Should().Be(0);
        }

        [Fact]
        public void Transaction_MultipleOperations_RedoesAsOne()
        {
            // Arrange
            var buffer = new TextBuffer();

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "line1");
                buffer.InsertLine(1, "line2");
                buffer.InsertLine(2, "line3");
            }

            buffer.Undo();

            // Act
            buffer.Redo();

            // Assert - all lines restored in one redo
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(0).Should().Be("line1");
            buffer.GetLine(1).Should().Be("line2");
            buffer.GetLine(2).Should().Be("line3");
        }

        [Fact]
        public void Transaction_MixedOperations_UndoesCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello" });

            // Act - mix of line and substring operations in transaction
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertSubstring(0, 5, " World");
                buffer.InsertLine(1, "Second Line");
                buffer.DeleteSubstring(0, 0, 6);
            }

            // Current state: line0="World", line1="Second Line"
            buffer.GetLine(0).Should().Be("World");
            buffer.GetLine(1).Should().Be("Second Line");

            // Undo entire transaction
            buffer.Undo();

            // Assert - back to original state
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("Hello");
        }

        [Fact]
        public void Transaction_Empty_DoesNotCreateUndoAction()
        {
            // Arrange
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "test");

            // Act - create empty transaction
            using (buffer.BeginUndoTransaction())
            {
                // No operations
            }

            // Undo should undo the insert, not the empty transaction
            buffer.Undo();

            // Assert
            buffer.LinesCount.Should().Be(0);
            buffer.CanUndo.Should().Be(false);
        }

        #endregion

        #region Nested Transaction Tests

        [Fact]
        public void NestedTransactions_UndoesAsOne()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act - nested transactions
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "line1");

                using (buffer.BeginUndoTransaction())
                {
                    buffer.InsertLine(1, "line2");
                    buffer.InsertLine(2, "line3");
                }

                buffer.InsertLine(3, "line4");
            }

            // Assert - all 4 lines exist
            buffer.LinesCount.Should().Be(4);

            // Undo should remove all lines at once (only outermost transaction)
            buffer.Undo();

            // Assert
            buffer.LinesCount.Should().Be(0);
            buffer.CanUndo.Should().Be(false);
        }

        [Fact]
        public void NestedTransactions_ThreeLevels_UndoesAsOne()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act - three levels of nesting
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "outer1");

                using (buffer.BeginUndoTransaction())
                {
                    buffer.InsertLine(1, "middle1");

                    using (buffer.BeginUndoTransaction())
                    {
                        buffer.InsertLine(2, "inner1");
                        buffer.InsertLine(3, "inner2");
                    }

                    buffer.InsertLine(4, "middle2");
                }

                buffer.InsertLine(5, "outer2");
            }

            // Assert - all 6 lines exist
            buffer.LinesCount.Should().Be(6);

            // Undo should remove all lines at once
            buffer.Undo();

            // Assert
            buffer.LinesCount.Should().Be(0);
        }

        #endregion

        #region Transaction and Non-Transaction Mix Tests

        [Fact]
        public void MixedTransactionAndNormal_UndoesCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act - perform mix of transaction and non-transaction operations
            buffer.InsertLine(0, "before");

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(1, "tx1");
                buffer.InsertLine(2, "tx2");
            }

            buffer.InsertLine(3, "after");

            // Assert
            buffer.LinesCount.Should().Be(4);

            // Undo "after"
            buffer.Undo();
            buffer.LinesCount.Should().Be(3);

            // Undo transaction (both tx1 and tx2)
            buffer.Undo();
            buffer.LinesCount.Should().Be(1);

            // Undo "before"
            buffer.Undo();
            buffer.LinesCount.Should().Be(0);
        }

        [Fact]
        public void MultipleTransactions_UndoesIndependently()
        {
            // Arrange
            var buffer = new TextBuffer();

            // Act - two separate transactions
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "tx1-line1");
                buffer.InsertLine(1, "tx1-line2");
            }

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(2, "tx2-line1");
                buffer.InsertLine(3, "tx2-line2");
            }

            // Assert
            buffer.LinesCount.Should().Be(4);

            // Undo second transaction
            buffer.Undo();
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("tx1-line1");
            buffer.GetLine(1).Should().Be("tx1-line2");

            // Undo first transaction
            buffer.Undo();
            buffer.LinesCount.Should().Be(0);
        }

        #endregion

        #region Transaction Callback Tests

        [Fact]
        public void Transaction_CallbacksInvokedDuringOperations()
        {
            // Arrange
            var buffer = new TextBuffer();
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            // Act - operations in transaction
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "line1");
                buffer.InsertLine(1, "line2");
            }

            // Assert - callbacks invoked during transaction operations
            mock.Verify(cb => cb.OnLinesInserted(0, 1), Times.Once);
            mock.Verify(cb => cb.OnLinesInserted(1, 1), Times.Once);
        }

        [Fact]
        public void Transaction_Undo_CallbacksInvokedInReverseOrder()
        {
            // Arrange
            var buffer = new TextBuffer();
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "line1");
                buffer.InsertLine(1, "line2");
            }

            mock.Reset();

            // Act - undo transaction
            buffer.Undo();

            // Assert - callbacks invoked in reverse order (line2 deleted first, then line1)
            mock.Verify(cb => cb.OnLinesDeleted(1, 1), Times.Once);
            mock.Verify(cb => cb.OnLinesDeleted(0, 1), Times.Once);
        }

        [Fact]
        public void Transaction_Redo_CallbacksInvokedInOriginalOrder()
        {
            // Arrange
            var buffer = new TextBuffer();
            var mock = new Mock<ITextBufferCallback>();
            buffer.Callbacks.Add(mock.Object);

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "line1");
                buffer.InsertLine(1, "line2");
            }

            buffer.Undo();
            mock.Reset();

            // Act - redo transaction
            buffer.Redo();

            // Assert - callbacks invoked in original order
            mock.Verify(cb => cb.OnLinesInserted(0, 1), Times.Once);
            mock.Verify(cb => cb.OnLinesInserted(1, 1), Times.Once);
        }

        #endregion

        #region Transaction with Substring Operations Tests

        [Fact]
        public void Transaction_SubstringOperations_UndoesCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "Hello" });

            // Act
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertSubstring(0, 5, " Beautiful");
                buffer.InsertSubstring(0, 15, " World");
            }

            // Current: "Hello Beautiful World"
            buffer.GetLine(0).Should().Be("Hello Beautiful World");

            // Undo
            buffer.Undo();

            // Assert - back to original
            buffer.GetLine(0).Should().Be("Hello");
        }

        [Fact]
        public void Transaction_ComplexEditing_UndoesCorrectly()
        {
            // Arrange
            var buffer = new TextBuffer(new[] { "First", "Second" });

            // Act - complex editing in transaction
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertSubstring(0, 5, " Line");
                buffer.DeleteSubstring(1, 0, 3);
                buffer.InsertLine(2, "Third");
                buffer.DeleteLine(0);
            }

            // Current state: line0="ond", line1="Third"
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("ond");
            buffer.GetLine(1).Should().Be("Third");

            // Undo entire transaction
            buffer.Undo();

            // Assert - back to original
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("First");
            buffer.GetLine(1).Should().Be("Second");
        }

        #endregion

        #region Redo After Transaction Tests

        [Fact]
        public void Transaction_NewOperation_ClearsRedoStack()
        {
            // Arrange
            var buffer = new TextBuffer();

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "line1");
                buffer.InsertLine(1, "line2");
            }

            buffer.Undo();

            // Act - perform new operation
            buffer.InsertLine(0, "different");

            // Assert - redo should no longer be available
            buffer.CanRedo.Should().Be(false);
        }

        [Fact]
        public void Transaction_NewTransaction_ClearsRedoStack()
        {
            // Arrange
            var buffer = new TextBuffer();

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "tx1");
            }

            buffer.Undo();

            // Act - new transaction
            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "tx2");
            }

            // Assert
            buffer.CanRedo.Should().Be(false);
        }

        #endregion
    }
}
