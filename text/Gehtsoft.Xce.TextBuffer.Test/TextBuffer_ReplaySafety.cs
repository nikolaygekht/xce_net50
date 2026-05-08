using System;
using System.Collections.Generic;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Replay-safety contract for Undo/Redo:
    /// <list type="bullet">
    /// <item>during replay, Owner fan-out is suppressed; the buffer's internal state
    /// (line content, cursor, block, markers) reaches its post-replay form before
    /// any Owner method is called;</item>
    /// <item>queued events are flushed to Owner bracketed by
    /// <c>OnReplayBegin</c> / <c>OnReplayEnd</c>;</item>
    /// <item>an Owner that throws during the flushed events cannot leave the buffer
    /// in a half-unwound state.</item>
    /// </list>
    /// </summary>
    public class TextBuffer_ReplaySafety
    {
        // ----------------------------------------------------------------------
        // Owner sees a Begin → events → End batch on Undo/Redo
        // ----------------------------------------------------------------------

        [Fact]
        public void Undo_FiresReplayBeginBeforeEvents_ReplayEndAfter()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.InsertSubstring(0, 1, "X");          // pre-replay edits
            var sink = new RecordingSink();
            buffer.Owner = sink;

            buffer.Undo();

            sink.Events.Should().StartWith("ReplayBegin");
            sink.Events.Should().EndWith("ReplayEnd");
            sink.Events.Should().Contain(e => e.StartsWith("SubstringDeleted"));
            sink.ReplayBeginCalls.Should().Be(1);
            sink.ReplayEndCalls.Should().Be(1);
        }

        [Fact]
        public void Redo_FiresReplayBeginBeforeEvents_ReplayEndAfter()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.InsertSubstring(0, 1, "X");
            buffer.Undo();
            var sink = new RecordingSink();
            buffer.Owner = sink;

            buffer.Redo();

            sink.Events.Should().StartWith("ReplayBegin");
            sink.Events.Should().EndWith("ReplayEnd");
            sink.Events.Should().Contain(e => e.StartsWith("SubstringInserted"));
        }

        [Fact]
        public void Undo_NoOpAction_StillFiresReplayBeginAndEnd()
        {
            // A no-op edit (delete past real content) still pushes an undo entry per D2.
            // Replaying it queues a length-0 callback; Begin/End must still bracket.
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.DeleteSubstring(0, 100, 5);
            var sink = new RecordingSink();
            buffer.Owner = sink;

            buffer.Undo();

            sink.ReplayBeginCalls.Should().Be(1);
            sink.ReplayEndCalls.Should().Be(1);
        }

        [Fact]
        public void Undo_NoOwner_DoesNotThrow()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.InsertSubstring(0, 1, "X");
            buffer.Owner.Should().BeNull();

            buffer.Invoking(b => b.Undo()).Should().NotThrow();
            buffer.GetLine(0).Should().Be("abc");
        }

        // ----------------------------------------------------------------------
        // The audit's regression case: undoing an auto-extended insert.
        // Owner that throws on the FIRST queued event must NOT leave the buffer
        // half-unwound: the line content is already restored before flushing.
        // ----------------------------------------------------------------------

        [Fact]
        public void Undo_AutoExtendedInsert_OwnerThrowsOnFirstEvent_BufferFullyRestored()
        {
            var buffer = new TextBuffer(new[] { "hello" });

            // InsertSubstring at column 10 on a 5-char line auto-pads with 5 spaces,
            // then inserts "X", giving "hello     X".
            buffer.InsertSubstring(0, 10, "X");
            buffer.GetLine(0).Should().Be("hello     X");

            // Owner throws on the very first queued event during the undo flush.
            var sink = new ThrowOnFirstEventSink();
            buffer.Owner = sink;

            // The Undo itself does the line work silently (Owner suppressed); the
            // throw happens AFTER the buffer is already back to "hello".
            Assert.Throws<InvalidOperationException>(() => buffer.Undo());

            // Critical property: no stray padding spaces left behind.
            buffer.GetLine(0).Should().Be("hello");
            buffer.LinesCount.Should().Be(1);

            // Begin fired, End fired in the finally even though an event threw.
            sink.ReplayBeginCalls.Should().Be(1);
            sink.ReplayEndCalls.Should().Be(1);

            // History remains intact: the action is on the redo stack, ready to replay.
            buffer.CanRedo.Should().BeTrue();
        }

        [Fact]
        public void Redo_AutoExtendedInsert_OwnerThrowsOnFirstEvent_BufferFullyApplied()
        {
            var buffer = new TextBuffer(new[] { "hello" });
            buffer.InsertSubstring(0, 10, "X");
            buffer.Undo();
            buffer.GetLine(0).Should().Be("hello");

            var sink = new ThrowOnFirstEventSink();
            buffer.Owner = sink;

            Assert.Throws<InvalidOperationException>(() => buffer.Redo());

            // Buffer reflects the redone state in full.
            buffer.GetLine(0).Should().Be("hello     X");
            sink.ReplayEndCalls.Should().Be(1);
            buffer.CanUndo.Should().BeTrue();
        }

        // ----------------------------------------------------------------------
        // Owner-throw on flushed events still triggers OnReplayEnd in finally.
        // ----------------------------------------------------------------------

        [Fact]
        public void Owner_ThrowsOnFlushedEvent_ReplayEndStillFires()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.InsertSubstring(0, 0, "X");

            var sink = new ThrowOnFirstEventSink();
            buffer.Owner = sink;

            Assert.Throws<InvalidOperationException>(() => buffer.Undo());

            sink.ReplayBeginCalls.Should().Be(1);
            sink.ReplayEndCalls.Should().Be(1);
        }

        [Fact]
        public void Owner_ThrowsOnReplayBegin_ReplayEndDoesNotFire()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.InsertSubstring(0, 0, "X");

            var sink = new ThrowOnReplayBeginSink();
            buffer.Owner = sink;

            Assert.Throws<InvalidOperationException>(() => buffer.Undo());

            sink.ReplayBeginCalls.Should().Be(1);
            sink.ReplayEndCalls.Should().Be(0);     // Begin never returned normally
        }

        // ----------------------------------------------------------------------
        // Internal trackers (cursor, markers) reach the post-replay state BEFORE
        // any Owner event is delivered. This is what makes the replay atomic.
        // ----------------------------------------------------------------------

        [Fact]
        public void DuringFlush_BufferStateIsAlreadyRestored()
        {
            var buffer = new TextBuffer(new[] { "hello" });
            buffer.Cursor.MoveTo(0, 0);
            buffer.InsertSubstring(0, 0, "X");
            // After the insert: line is "Xhello", cursor at (0, 1).
            buffer.Cursor.Column.Should().Be(1);

            // Capture state from inside Owner's first event handler.
            var inspector = new BufferInspectingSink(buffer);
            buffer.Owner = inspector;

            buffer.Undo();

            // At the moment Owner saw OnReplayBegin and the first On*Deleted event,
            // the line content and cursor were already at their post-undo values.
            inspector.LineAtBegin.Should().Be("hello");
            inspector.CursorColAtBegin.Should().Be(0);
            inspector.LineAtFirstEvent.Should().Be("hello");
            inspector.CursorColAtFirstEvent.Should().Be(0);
        }

        // ----------------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------------

        private sealed class RecordingSink : ITextBufferCallback
        {
            public List<string> Events { get; } = new List<string>();
            public int ReplayBeginCalls;
            public int ReplayEndCalls;

            public void OnLinesInserted(int lineIndex, int count) => Events.Add($"LinesInserted({lineIndex},{count})");
            public void OnLinesDeleted(int lineIndex, int count) => Events.Add($"LinesDeleted({lineIndex},{count})");
            public void OnSubstringInserted(int lineIndex, int columnIndex, int length) => Events.Add($"SubstringInserted({lineIndex},{columnIndex},{length})");
            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length) => Events.Add($"SubstringDeleted({lineIndex},{columnIndex},{length})");
            public void OnReplayBegin() { ReplayBeginCalls++; Events.Add("ReplayBegin"); }
            public void OnReplayEnd() { ReplayEndCalls++; Events.Add("ReplayEnd"); }
        }

        private sealed class ThrowOnFirstEventSink : ITextBufferCallback
        {
            public int ReplayBeginCalls;
            public int ReplayEndCalls;
            private bool mFirst = true;

            public void OnLinesInserted(int lineIndex, int count) => MaybeThrow();
            public void OnLinesDeleted(int lineIndex, int count) => MaybeThrow();
            public void OnSubstringInserted(int lineIndex, int columnIndex, int length) => MaybeThrow();
            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length) => MaybeThrow();
            public void OnReplayBegin() => ReplayBeginCalls++;
            public void OnReplayEnd() => ReplayEndCalls++;

            private void MaybeThrow()
            {
                if (mFirst)
                {
                    mFirst = false;
                    throw new InvalidOperationException("owner threw on first replay event");
                }
            }
        }

        private sealed class ThrowOnReplayBeginSink : ITextBufferCallback
        {
            public int ReplayBeginCalls;
            public int ReplayEndCalls;

            public void OnLinesInserted(int lineIndex, int count) { }
            public void OnLinesDeleted(int lineIndex, int count) { }
            public void OnSubstringInserted(int lineIndex, int columnIndex, int length) { }
            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length) { }
            public void OnReplayBegin()
            {
                ReplayBeginCalls++;
                throw new InvalidOperationException("owner threw on replay begin");
            }
            public void OnReplayEnd() => ReplayEndCalls++;
        }

        private sealed class BufferInspectingSink : ITextBufferCallback
        {
            private readonly TextBuffer mBuffer;
            private bool mSeenFirstEvent;

            public string LineAtBegin;
            public int CursorColAtBegin;
            public string LineAtFirstEvent;
            public int CursorColAtFirstEvent;

            public BufferInspectingSink(TextBuffer buffer) { mBuffer = buffer; }

            public void OnLinesInserted(int lineIndex, int count) => CaptureFirstEvent();
            public void OnLinesDeleted(int lineIndex, int count) => CaptureFirstEvent();
            public void OnSubstringInserted(int lineIndex, int columnIndex, int length) => CaptureFirstEvent();
            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length) => CaptureFirstEvent();

            public void OnReplayBegin()
            {
                LineAtBegin = mBuffer.GetLine(0);
                CursorColAtBegin = mBuffer.Cursor.Column;
            }

            public void OnReplayEnd() { }

            private void CaptureFirstEvent()
            {
                if (mSeenFirstEvent) return;
                mSeenFirstEvent = true;
                LineAtFirstEvent = mBuffer.GetLine(0);
                CursorColAtFirstEvent = mBuffer.Cursor.Column;
            }
        }
    }
}
