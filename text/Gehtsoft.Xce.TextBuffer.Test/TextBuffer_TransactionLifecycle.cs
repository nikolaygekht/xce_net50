using System;
using AwesomeAssertions;
using Gehtsoft.Xce.TextBuffer.Undo;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Lifecycle / misuse scenarios for the transaction and undo system.
    /// These tests describe real-life caller mistakes and the contract the buffer
    /// promises in those situations: fail loudly, but never silently corrupt state.
    /// </summary>
    public class TextBuffer_TransactionLifecycle
    {
        #region Phase 1.1 - Wrong-order transaction disposal

        [Fact]
        public void TransactionsDisposedInWrongOrder_OuterFirst_AllowsCorrectOrderedRecovery()
        {
            // Real scenario: developer holds nested transactions in fields (or via
            // try/finally) and accidentally disposes the outer one before the inner.
            // The buffer must reject the wrong-order commit AND keep the transaction
            // stack intact so the developer can still close out in the correct order.
            var buffer = new TextBuffer();

            var outer = buffer.BeginUndoTransaction();
            buffer.InsertLine(0, "outer-line");

            var inner = buffer.BeginUndoTransaction();
            buffer.InsertLine(1, "inner-line");

            // Disposing outer first is a programmer error: the buffer must throw
            // rather than silently committing the wrong scope. Critically, after
            // the throw the inner transaction must still be on the stack -
            // a buggy implementation that pops-before-validating would lose it
            // and leave the buffer permanently confused.
            Assert.Throws<InvalidOperationException>(() => outer.Dispose());

            // Recovery path: dispose in correct order. Inner commits into outer.
            inner.Dispose();

            // Outer can now be re-disposed and commits its accumulated work.
            outer.Dispose();

            buffer.LinesCount.Should().Be(2);
            buffer.CanUndo.Should().Be(true);

            // Single Undo unwinds both inserts as one transactional unit.
            buffer.Undo();
            buffer.LinesCount.Should().Be(0);
        }

        [Fact]
        public void TransactionsDisposedInWrongOrder_BufferRemainsUsableForFutureEdits()
        {
            // After a wrong-order disposal mistake (and proper recovery), the buffer
            // must behave normally for subsequent unrelated editing - no leaked
            // transaction state, no skewed undo stack. Both transactions here are empty
            // so they leave no undo entry; the recovery just needs to clear them off the stack.
            var buffer = new TextBuffer();
            var outer = buffer.BeginUndoTransaction();
            var inner = buffer.BeginUndoTransaction();

            try { outer.Dispose(); } catch (InvalidOperationException) { }
            inner.Dispose();
            outer.Dispose();

            buffer.CanUndo.Should().Be(false); // empty transactions leave no undo entry

            // Continue editing - everything should behave normally.
            buffer.InsertLine(0, "after-recovery");
            buffer.LinesCount.Should().Be(1);

            buffer.Undo();
            buffer.LinesCount.Should().Be(0);
        }

        #endregion

        #region Phase 1.2 - Undo/Redo rejected during open transaction

        [Fact]
        public void Undo_DuringOpenTransaction_ThrowsAndPreservesHistory()
        {
            // Real scenario: a "format document" command wraps its edits in a
            // transaction. While the transaction is still open, an event handler
            // (e.g., keyboard shortcut for Ctrl+Z) fires Undo. The buffer must
            // reject this rather than tearing pre-transaction history apart and
            // leaving the half-built transaction silently committing later.
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "pre-existing");

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(1, "tx-line");

                // Calling Undo here is a programmer error. It would otherwise pop
                // the pre-transaction "pre-existing" insert while the transaction
                // is mid-flight, producing a corrupt history.
                Assert.Throws<InvalidOperationException>(() => buffer.Undo());

                // The pre-transaction action must still be on the undo stack.
                buffer.CanUndo.Should().Be(true);
            }

            // Transaction commits normally on dispose.
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(0).Should().Be("pre-existing");
            buffer.GetLine(1).Should().Be("tx-line");

            // Two undo units exist: the transaction and the pre-existing insert.
            buffer.Undo(); // unwinds the transaction
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("pre-existing");

            buffer.Undo(); // unwinds the pre-existing insert
            buffer.LinesCount.Should().Be(0);
        }

        [Fact]
        public void Redo_DuringOpenTransaction_ThrowsAndPreservesRedoStack()
        {
            // Real scenario: similar to above, but for Redo. After undoing some
            // pre-transaction work, the user starts a transaction. A misfired Redo
            // shortcut must not slip a pre-transaction action back into the buffer
            // while the transaction is open.
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "pre-existing");
            buffer.Undo(); // pre-existing is now on the redo stack

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "tx-line");

                // Misfired Redo while transaction is open - must throw.
                Assert.Throws<InvalidOperationException>(() => buffer.Redo());
            }

            // After the transaction, the buffer holds tx-line. Performing a new
            // operation (the transaction) clears the redo stack per existing rules,
            // so the previously-undone "pre-existing" cannot be redone any more.
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("tx-line");
            buffer.CanRedo.Should().Be(false);
        }

        [Fact]
        public void Undo_DuringNestedTransaction_AlsoThrows()
        {
            // Variant: even at the inner level of a nested transaction, Undo must
            // be rejected. The whole transaction tree is logically pending.
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "pre-existing");

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(1, "outer-line");
                using (buffer.BeginUndoTransaction())
                {
                    buffer.InsertLine(2, "inner-line");
                    Assert.Throws<InvalidOperationException>(() => buffer.Undo());
                }
            }

            buffer.LinesCount.Should().Be(3);
        }

        #endregion

        #region Phase 1.3 - Sub-action exception during undo/redo

        /// <summary>
        /// Test-only undo action that throws on demand. Stands in for any future
        /// extension-supplied IUndoAction (e.g., a buggy IUndoState snapshot in Phase 2)
        /// whose Undo or Redo might throw.
        /// </summary>
        private sealed class ThrowingUndoAction : IUndoAction
        {
            private readonly bool mThrowOnUndo;
            private readonly bool mThrowOnRedo;

            public ThrowingUndoAction(bool throwOnUndo = true, bool throwOnRedo = false)
            {
                mThrowOnUndo = throwOnUndo;
                mThrowOnRedo = throwOnRedo;
            }

            public void Undo()
            {
                if (mThrowOnUndo)
                    throw new InvalidOperationException("simulated extension bug during Undo");
            }

            public void Redo()
            {
                if (mThrowOnRedo)
                    throw new InvalidOperationException("simulated extension bug during Redo");
            }
        }

        [Fact]
        public void Undo_ActionThrows_ClearsHistoryAndThrowsUndoCorrupted()
        {
            // Real scenario: an extension-supplied undo action (or, in Phase 2, an
            // IUndoState snapshot restorer) throws during Undo. The partially-applied
            // state is no longer trustworthy. The buffer must:
            //   - propagate a clear UndoCorruptedException wrapping the original error,
            //   - drop both undo and redo stacks so subsequent code cannot accidentally
            //     replay corrupt history.
            var buffer = new TextBuffer();
            buffer.InsertLine(0, "good-line");          // legit undo entry #1
            buffer.RegisterUndoActionForTesting(new ThrowingUndoAction(throwOnUndo: true));
            buffer.InsertLine(1, "another-good-line");  // legit undo entry #3
            buffer.Undo();                              // undo the second good line
            buffer.CanRedo.Should().Be(true);           // redo stack is non-empty

            // Now undo the ThrowingUndoAction. It will throw.
            var ex = Assert.Throws<UndoCorruptedException>(() => buffer.Undo());

            ex.InnerException.Should().NotBeNull();
            ex.InnerException.Should().BeOfType<InvalidOperationException>();

            // History on both sides must be wiped.
            buffer.CanUndo.Should().Be(false);
            buffer.CanRedo.Should().Be(false);

            // Buffer is still functionally usable for fresh edits.
            buffer.InsertLine(0, "fresh-start");
            buffer.LinesCount.Should().BeGreaterThan(0);
            buffer.CanUndo.Should().Be(true);
        }

        [Fact]
        public void Redo_ActionThrows_ClearsHistoryAndThrowsUndoCorrupted()
        {
            // Same contract for Redo: a throwing action during redo wipes history
            // rather than leaving inconsistent state on either stack.
            var buffer = new TextBuffer();
            buffer.RegisterUndoActionForTesting(new ThrowingUndoAction(throwOnUndo: false, throwOnRedo: true));
            buffer.Undo(); // moves the action onto the redo stack

            var ex = Assert.Throws<UndoCorruptedException>(() => buffer.Redo());

            ex.InnerException.Should().NotBeNull();
            buffer.CanUndo.Should().Be(false);
            buffer.CanRedo.Should().Be(false);
        }

        #endregion
    }
}
