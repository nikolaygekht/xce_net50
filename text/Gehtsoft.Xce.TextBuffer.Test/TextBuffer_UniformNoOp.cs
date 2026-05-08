using System;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Pins design D2 (uniform edit semantics): every public edit method always
    /// pushes an undo entry and always invokes the corresponding Owner callback,
    /// even when the edit changes nothing (length-0 callback, no-op undo entry).
    /// Negative arguments still throw and don't push partial entries (D4).
    /// </summary>
    public class TextBuffer_UniformNoOp
    {
        private sealed class CountingSink : ITextBufferCallback
        {
            public int LinesInsertedCalls;
            public int LinesDeletedCalls;
            public int SubstringInsertedCalls;
            public int SubstringDeletedCalls;
            public (int line, int count) LastLinesInserted;
            public (int line, int count) LastLinesDeleted;
            public (int line, int col, int len) LastSubstringInserted;
            public (int line, int col, int len) LastSubstringDeleted;

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
            {
                SubstringInsertedCalls++;
                LastSubstringInserted = (lineIndex, columnIndex, length);
            }

            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
            {
                SubstringDeletedCalls++;
                LastSubstringDeleted = (lineIndex, columnIndex, length);
            }
        }

        // ----------------------------------------------------------------------
        // Empty / no-op edits push uniform undo entries and fire length-0 callbacks.
        // ----------------------------------------------------------------------

        [Fact]
        public void InsertSubstring_EmptyText_PushesUndoEntry_FiresZeroLengthCallback()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            var sink = new CountingSink();
            buffer.Owner = sink;

            buffer.InsertSubstring(0, 1, "");

            // Buffer content unchanged.
            buffer.GetLine(0).Should().Be("abc");
            // Callback fired exactly once with length 0.
            sink.SubstringInsertedCalls.Should().Be(1);
            sink.LastSubstringInserted.Should().Be((0, 1, 0));
            // Undo entry exists and is harmless.
            buffer.CanUndo.Should().BeTrue();
            buffer.Undo();
            buffer.GetLine(0).Should().Be("abc");
        }

        [Fact]
        public void DeleteSubstring_ZeroLength_PushesUndoEntry_FiresZeroLengthCallback()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            var sink = new CountingSink();
            buffer.Owner = sink;

            buffer.DeleteSubstring(0, 0, 0);

            buffer.GetLine(0).Should().Be("abc");
            sink.SubstringDeletedCalls.Should().Be(1);
            sink.LastSubstringDeleted.Should().Be((0, 0, 0));
            buffer.CanUndo.Should().BeTrue();
            buffer.Undo();
            buffer.GetLine(0).Should().Be("abc");
        }

        [Fact]
        public void DeleteSubstring_PastLineEnd_PushesUndoEntry_FiresZeroLengthCallback()
        {
            // Past real content (column far beyond line end). Per D3 the buffer
            // is conceptually infinite empty there, so this is a uniform no-op
            // (with undo entry + length-0 callback).
            var buffer = new TextBuffer(new[] { "abcd" });
            var sink = new CountingSink();
            buffer.Owner = sink;

            buffer.DeleteSubstring(0, 99, 5);

            buffer.GetLine(0).Should().Be("abcd");
            sink.SubstringDeletedCalls.Should().Be(1);
            sink.LastSubstringDeleted.Should().Be((0, 99, 0));
            buffer.CanUndo.Should().BeTrue();
        }

        [Fact]
        public void DeleteSubstring_LineMissing_PushesUndoEntry_FiresZeroLengthCallback()
        {
            var buffer = new TextBuffer(new[] { "only" });
            var sink = new CountingSink();
            buffer.Owner = sink;

            buffer.DeleteSubstring(99, 0, 5);

            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("only");
            sink.SubstringDeletedCalls.Should().Be(1);
            sink.LastSubstringDeleted.Should().Be((99, 0, 0));
            buffer.CanUndo.Should().BeTrue();
        }

        [Fact]
        public void DeleteLine_PastBufferEnd_PushesUndoEntry_FiresZeroCountCallback()
        {
            var buffer = new TextBuffer(new[] { "L0", "L1", "L2" });
            var sink = new CountingSink();
            buffer.Owner = sink;

            buffer.DeleteLine(99);

            buffer.LinesCount.Should().Be(3);
            sink.LinesDeletedCalls.Should().Be(1);
            sink.LastLinesDeleted.Should().Be((99, 0));
            buffer.CanUndo.Should().BeTrue();
            buffer.Undo();
            buffer.LinesCount.Should().Be(3);                              // still unchanged after undo
        }

        [Fact]
        public void EmptyTransaction_PushesUndoEntry_FiresNoCallbacks_UndoIsNoOp()
        {
            var buffer = new TextBuffer(new[] { "anchor" });
            var sink = new CountingSink();
            buffer.Owner = sink;

            using (buffer.BeginUndoTransaction())
            {
                // No operations.
            }

            // Empty transaction body: no callbacks fired during commit.
            sink.LinesInsertedCalls.Should().Be(0);
            sink.LinesDeletedCalls.Should().Be(0);
            sink.SubstringInsertedCalls.Should().Be(0);
            sink.SubstringDeletedCalls.Should().Be(0);

            // But the entry is on the undo stack.
            buffer.CanUndo.Should().BeTrue();

            buffer.Undo();
            buffer.GetLine(0).Should().Be("anchor");                       // no-op unwind
            buffer.CanRedo.Should().BeTrue();

            buffer.Redo();
            buffer.GetLine(0).Should().Be("anchor");                       // no-op redo
        }

        // ----------------------------------------------------------------------
        // Negative arguments throw and do NOT push partial undo entries.
        // ----------------------------------------------------------------------

        [Fact]
        public void NegativeIndices_Throw_AndDoNotPushPartialUndoEntry()
        {
            var buffer = new TextBuffer(new[] { "abc" });
            var sink = new CountingSink();
            buffer.Owner = sink;

            // Sanity: nothing on the undo stack yet.
            buffer.CanUndo.Should().BeFalse();

            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.InsertLine(-1, "x"));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DeleteLine(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.InsertSubstring(-1, 0, "x"));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.InsertSubstring(0, -1, "x"));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DeleteSubstring(-1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DeleteSubstring(0, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => buffer.DeleteSubstring(0, 0, -1));

            // None of the throws should have left undo entries behind.
            buffer.CanUndo.Should().BeFalse();
            // Nor should any callback have fired.
            sink.LinesInsertedCalls.Should().Be(0);
            sink.LinesDeletedCalls.Should().Be(0);
            sink.SubstringInsertedCalls.Should().Be(0);
            sink.SubstringDeletedCalls.Should().Be(0);
        }
    }
}
