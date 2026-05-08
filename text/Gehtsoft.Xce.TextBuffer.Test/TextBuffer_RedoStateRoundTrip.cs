using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Mirror of <see cref="TextBuffer_StateUndoScenarios"/>, but with the focus on
    /// the *redo* leg: an edit (or transactional command) is performed, undone, then
    /// redone. The post-redo state must match the post-edit state exactly, for the
    /// cursor, the primary block, AND the marker collection — together.
    /// </summary>
    public class TextBuffer_RedoStateRoundTrip
    {
        [Fact]
        public void Cursor_BlockAndMarkers_AllMatchPostEditState_AfterUndoThenRedo()
        {
            // The combined "everything at once" redo case. Set up state, perform
            // a sequence of edits, capture the post-edit state, undo, redo, and
            // assert every piece matches the pre-undo (post-edit) snapshot.
            var buffer = new TextBuffer(new[] { "alpha", "beta", "gamma", "delta" });
            buffer.Cursor.SetSelection(line: 1, column: 0, anchorLine: 2, anchorColumn: 5);
            buffer.Block.SetStream(firstLine: 0, firstColumn: 1, lastLine: 1, lastColumn: 3);
            buffer.Markers.Add(new TextMarker("m1", 2, 1));
            buffer.Markers.Add(new TextMarker("m2", 3, 4));

            // A single editing command — outside a transaction.
            buffer.InsertLine(0, "header");

            // Capture post-edit state.
            var postCursor = (buffer.Cursor.Line, buffer.Cursor.Column,
                              buffer.Cursor.AnchorLine, buffer.Cursor.AnchorColumn);
            var postBlock = (buffer.Block.BlockType, buffer.Block.FirstLine,
                             buffer.Block.FirstColumn, buffer.Block.LastLine,
                             buffer.Block.LastColumn);
            var postM1 = (buffer.Markers.FindById("m1").Line, buffer.Markers.FindById("m1").Column);
            var postM2 = (buffer.Markers.FindById("m2").Line, buffer.Markers.FindById("m2").Column);

            buffer.Undo();
            buffer.Redo();

            // After redo, every coordinate must match what we captured post-edit.
            (buffer.Cursor.Line, buffer.Cursor.Column,
             buffer.Cursor.AnchorLine, buffer.Cursor.AnchorColumn).Should().Be(postCursor);
            (buffer.Block.BlockType, buffer.Block.FirstLine,
             buffer.Block.FirstColumn, buffer.Block.LastLine,
             buffer.Block.LastColumn).Should().Be(postBlock);
            (buffer.Markers.FindById("m1").Line, buffer.Markers.FindById("m1").Column).Should().Be(postM1);
            (buffer.Markers.FindById("m2").Line, buffer.Markers.FindById("m2").Column).Should().Be(postM2);
        }

        [Fact]
        public void Transaction_FullState_RoundTripsThroughUndoThenRedo_ExactlyMatchesCommitTime()
        {
            // Same scenario, but the edits sit inside a single transaction. Undoing
            // the transaction wipes everything; redoing must put it ALL back to
            // exactly what we committed.
            var buffer = new TextBuffer(new[] { "L0", "L1", "L2", "L3", "L4" });
            buffer.Cursor.MoveTo(2, 1);
            buffer.Block.SetBox(firstLine: 1, firstColumn: 0, lastLine: 3, lastColumn: 2);
            buffer.Markers.Add(new TextMarker("anchor", 0, 0));
            buffer.Markers.Add(new TextMarker("tail", 4, 1));

            using (buffer.BeginUndoTransaction())
            {
                buffer.DeleteLine(0);
                buffer.DeleteLine(0);
                buffer.InsertLine(0, "new-top");
                buffer.InsertSubstring(0, 0, "// ");
                // Mid-transaction: rearrange state.
                buffer.Cursor.MoveTo(0, 3);
                buffer.Block.SetStream(firstLine: 0, firstColumn: 0, lastLine: 0, lastColumn: 7);
            }

            // Capture post-commit state.
            var postCursor = (buffer.Cursor.Line, buffer.Cursor.Column);
            var postBlock = (buffer.Block.BlockType, buffer.Block.FirstLine,
                             buffer.Block.FirstColumn, buffer.Block.LastLine,
                             buffer.Block.LastColumn);
            var postLine0 = buffer.GetLine(0);
            var postLineCount = buffer.LinesCount;
            var postAnchor = (buffer.Markers.FindById("anchor").Line, buffer.Markers.FindById("anchor").Column);
            var postTail = (buffer.Markers.FindById("tail").Line, buffer.Markers.FindById("tail").Column);

            buffer.Undo();
            buffer.Redo();

            buffer.LinesCount.Should().Be(postLineCount);
            buffer.GetLine(0).Should().Be(postLine0);
            (buffer.Cursor.Line, buffer.Cursor.Column).Should().Be(postCursor);
            (buffer.Block.BlockType, buffer.Block.FirstLine,
             buffer.Block.FirstColumn, buffer.Block.LastLine,
             buffer.Block.LastColumn).Should().Be(postBlock);
            (buffer.Markers.FindById("anchor").Line, buffer.Markers.FindById("anchor").Column).Should().Be(postAnchor);
            (buffer.Markers.FindById("tail").Line, buffer.Markers.FindById("tail").Column).Should().Be(postTail);
        }

        [Fact]
        public void RepeatedUndoRedoCycles_ConvergeToSameState()
        {
            // Stress the snapshot machinery: doing undo→redo→undo→redo... must
            // give the same coordinates every time, with no drift.
            var buffer = new TextBuffer(new[] { "abc", "def", "ghi" });
            buffer.Cursor.SetSelection(0, 1, 1, 2);
            buffer.Markers.Add(new TextMarker("k", 2, 1));

            buffer.InsertSubstring(1, 1, "XX");
            buffer.DeleteLine(0);

            var firstPostState = (
                buffer.LinesCount,
                buffer.GetLine(0),
                buffer.Cursor.Line, buffer.Cursor.Column,
                buffer.Cursor.AnchorLine, buffer.Cursor.AnchorColumn,
                buffer.Markers.FindById("k").Line, buffer.Markers.FindById("k").Column);

            for (int i = 0; i < 5; i++)
            {
                buffer.Undo();
                buffer.Undo();
                buffer.Redo();
                buffer.Redo();
            }

            (buffer.LinesCount,
             buffer.GetLine(0),
             buffer.Cursor.Line, buffer.Cursor.Column,
             buffer.Cursor.AnchorLine, buffer.Cursor.AnchorColumn,
             buffer.Markers.FindById("k").Line, buffer.Markers.FindById("k").Column)
                .Should().Be(firstPostState);
        }

        [Fact]
        public void StreamSelection_OvertypeReplacesIt_RedoLandsExactlyAtCommitTimeState()
        {
            // The "select then type" canonical flow. Undo takes us back to the
            // pre-typing state; redo must reproduce the post-typing state EXACTLY,
            // including the collapsed selection that lives at the end of the typed text.
            var buffer = new TextBuffer(new[] { "Hello, world" });
            buffer.Block.SetStream(firstLine: 0, firstColumn: 7, lastLine: 0, lastColumn: 12);
            buffer.Cursor.MoveTo(0, 12);

            using (buffer.BeginUndoTransaction())
            {
                buffer.DeleteSubstring(0, 7, 5);
                buffer.InsertSubstring(0, 7, "you");
                buffer.Block.Clear();
                buffer.Cursor.MoveTo(0, 10);
            }

            buffer.Undo();
            buffer.Redo();

            buffer.GetLine(0).Should().Be("Hello, you");
            buffer.Block.BlockType.Should().Be(TextBufferBlockType.None);
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(10);
            buffer.Cursor.HasSelection.Should().BeFalse();
        }
    }
}
