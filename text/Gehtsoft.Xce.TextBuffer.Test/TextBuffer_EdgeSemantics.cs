using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Pins the boundary-case behaviour that today is implicit. Each test describes
    /// a real-life editor scenario at a boundary that's easy to "fix" the wrong way.
    /// If a future change breaks one of these, the contract has shifted intentionally
    /// and the test failure makes the user notice.
    /// </summary>
    public class TextBuffer_EdgeSemantics
    {
        // ----------------------------------------------------------------------
        // Stream block edge semantics — the asymmetry between FirstColumn (<=)
        // and LastColumn (<) is intentional: insertions exactly at either end
        // land OUTSIDE the block. Typing at a selection boundary should not be
        // swallowed into the selection.
        // ----------------------------------------------------------------------

        [Fact]
        public void StreamBlock_InsertExactlyAtFirstColumn_LandsBeforeBlock()
        {
            // User scenario: caret at the left edge of a selection ("|world"), they type 'X'.
            // The 'X' should become part of the leading text, not the selection.
            // i.e. selection content stays "world", block shifts right to skip the 'X'.
            var buffer = new TextBuffer(new[] { "Hello, world" });
            buffer.Block.SetStream(0, 7, 0, 12);                            // "world"

            buffer.InsertSubstring(0, 7, "X");

            buffer.GetLine(0).Should().Be("Hello, Xworld");
            buffer.Block.FirstColumn.Should().Be(8);                        // shifted right
            buffer.Block.LastColumn.Should().Be(13);
        }

        [Fact]
        public void StreamBlock_InsertExactlyAtLastColumn_LandsAfterBlock()
        {
            // User scenario: caret at the right edge of a selection ("world|"), they type 'X'.
            // The 'X' should become trailing text, not be swallowed into the selection.
            var buffer = new TextBuffer(new[] { "Hello, world" });
            buffer.Block.SetStream(0, 7, 0, 12);                            // "world"

            buffer.InsertSubstring(0, 12, "X");

            buffer.GetLine(0).Should().Be("Hello, worldX");
            buffer.Block.FirstColumn.Should().Be(7);                        // unchanged
            buffer.Block.LastColumn.Should().Be(12);                        // unchanged - 'X' is outside
        }

        [Fact]
        public void StreamBlock_InsertOneInsideLastColumn_GrowsBlockToInclude()
        {
            // Insert STRICTLY inside the selection - block grows to include the new text.
            var buffer = new TextBuffer(new[] { "Hello, world" });
            buffer.Block.SetStream(0, 7, 0, 12);                            // "world"

            buffer.InsertSubstring(0, 11, "X");                             // between 'l' and 'd'

            buffer.GetLine(0).Should().Be("Hello, worlXd");
            buffer.Block.FirstColumn.Should().Be(7);
            buffer.Block.LastColumn.Should().Be(13);                        // grew by 1
        }

        [Fact]
        public void StreamBlock_DeleteExactlyAtFirstColumn_BlockUnchanged()
        {
            // Deleting characters AT FirstColumn (i.e., just inside the selection's start)
            // shouldn't shift FirstColumn left - the block's left edge stays put while
            // its content shrinks. Compare with deletion BEFORE FirstColumn which would
            // shift the block left.
            var buffer = new TextBuffer(new[] { "Hello, world" });
            buffer.Block.SetStream(0, 7, 0, 12);                            // "world"

            buffer.DeleteSubstring(0, 7, 1);                                // delete 'w'

            buffer.GetLine(0).Should().Be("Hello, orld");
            buffer.Block.FirstColumn.Should().Be(7);                        // unchanged
            buffer.Block.LastColumn.Should().Be(11);                        // shrunk by 1
        }

        [Fact]
        public void StreamBlock_DeleteEndingExactlyAtFirstColumn_ShiftsBlockLeft()
        {
            // Deletion ENTIRELY before FirstColumn (deletedLast == FirstColumn) shifts
            // the block left - no overlap, just text removed before it.
            var buffer = new TextBuffer(new[] { "Hello, world" });
            buffer.Block.SetStream(0, 7, 0, 12);                            // "world"

            buffer.DeleteSubstring(0, 5, 2);                                // delete ", "

            buffer.GetLine(0).Should().Be("Helloworld");
            buffer.Block.FirstColumn.Should().Be(5);                        // shifted left by 2
            buffer.Block.LastColumn.Should().Be(10);                        // shifted left by 2
        }

        [Fact]
        public void StreamBlock_DeleteOverlappingFirstColumn_ClipsToDeletionStart()
        {
            // Deletion straddling FirstColumn (some before, some inside the block):
            // the block's left edge moves to the deletion start, content shrinks accordingly.
            var buffer = new TextBuffer(new[] { "Hello, world" });
            buffer.Block.SetStream(0, 7, 0, 12);                            // "world"

            buffer.DeleteSubstring(0, 5, 4);                                // delete ", wo"

            buffer.GetLine(0).Should().Be("Hellorld");
            buffer.Block.FirstColumn.Should().Be(5);                        // moved to deletion start
            buffer.Block.LastColumn.Should().Be(8);                         // 12 - 4
        }

        // ----------------------------------------------------------------------
        // Box block — columns are anchored. Substring edits never shift them.
        // Real-world rationale: box selections describe a rectangle of column
        // positions the user picked. Editing text inside or beside the box
        // doesn't change which columns the user is targeting.
        // ----------------------------------------------------------------------

        [Fact]
        public void BoxBlock_InsertInsideBox_ColumnsStayAnchored()
        {
            var buffer = new TextBuffer(new[] { "AAAAA", "BBBBB", "CCCCC", "DDDDD" });
            buffer.Block.SetBox(1, 1, 2, 4);                                // 2x3 box

            buffer.InsertSubstring(1, 2, "XY");                             // insert inside box

            buffer.Block.FirstColumn.Should().Be(1);                        // unchanged
            buffer.Block.LastColumn.Should().Be(4);                         // unchanged
        }

        [Fact]
        public void BoxBlock_DeleteBeforeBox_ColumnsStayAnchored()
        {
            var buffer = new TextBuffer(new[] { "AAAAA", "BBBBB", "CCCCC" });
            buffer.Block.SetBox(0, 2, 2, 4);

            buffer.DeleteSubstring(1, 0, 2);                                // delete chars before box on line 1

            buffer.Block.FirstColumn.Should().Be(2);                        // anchored - unchanged
            buffer.Block.LastColumn.Should().Be(4);                         // anchored - unchanged
        }

        // ----------------------------------------------------------------------
        // Auto-extension fires multiple OnLinesInserted / OnSubstringInserted
        // events for a single logical InsertSubstring past line/buffer end.
        // External listeners must expect this; the buffer treats them as one
        // undo unit.
        // ----------------------------------------------------------------------

        [Fact]
        public void InsertSubstring_PastBufferEnd_FiresLinesInsertedAndSubstringInserted()
        {
            // Real scenario: a "go to line N and write" command runs InsertSubstring
            // with a line index past the current end. The buffer auto-creates lines
            // and inserts the text. Listeners (UI redraw, syntax highlighter) see
            // multiple distinct events but should treat them as one logical edit
            // (one Undo unwinds them all).
            var buffer = new TextBuffer(new[] { "first" });
            var recorder = new EventRecorder();
            buffer.Callbacks.Add(recorder);

            buffer.InsertSubstring(3, 0, "hello");

            recorder.Events.Should().Equal(new[] {
                "OnLinesInserted(1, 3)",
                "OnSubstringInserted(3, 0, 5)",
            });

            // One Undo unwinds everything.
            buffer.Undo();
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("first");
        }

        [Fact]
        public void InsertSubstring_PastLineEnd_FiresSpacesEventThenTextEvent()
        {
            // When inserting past an existing line's end, the buffer first pads
            // with spaces (firing one OnSubstringInserted for the spaces) and
            // then inserts the actual text (firing another). One logical edit,
            // two distinct events.
            var buffer = new TextBuffer(new[] { "ab" });
            var recorder = new EventRecorder();
            buffer.Callbacks.Add(recorder);

            buffer.InsertSubstring(0, 5, "X");

            recorder.Events.Should().Equal(new[] {
                "OnSubstringInserted(0, 2, 3)",     // 3 padding spaces
                "OnSubstringInserted(0, 5, 1)",     // actual 'X'
            });

            buffer.Undo();
            buffer.GetLine(0).Should().Be("ab");
        }

        [Fact]
        public void InsertLine_PastBufferEnd_FiresAutoExtendThenInsert()
        {
            // Same dual-event pattern for InsertLine: when the requested lineIndex is
            // past the current buffer end, the auto-extension fires its own event before
            // the actual line insert.
            var buffer = new TextBuffer(new[] { "first" });
            var recorder = new EventRecorder();
            buffer.Callbacks.Add(recorder);

            buffer.InsertLine(4, "appended");

            recorder.Events.Should().Equal(new[] {
                "OnLinesInserted(1, 3)",            // lines 1..3 auto-added
                "OnLinesInserted(4, 1)",            // requested line inserted
            });

            buffer.Undo();
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("first");
        }

        [Fact]
        public void InsertSubstring_PastLineAndBufferEnd_FiresAllThreeEventsInOrder()
        {
            // Worst case: insert past both buffer end AND line end. Three events,
            // one logical edit, one Undo.
            var buffer = new TextBuffer(new[] { "first" });
            var recorder = new EventRecorder();
            buffer.Callbacks.Add(recorder);

            buffer.InsertSubstring(3, 4, "X");

            recorder.Events.Should().Equal(new[] {
                "OnLinesInserted(1, 3)",            // lines 1..3 auto-added
                "OnSubstringInserted(3, 0, 4)",     // 4 padding spaces
                "OnSubstringInserted(3, 4, 1)",     // 'X'
            });

            buffer.Undo();
            buffer.LinesCount.Should().Be(1);
            buffer.GetLine(0).Should().Be("first");
        }

        /// <summary>Records ITextBufferCallback events as strings so tests can assert exact sequence.</summary>
        private sealed class EventRecorder : ITextBufferCallback
        {
            public System.Collections.Generic.List<string> Events { get; } = new();
            public void OnLinesInserted(int lineIndex, int count)
                => Events.Add($"OnLinesInserted({lineIndex}, {count})");
            public void OnLinesDeleted(int lineIndex, int count)
                => Events.Add($"OnLinesDeleted({lineIndex}, {count})");
            public void OnSubstringInserted(int lineIndex, int columnIndex, int length)
                => Events.Add($"OnSubstringInserted({lineIndex}, {columnIndex}, {length})");
            public void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
                => Events.Add($"OnSubstringDeleted({lineIndex}, {columnIndex}, {length})");
        }
    }
}
