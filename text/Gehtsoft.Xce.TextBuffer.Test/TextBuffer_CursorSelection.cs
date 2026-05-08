using AwesomeAssertions;
using Xunit;

namespace Gehtsoft.Xce.TextBuffer.Test
{
    /// <summary>
    /// Cursor anchor + selection behaviour under edits and undo/redo. The buffer
    /// adjusts the caret like a stream block, but the anchor must follow the same
    /// rules independently — including for "reversed" selections where the user
    /// selected backwards and the anchor sits AFTER the caret.
    /// </summary>
    public class TextBuffer_CursorSelection
    {
        // ----------------------------------------------------------------------
        // Reversed selection (anchor positioned after caret) - common when the
        // user drags a selection to the left or shift-arrows backwards.
        // ----------------------------------------------------------------------

        [Fact]
        public void ReversedSelection_LineInsertBeforeBoth_ShiftsBothDown()
        {
            // User selected from line 3 (anchor) backwards to line 1 (caret).
            // Inserting a line at the top must shift both endpoints by 1.
            var buffer = new TextBuffer(new[] { "L0", "L1", "L2", "L3", "L4" });
            buffer.Cursor.SetSelection(line: 1, column: 0, anchorLine: 3, anchorColumn: 2);

            buffer.InsertLine(0, "new");

            buffer.Cursor.Line.Should().Be(2);
            buffer.Cursor.Column.Should().Be(0);
            buffer.Cursor.AnchorLine.Should().Be(4);
            buffer.Cursor.AnchorColumn.Should().Be(2);
            buffer.Cursor.HasSelection.Should().BeTrue();
        }

        [Fact]
        public void ReversedSelection_SubstringInsertBeforeCaret_ShiftsCaretAndAnchorIfOnSameLine()
        {
            // Reversed single-line selection: caret at col 2, anchor at col 7.
            // Insert "XX" before col 2: both endpoints shift right by 2 (anchor too,
            // because columnIndex (2) <= anchorColumn (7) AND same line).
            var buffer = new TextBuffer(new[] { "abcdefghij" });
            buffer.Cursor.SetSelection(line: 0, column: 2, anchorLine: 0, anchorColumn: 7);

            buffer.InsertSubstring(0, 2, "XX");

            buffer.Cursor.Column.Should().Be(4);
            buffer.Cursor.AnchorColumn.Should().Be(9);
        }

        [Fact]
        public void ReversedSelection_DeleteSpanningCaret_ClampsCaretButNotAnchor()
        {
            // Caret at col 2, anchor at col 8. Delete cols 1..4 (length 3, spans caret).
            // Caret clamps to deletion start (1); anchor (8) shifts left by 3 → 5.
            var buffer = new TextBuffer(new[] { "abcdefghij" });
            buffer.Cursor.SetSelection(line: 0, column: 2, anchorLine: 0, anchorColumn: 8);

            buffer.DeleteSubstring(0, 1, 3);

            buffer.Cursor.Column.Should().Be(1);
            buffer.Cursor.AnchorColumn.Should().Be(5);
        }

        [Fact]
        public void ReversedSelection_RoundTripsThroughUndoAndRedo_Exactly()
        {
            // The point of state snapshots: a non-collapsed reversed selection
            // must come back exactly after Undo, and again exactly after Redo.
            var buffer = new TextBuffer(new[] { "alpha", "beta", "gamma" });
            buffer.Cursor.SetSelection(line: 0, column: 4, anchorLine: 2, anchorColumn: 1);

            buffer.InsertLine(1, "new");

            buffer.Undo();
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(4);
            buffer.Cursor.AnchorLine.Should().Be(2);
            buffer.Cursor.AnchorColumn.Should().Be(1);

            buffer.Redo();
            // Forward state: caret stayed (line 0 < insertion at 1), anchor shifted.
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(4);
            buffer.Cursor.AnchorLine.Should().Be(3);
            buffer.Cursor.AnchorColumn.Should().Be(1);
        }

        // ----------------------------------------------------------------------
        // Anchor on a deleted line - mirrors the existing caret rule.
        // ----------------------------------------------------------------------

        [Fact]
        public void AnchorOnDeletedLine_SnapsToDeletionPoint_AndRestoresExactlyOnUndo()
        {
            // Caret on line 0, anchor on line 2. Delete line 2: anchor must snap
            // to (deletionStart, 0) just like the caret would. Undo restores
            // exact column.
            var buffer = new TextBuffer(new[] { "alpha", "beta", "gamma", "delta" });
            buffer.Cursor.SetSelection(line: 0, column: 1, anchorLine: 2, anchorColumn: 4);

            buffer.DeleteLine(2);

            // Forward: anchor snapped, caret unchanged.
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(1);
            buffer.Cursor.AnchorLine.Should().Be(2);
            buffer.Cursor.AnchorColumn.Should().Be(0);

            buffer.Undo();
            // Restored exactly.
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(1);
            buffer.Cursor.AnchorLine.Should().Be(2);
            buffer.Cursor.AnchorColumn.Should().Be(4);
        }

        // ----------------------------------------------------------------------
        // Forward selection round-trip through transactions.
        // ----------------------------------------------------------------------

        [Fact]
        public void ForwardSelection_RoundTripsThroughTransactionUndoAndRedo_Exactly()
        {
            var buffer = new TextBuffer(new[] { "one", "two", "three", "four" });
            buffer.Cursor.SetSelection(line: 1, column: 0, anchorLine: 2, anchorColumn: 5);

            using (buffer.BeginUndoTransaction())
            {
                buffer.InsertLine(0, "header");
                buffer.InsertSubstring(2, 0, "// ");
                // The format command moves the caret to (0, 0) at the end.
                buffer.Cursor.MoveTo(0, 0);
            }

            buffer.Undo();
            buffer.Cursor.Line.Should().Be(1);
            buffer.Cursor.Column.Should().Be(0);
            buffer.Cursor.AnchorLine.Should().Be(2);
            buffer.Cursor.AnchorColumn.Should().Be(5);
            buffer.Cursor.HasSelection.Should().BeTrue();

            buffer.Redo();
            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(0);
            buffer.Cursor.AnchorLine.Should().Be(0);
            buffer.Cursor.AnchorColumn.Should().Be(0);
            buffer.Cursor.HasSelection.Should().BeFalse();
        }

        // ----------------------------------------------------------------------
        // Collapse() and HasSelection invariants.
        // ----------------------------------------------------------------------

        [Fact]
        public void Collapse_AnchorMovesToCaret_HasSelectionBecomesFalse()
        {
            var buffer = new TextBuffer(new[] { "abc", "def" });
            buffer.Cursor.SetSelection(line: 0, column: 1, anchorLine: 1, anchorColumn: 2);
            buffer.Cursor.HasSelection.Should().BeTrue();

            buffer.Cursor.Collapse();

            buffer.Cursor.Line.Should().Be(0);
            buffer.Cursor.Column.Should().Be(1);
            buffer.Cursor.AnchorLine.Should().Be(0);
            buffer.Cursor.AnchorColumn.Should().Be(1);
            buffer.Cursor.HasSelection.Should().BeFalse();
        }

        [Fact]
        public void HasSelection_TrueWhenAnyEndpointDiffers_FalseOnlyWhenBothMatch()
        {
            var buffer = new TextBuffer(new[] { "abc", "def" });

            // Same line, different column → has selection
            buffer.Cursor.SetSelection(0, 1, 0, 2);
            buffer.Cursor.HasSelection.Should().BeTrue();

            // Different line, same column → has selection
            buffer.Cursor.SetSelection(0, 1, 1, 1);
            buffer.Cursor.HasSelection.Should().BeTrue();

            // Both equal → no selection
            buffer.Cursor.SetSelection(0, 1, 0, 1);
            buffer.Cursor.HasSelection.Should().BeFalse();

            // MoveTo collapses selection
            buffer.Cursor.SetSelection(0, 1, 1, 1);
            buffer.Cursor.MoveTo(0, 0);
            buffer.Cursor.HasSelection.Should().BeFalse();
        }

        // ----------------------------------------------------------------------
        // Selection that *spans* an inserted region grows; selection *adjacent*
        // to the insertion does not. (Mirrors stream-block edge semantics.)
        // ----------------------------------------------------------------------

        [Fact]
        public void Selection_InsertStrictlyInside_BothEndpointsCompose_SelectionGrows()
        {
            // Forward selection from col 2 to col 7 on a single line. Insert
            // "XX" inside (col 4): caret at 2 unchanged (insert AFTER caret has
            // no effect on caret per OnSubstringInserted's `<=` rule); anchor
            // at 7 shifts to 9. Net: selection grew by 2.
            var buffer = new TextBuffer(new[] { "abcdefghij" });
            buffer.Cursor.SetSelection(line: 0, column: 2, anchorLine: 0, anchorColumn: 7);

            buffer.InsertSubstring(0, 4, "XX");

            buffer.Cursor.Column.Should().Be(2);
            buffer.Cursor.AnchorColumn.Should().Be(9);
        }

        [Fact]
        public void Selection_InsertAtCaretBoundary_BoundaryShiftsRight_SelectionStaysSameSize()
        {
            // Forward selection from col 2 to col 7. Insert "XX" exactly at col 2
            // (the caret's position). Per the cursor's OnSubstringInserted rule,
            // `columnIndex (2) <= mColumn (2)` is true, so the caret shifts right
            // by 2. Anchor at col 7 also shifts right (2 <= 7). Both endpoints
            // moved → selection size unchanged but content is now what was
            // selected before, just at higher indices.
            var buffer = new TextBuffer(new[] { "abcdefghij" });
            buffer.Cursor.SetSelection(line: 0, column: 2, anchorLine: 0, anchorColumn: 7);

            buffer.InsertSubstring(0, 2, "XX");

            buffer.Cursor.Column.Should().Be(4);
            buffer.Cursor.AnchorColumn.Should().Be(9);
        }

        [Fact]
        public void Selection_InsertAtAnchorBoundary_OnlyAnchorShifts_SelectionGrows()
        {
            // Forward selection from col 2 to col 7. Insert "XX" exactly at col 7
            // (the anchor's position). Per the rule, `columnIndex (7) <= mColumn (2)`
            // is false → caret unchanged. `columnIndex (7) <= mAnchorColumn (7)`
            // is true → anchor shifts to 9. Selection grew to absorb the insert.
            var buffer = new TextBuffer(new[] { "abcdefghij" });
            buffer.Cursor.SetSelection(line: 0, column: 2, anchorLine: 0, anchorColumn: 7);

            buffer.InsertSubstring(0, 7, "XX");

            buffer.Cursor.Column.Should().Be(2);
            buffer.Cursor.AnchorColumn.Should().Be(9);
        }
    }
}
