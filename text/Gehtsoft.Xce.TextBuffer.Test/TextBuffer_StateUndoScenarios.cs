using System;
using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Real-world editor scenarios where cursor, primary selection block, and markers
    /// must round-trip through undo/redo. These tests are the "spec" for Phase 2+3 -
    /// every assertion describes what an end-user editing in a real editor expects.
    /// </summary>
    public class TextBuffer_StateUndoScenarios
    {
        // ----------------------------------------------------------------------
        // Cursor scenarios
        // ----------------------------------------------------------------------

        [Fact]
        public void CursorReturnsHome_AfterUndoOfTextInsertion()
        {
            // The bread-and-butter user flow: caret somewhere in the middle of
            // a line, type a character (or a few), press Ctrl+Z. The caret must
            // be back exactly where it was before typing.
            var buffer = new TextBuffer(new[] { "abc", "def", "ghi" });
            buffer.Cursor.MoveTo(line: 1, column: 2);                  // caret after "de"

            buffer.InsertSubstring(1, 2, "X");                          // "deXf"
            buffer.Cursor.Line.Should().Be(1);
            buffer.Cursor.Column.Should().Be(3);                        // moved right past 'X'

            buffer.Undo();
            buffer.Cursor.Line.Should().Be(1);
            buffer.Cursor.Column.Should().Be(2);                        // home position restored
        }

        [Fact]
        public void CursorMovesToEdit_AfterRedo()
        {
            // After undo'ing back home, redo should land the caret at the post-edit
            // position - so a user undoing-then-redoing returns to the same final state.
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.Cursor.MoveTo(0, 1);

            buffer.InsertSubstring(0, 1, "XY");                         // caret -> (0, 3)
            buffer.Undo();                                              // caret -> (0, 1)
            buffer.Redo();

            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(3);
        }

        [Fact]
        public void CursorOnDeletedLine_RestoredExactlyAfterUndo()
        {
            // Caret sits on a line; another part of the program (or a key chord like
            // Ctrl+Shift+K) deletes that exact line. The caret cannot remain on a
            // non-existent line, so the buffer must snap it. Undo must restore the
            // caret to its precise pre-delete position - column included.
            var buffer = new TextBuffer(new[] { "alpha", "beta", "gamma" });
            buffer.Cursor.MoveTo(line: 1, column: 3);                   // inside "beta"

            buffer.DeleteLine(1);                                       // beta is gone
            // Caret must still be on a valid line; column lossy is OK in the forward
            // direction (matches existing marker semantics).
            buffer.Cursor.Line.Should().Be(1);                          // snapped to deletion site

            buffer.Undo();                                              // beta restored
            buffer.Cursor.Line.Should().Be(1);
            buffer.Cursor.Column.Should().Be(3);                        // exact column preserved
        }

        [Fact]
        public void CursorRoundTrips_ThroughFormatDocumentTransaction()
        {
            // "Format document" is a real command: many edits in one transaction.
            // The user's caret may end up anywhere after format runs, but a single
            // Ctrl+Z must put them back exactly where they were before invoking format.
            var buffer = new TextBuffer(new[] { "a", "b", "c", "d" });
            buffer.Cursor.MoveTo(2, 0);

            using (buffer.BeginUndoTransaction())
            {
                // simulate a multi-step format: indent every line, append marker line, etc.
                buffer.InsertSubstring(0, 0, "    ");
                buffer.InsertSubstring(1, 0, "    ");
                buffer.InsertSubstring(2, 0, "    ");
                buffer.InsertSubstring(3, 0, "    ");
                buffer.InsertLine(4, "// formatted");
                // and the format command happens to leave caret at end of file
                buffer.Cursor.MoveTo(4, 12);
            }

            buffer.Undo();
            buffer.Cursor.Line.Should().Be(2);
            buffer.Cursor.Column.Should().Be(0);
        }

        // ----------------------------------------------------------------------
        // Marker scenarios
        // ----------------------------------------------------------------------

        [Fact]
        public void Bookmark_SurvivesLineDeleteWithExactColumn_OnUndo()
        {
            // The headline marker bug from LATEST_FINDINGS.md: a bookmark on a line
            // that gets deleted loses its column to (line, 0) in the forward direction.
            // After undo, the bookmark must be exactly back at its original (line, col).
            var buffer = new TextBuffer(new[] { "line0", "line1", "line2", "line3" });
            buffer.Markers.Add(new TextMarker("bookmark", line: 2, column: 4));

            buffer.DeleteLine(2);                                       // bookmark column lost in forward dir
            buffer.Undo();

            var marker = buffer.Markers.FindById("bookmark");
            marker.Line.Should().Be(2);
            marker.Column.Should().Be(4);                                // RESTORED exactly
        }

        [Fact]
        public void MultipleBookmarks_SurviveMultiLineDelete_OnUndo()
        {
            // A real editor often has multiple bookmarks. After a sweeping delete
            // that wipes some of them and shifts others, undo must restore ALL
            // bookmarks to their original positions.
            var buffer = new TextBuffer(new[] {
                "L0", "L1", "L2", "L3", "L4", "L5", "L6", "L7", "L8", "L9"
            });
            buffer.Markers.Add(new TextMarker("before", 1, 1));
            buffer.Markers.Add(new TextMarker("inside1", 4, 1));
            buffer.Markers.Add(new TextMarker("inside2", 6, 1));
            buffer.Markers.Add(new TextMarker("after", 8, 1));

            // delete lines 3..7 (5 lines including inside1, inside2)
            using (buffer.BeginUndoTransaction())
            {
                for (int i = 0; i < 5; i++) buffer.DeleteLine(3);
            }

            buffer.Undo();

            buffer.Markers.FindById("before").Line.Should().Be(1);
            buffer.Markers.FindById("before").Column.Should().Be(1);
            buffer.Markers.FindById("inside1").Line.Should().Be(4);
            buffer.Markers.FindById("inside1").Column.Should().Be(1);
            buffer.Markers.FindById("inside2").Line.Should().Be(6);
            buffer.Markers.FindById("inside2").Column.Should().Be(1);
            buffer.Markers.FindById("after").Line.Should().Be(8);
            buffer.Markers.FindById("after").Column.Should().Be(1);
        }

        [Fact]
        public void Markers_AddedAndRemoved_DuringTransaction_RoundTripOnUndo()
        {
            // Edge case: a "rename refactor" command runs as a transaction that both
            // edits text AND adds a bookmark for the rename target / removes a stale
            // bookmark. Undo must put the marker collection back to exactly what it
            // was before the transaction - same set of markers, same positions.
            var buffer = new TextBuffer(new[] { "L0", "L1", "L2" });
            var keep = new TextMarker("keep", 0, 0);
            var willRemove = new TextMarker("toRemove", 1, 0);
            buffer.Markers.Add(keep);
            buffer.Markers.Add(willRemove);

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(3, "added");
                buffer.Markers.RemoveById("toRemove");
                buffer.Markers.Add(new TextMarker("addedDuringTx", 3, 0));
            }

            buffer.Markers.Count.Should().Be(2);
            buffer.Markers.FindById("addedDuringTx").Should().NotBeNull();

            buffer.Undo();

            buffer.Markers.Count.Should().Be(2);
            buffer.Markers.FindById("keep").Should().NotBeNull();
            buffer.Markers.FindById("toRemove").Should().NotBeNull();
            buffer.Markers.FindById("addedDuringTx").Should().BeNull();
        }

        [Fact]
        public void Bookmark_TracksInsertionsBeforeIt_AndUndoesCleanly()
        {
            // Bookmark forward-tracking is already correct today; this test just
            // pins that the snapshot doesn't break the existing behaviour.
            var buffer = new TextBuffer(new[] { "L0", "L1", "L2" });
            buffer.Markers.Add(new TextMarker("bm", 1, 0));

            buffer.InsertLine(0, "new");
            buffer.Markers.FindById("bm").Line.Should().Be(2);          // shifted forward

            buffer.Undo();
            buffer.Markers.FindById("bm").Line.Should().Be(1);          // shifted back
        }

        // ----------------------------------------------------------------------
        // Block scenarios
        // ----------------------------------------------------------------------

        [Fact]
        public void StreamSelection_SurvivesEncompassingDelete_OnUndo()
        {
            // Stream selection across lines 2..4. User runs "delete lines 1..5"
            // (e.g., a refactor). Selection collapses in forward direction.
            // Undo must restore the selection to its exact original coordinates.
            var buffer = new TextBuffer(new[] {
                "L0", "L1", "L2-content", "L3-content", "L4-content", "L5", "L6"
            });
            buffer.Block.SetStream(firstLine: 2, firstColumn: 0,
                                   lastLine: 4, lastColumn: 5);

            using (buffer.BeginUndoTransaction())
            {
                for (int i = 0; i < 5; i++) buffer.DeleteLine(1);       // remove L1..L5
            }

            buffer.Undo();

            buffer.Block.BlockType.Should().Be(TextBufferBlockType.Stream);
            buffer.Block.FirstLine.Should().Be(2);
            buffer.Block.FirstColumn.Should().Be(0);
            buffer.Block.LastLine.Should().Be(4);
            buffer.Block.LastColumn.Should().Be(5);
        }

        [Fact]
        public void StreamSelection_OvertypeReplacesIt_UndoRestoresSelection()
        {
            // The single most common real-world editor flow: user has a selection
            // and types, replacing it. The selection is deleted, the typed text
            // inserted at the start of the original selection. Undo must restore
            // both the selection AND the cursor to the moment just before typing.
            var buffer = new TextBuffer(new[] { "Hello, world" });
            buffer.Block.SetStream(firstLine: 0, firstColumn: 7,
                                   lastLine: 0, lastColumn: 12);        // "world" selected
            buffer.Cursor.MoveTo(0, 12);                                // caret at end of selection

            using (buffer.BeginUndoTransaction())
            {
                buffer.DeleteSubstring(0, 7, 5);                        // remove "world"
                buffer.InsertSubstring(0, 7, "you");                    // insert "you"
                buffer.Block.Clear();                                   // selection collapsed
                buffer.Cursor.MoveTo(0, 10);                            // caret after "you"
            }
            buffer.GetLine(0).Should().Be("Hello, you");

            buffer.Undo();

            buffer.GetLine(0).Should().Be("Hello, world");
            buffer.Block.BlockType.Should().Be(TextBufferBlockType.Stream);
            buffer.Block.FirstLine.Should().Be(0);
            buffer.Block.FirstColumn.Should().Be(7);
            buffer.Block.LastLine.Should().Be(0);
            buffer.Block.LastColumn.Should().Be(12);
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(12);
        }

        [Fact]
        public void BoxSelection_SurvivesCommentLinesTransaction_OnUndo()
        {
            // "Comment selected lines" is a typical command: insert "// " at column 0
            // of every line in the selection. Box selection shouldn't shift its
            // columns (box columns are anchored), but the behaviour should still
            // round-trip via undo regardless.
            var buffer = new TextBuffer(new[] { "AAA", "BBB", "CCC", "DDD" });
            buffer.Block.SetBox(firstLine: 1, firstColumn: 0,
                                lastLine: 2, lastColumn: 3);

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertSubstring(1, 0, "// ");
                buffer.InsertSubstring(2, 0, "// ");
            }

            buffer.Undo();

            buffer.Block.BlockType.Should().Be(TextBufferBlockType.Box);
            buffer.Block.FirstLine.Should().Be(1);
            buffer.Block.FirstColumn.Should().Be(0);
            buffer.Block.LastLine.Should().Be(2);
            buffer.Block.LastColumn.Should().Be(3);
        }

        [Fact]
        public void LineSelection_SurvivesFullDelete_OnUndo()
        {
            // User selects whole lines, runs a command that deletes them.
            // Undo must restore the line selection.
            var buffer = new TextBuffer(new[] { "L0", "L1", "L2", "L3", "L4" });
            buffer.Block.SetLine(firstLine: 1, lastLine: 3);

            using (buffer.BeginUndoTransaction())
            {
                for (int i = 0; i < 3; i++) buffer.DeleteLine(1);
            }

            buffer.Undo();

            buffer.Block.BlockType.Should().Be(TextBufferBlockType.Line);
            buffer.Block.FirstLine.Should().Be(1);
            buffer.Block.LastLine.Should().Be(3);
        }

        // ----------------------------------------------------------------------
        // Combined scenarios - the "everything at once" tests
        // ----------------------------------------------------------------------

        [Fact]
        public void CursorBlockAndMarkers_AllRoundTrip_ThroughOneUndo()
        {
            // The combined scenario - everything an editor cares about, all at once.
            // Caret somewhere, selection somewhere, two bookmarks somewhere.
            // A multi-step transactional command rearranges everything.
            // A single Ctrl+Z must put EVERY piece of state back exactly.
            var buffer = new TextBuffer(new[] {
                "alpha", "beta", "gamma", "delta", "epsilon", "zeta"
            });
            buffer.Cursor.MoveTo(line: 3, column: 2);                   // caret at "del|ta"
            buffer.Block.SetStream(firstLine: 1, firstColumn: 0,
                                   lastLine: 2, lastColumn: 5);         // "beta\ngamma"
            buffer.Markers.Add(new TextMarker("m1", 0, 3));             // alp|ha
            buffer.Markers.Add(new TextMarker("m2", 4, 4));             // epsi|lon

            using (buffer.BeginUndoTransaction())
            {
                buffer.DeleteLine(0);
                buffer.DeleteLine(0);
                buffer.InsertLine(0, "new-header");
                buffer.InsertSubstring(0, 0, "// ");
                buffer.Cursor.MoveTo(0, 0);
                buffer.Block.Clear();
            }

            buffer.Undo();

            buffer.Cursor.Line.Should().Be(3);
            buffer.Cursor.Column.Should().Be(2);

            buffer.Block.BlockType.Should().Be(TextBufferBlockType.Stream);
            buffer.Block.FirstLine.Should().Be(1);
            buffer.Block.FirstColumn.Should().Be(0);
            buffer.Block.LastLine.Should().Be(2);
            buffer.Block.LastColumn.Should().Be(5);

            buffer.Markers.FindById("m1").Line.Should().Be(0);
            buffer.Markers.FindById("m1").Column.Should().Be(3);
            buffer.Markers.FindById("m2").Line.Should().Be(4);
            buffer.Markers.FindById("m2").Column.Should().Be(4);
        }

        [Fact]
        public void ManyEditsInSequence_StateRoundTripsAtEachUndoStep()
        {
            // User makes a sequence of independent edits (not one transaction).
            // Each Ctrl+Z must walk back the cursor and any state to the moment
            // just before that specific edit - not jump straight to the start.
            var buffer = new TextBuffer(new[] { "abc" });
            buffer.Cursor.MoveTo(0, 0);

            buffer.InsertSubstring(0, 0, "X");                          // caret -> (0, 1)
            buffer.Cursor.MoveTo(0, 4);                                 // user moves caret manually
            buffer.InsertSubstring(0, 4, "Y");                          // caret -> (0, 5)
            buffer.Cursor.MoveTo(0, 0);                                 // user moves caret again
            buffer.InsertSubstring(0, 0, "Z");                          // caret -> (0, 1)

            buffer.GetLine(0).Should().Be("ZXabcY");

            // First undo: the most recent edit was "insert Z at col 0"; before that
            // edit the caret was at (0, 0), so undo lands the caret there.
            buffer.Undo();
            buffer.GetLine(0).Should().Be("XabcY");
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(0);

            // Second undo: previous edit was "insert Y at col 4"; pre-edit caret was (0, 4).
            buffer.Undo();
            buffer.GetLine(0).Should().Be("Xabc");
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(4);

            // Third undo: original edit was "insert X at col 0"; pre-edit caret was (0, 0).
            buffer.Undo();
            buffer.GetLine(0).Should().Be("abc");
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(0);
        }
    }
}
