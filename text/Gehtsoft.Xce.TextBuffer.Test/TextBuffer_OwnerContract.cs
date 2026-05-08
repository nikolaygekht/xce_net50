using System;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Pins the contract for the single <see cref="TextBuffer.Owner"/> sink:
    /// nullability, exception propagation with consistent buffer state, and
    /// reassignment routing. (Multi-subscriber tests from the old collection
    /// model are gone with the API.)
    /// </summary>
    public class TextBuffer_OwnerContract
    {
        // ----------------------------------------------------------------------
        // Owner = null: edits succeed without dispatching anywhere, no NRE.
        // ----------------------------------------------------------------------

        [Fact]
        public void Owner_Null_EditsSucceedWithoutThrowing()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.Owner.Should().BeNull();

            // Each kind of edit, no Owner attached.
            buffer.InsertLine(1, "def");
            buffer.InsertSubstring(0, 3, "X");
            buffer.DeleteSubstring(0, 0, 1);
            buffer.DeleteLine(1);

            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("bcX");
            // Each edit pushed an undo entry per D2.
            buffer.CanUndo.Should().BeTrue();
        }

        [Fact]
        public void Owner_NullReassignedFromExisting_StopsRoutingToOldOwner()
        {
            var buffer = new TextBuffer(new[] { "L0" });
            var sink = new RecordingSink();
            buffer.Owner = sink;

            buffer.InsertLine(0, "A");
            sink.LinesInsertedCalls.Should().Be(1);

            buffer.Owner = null;
            buffer.InsertLine(0, "B");
            sink.LinesInsertedCalls.Should().Be(1);                        // unchanged
        }

        // ----------------------------------------------------------------------
        // Owner reassignment between edits: the new owner gets subsequent events;
        // the old owner does not.
        // ----------------------------------------------------------------------

        [Fact]
        public void Owner_ReassignedBetweenEdits_NewReceivesSubsequentOnly()
        {
            var buffer = new TextBuffer(new[] { "L0" });
            var first = new RecordingSink();
            var second = new RecordingSink();

            buffer.Owner = first;
            buffer.InsertLine(0, "A");                                     // routed to first

            buffer.Owner = second;
            buffer.InsertLine(0, "B");                                     // routed to second

            first.LinesInsertedCalls.Should().Be(1);
            first.LastLinesInserted.Should().Be((0, 1));                   // the "A" insert
            second.LinesInsertedCalls.Should().Be(1);
            second.LastLinesInserted.Should().Be((0, 1));                  // the "B" insert
        }

        // ----------------------------------------------------------------------
        // Owner throw during a callback: exception propagates, the buffer
        // mutation is observable, and the undo entry is on the stack so the
        // caller can roll the edit back.
        // ----------------------------------------------------------------------

        [Fact]
        public void Owner_ThrowsOnInsertLine_ExceptionPropagates_BufferConsistent_UndoOnStack()
        {
            var buffer = new TextBuffer(new[] { "L0" });
            buffer.Owner = new ThrowingSink();

            Assert.Throws<InvalidOperationException>(() => buffer.InsertLine(1, "L1"));

            // Buffer is in the post-mutation state — the throw fired AFTER
            // the line was inserted and the undo was registered.
            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(1).Should().Be("L1");

            // Undo entry exists; user can roll the edit back.
            buffer.CanUndo.Should().BeTrue();

            // Detach the throwing owner before undo so we can verify the rollback.
            buffer.Owner = null;
            buffer.Undo();
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("L0");
        }

        [Fact]
        public void Owner_ThrowsOnInsertSubstring_BufferConsistent_UndoOnStack()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.Owner = new ThrowingSink();

            Assert.Throws<InvalidOperationException>(() => buffer.InsertSubstring(0, 1, "X"));

            buffer.GetLine(0).Should().Be("aXbc");
            buffer.CanUndo.Should().BeTrue();

            buffer.Owner = null;
            buffer.Undo();
            buffer.GetLine(0).Should().Be("abc");
        }

        [Fact]
        public void Owner_ThrowsOnDelete_BufferConsistent_UndoOnStack()
        {
            var buffer = new TextBuffer(new[] { "L0", "L1", "L2" });
            buffer.Owner = new ThrowingSink();

            Assert.Throws<InvalidOperationException>(() => buffer.DeleteLine(1));

            buffer.LinesCount.Should().Be(2);
            buffer.GetLine(1).Should().Be("L2");
            buffer.CanUndo.Should().BeTrue();

            buffer.Owner = null;
            buffer.Undo();
            buffer.LinesCount.Should().Be(3);
            buffer.GetLine(1).Should().Be("L1");
        }

        // ----------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------

        private sealed class RecordingSink : ITextBufferCallback
        {
            public int LinesInsertedCalls;
            public int LinesDeletedCalls;
            public int SubstringInsertedCalls;
            public int SubstringDeletedCalls;
            public (int line, int count) LastLinesInserted;
            public (int line, int count) LastLinesDeleted;

            public void OnLinesInserted(int lineIndex, int count)
            {
                LinesInsertedCalls++;
                LastLinesInserted = (lineIndex, count);
            }

            public void OnLinesDeleted(int lineIndex, int count)
            {
                LinesDeletedCalls++;
                LastLinesDeleted = (lineIndex, count);
            }

            public void OnSubstringInserted(int lineIndex, int columnIndex, int length)
                => SubstringInsertedCalls++;

            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
                => SubstringDeletedCalls++;
        }

        private sealed class ThrowingSink : ITextBufferCallback
        {
            public void OnLinesInserted(int lineIndex, int count)
                => throw new InvalidOperationException("owner threw on lines-inserted");
            public void OnLinesDeleted(int lineIndex, int count)
                => throw new InvalidOperationException("owner threw on lines-deleted");
            public void OnSubstringInserted(int lineIndex, int columnIndex, int length)
                => throw new InvalidOperationException("owner threw on substring-inserted");
            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
                => throw new InvalidOperationException("owner threw on substring-deleted");
        }
    }
}
