namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Snapshot of the cursor / primary block / marker positions at a moment in time.
    /// Captured at the boundary of a user-visible edit (top-level Insert/Delete or
    /// outermost transaction) so undo/redo can restore observable editor state exactly.
    /// </summary>
    internal sealed class BufferStateSnapshot
    {
        // Cursor
        private readonly int mCaretLine;
        private readonly int mCaretCol;
        private readonly int mAnchorLine;
        private readonly int mAnchorCol;

        // Block
        private readonly TextBufferBlockType mBlockType;
        private readonly int mBlockFirstLine;
        private readonly int mBlockLastLine;
        private readonly int mBlockFirstCol;
        private readonly int mBlockLastCol;

        // Markers
        private readonly TextMarkerCollection.MarkerSnapshotEntry[] mMarkers;

        private BufferStateSnapshot(
            int caretLine, int caretCol, int anchorLine, int anchorCol,
            TextBufferBlockType blockType, int blockFirstLine, int blockLastLine, int blockFirstCol, int blockLastCol,
            TextMarkerCollection.MarkerSnapshotEntry[] markers)
        {
            mCaretLine = caretLine;
            mCaretCol = caretCol;
            mAnchorLine = anchorLine;
            mAnchorCol = anchorCol;
            mBlockType = blockType;
            mBlockFirstLine = blockFirstLine;
            mBlockLastLine = blockLastLine;
            mBlockFirstCol = blockFirstCol;
            mBlockLastCol = blockLastCol;
            mMarkers = markers;
        }

        public static BufferStateSnapshot Capture(TextCursor cursor, TextBufferBlock block, TextMarkerCollection markers)
        {
            var c = cursor.Snapshot();
            var b = block.Snapshot();
            return new BufferStateSnapshot(
                c.line, c.column, c.anchorLine, c.anchorColumn,
                b.type, b.firstLine, b.lastLine, b.firstCol, b.lastCol,
                markers.Snapshot());
        }

        public void Restore(TextCursor cursor, TextBufferBlock block, TextMarkerCollection markers)
        {
            cursor.RestoreFromSnapshot(mCaretLine, mCaretCol, mAnchorLine, mAnchorCol);
            block.RestoreFromSnapshot(mBlockType, mBlockFirstLine, mBlockLastLine, mBlockFirstCol, mBlockLastCol);
            markers.RestoreFromSnapshot(mMarkers);
        }
    }
}
