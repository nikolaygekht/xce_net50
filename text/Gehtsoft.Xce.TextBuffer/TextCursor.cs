using System;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Caret + selection anchor for a TextBuffer. The cursor is owned by the buffer
    /// and is automatically adjusted by edits and round-tripped through undo/redo.
    ///
    /// Adjustment rules on edits:
    /// - line insert/delete: caret and anchor shift like markers (lines before -> shift, lines on deleted -> snap to (deletedFirst, 0))
    /// - substring insert at column &lt;= caret.column on caret.line: caret shifts right by inserted length
    ///   (insert at the caret pushes the caret right - typical typing behaviour)
    /// - substring delete overlapping caret: caret clamps to the deletion start
    /// </summary>
    public class TextCursor
    {
        private int mLine;
        private int mColumn;
        private int mAnchorLine;
        private int mAnchorColumn;

        public int Line => mLine;
        public int Column => mColumn;
        public int AnchorLine => mAnchorLine;
        public int AnchorColumn => mAnchorColumn;

        /// <summary>True if caret and anchor differ.</summary>
        public bool HasSelection => mLine != mAnchorLine || mColumn != mAnchorColumn;

        internal TextCursor() { }

        /// <summary>Move caret to (line, col) and collapse the selection to it.</summary>
        public void MoveTo(int line, int column)
        {
            if (line < 0) throw new ArgumentOutOfRangeException(nameof(line));
            if (column < 0) throw new ArgumentOutOfRangeException(nameof(column));
            mLine = line;
            mColumn = column;
            mAnchorLine = line;
            mAnchorColumn = column;
        }

        /// <summary>Set caret and anchor explicitly.</summary>
        public void SetSelection(int line, int column, int anchorLine, int anchorColumn)
        {
            if (line < 0) throw new ArgumentOutOfRangeException(nameof(line));
            if (column < 0) throw new ArgumentOutOfRangeException(nameof(column));
            if (anchorLine < 0) throw new ArgumentOutOfRangeException(nameof(anchorLine));
            if (anchorColumn < 0) throw new ArgumentOutOfRangeException(nameof(anchorColumn));
            mLine = line;
            mColumn = column;
            mAnchorLine = anchorLine;
            mAnchorColumn = anchorColumn;
        }

        /// <summary>Collapse selection: anchor = caret.</summary>
        public void Collapse()
        {
            mAnchorLine = mLine;
            mAnchorColumn = mColumn;
        }

        // --- internal adjustment hooks driven by TextBuffer ---

        internal void OnLinesInserted(int lineIndex, int count)
        {
            if (mLine >= lineIndex) mLine += count;
            if (mAnchorLine >= lineIndex) mAnchorLine += count;
        }

        internal void OnLinesDeleted(int lineIndex, int count)
        {
            int deletedFirst = lineIndex;
            int deletedLast = lineIndex + count - 1;
            AdjustLineOnDelete(ref mLine, ref mColumn, deletedFirst, deletedLast, count);
            AdjustLineOnDelete(ref mAnchorLine, ref mAnchorColumn, deletedFirst, deletedLast, count);
        }

        internal void OnSubstringInserted(int lineIndex, int columnIndex, int length)
        {
            if (lineIndex == mLine && columnIndex <= mColumn) mColumn += length;
            if (lineIndex == mAnchorLine && columnIndex <= mAnchorColumn) mAnchorColumn += length;
        }

        internal void OnSubstringDeleted(int lineIndex, int columnIndex, int length)
        {
            int deletedLast = columnIndex + length;
            AdjustColOnDelete(ref mColumn, mLine, lineIndex, columnIndex, deletedLast, length);
            AdjustColOnDelete(ref mAnchorColumn, mAnchorLine, lineIndex, columnIndex, deletedLast, length);
        }

        // --- snapshot support (used by BufferStateSnapshot) ---

        internal (int line, int column, int anchorLine, int anchorColumn) Snapshot()
            => (mLine, mColumn, mAnchorLine, mAnchorColumn);

        internal void RestoreFromSnapshot(int line, int column, int anchorLine, int anchorColumn)
        {
            mLine = line;
            mColumn = column;
            mAnchorLine = anchorLine;
            mAnchorColumn = anchorColumn;
        }

        // --- helpers ---

        private static void AdjustLineOnDelete(ref int line, ref int col, int deletedFirst, int deletedLast, int count)
        {
            if (line < deletedFirst) return;
            if (line > deletedLast) { line -= count; return; }
            // on a deleted line: snap to deletion start, column 0
            line = deletedFirst;
            col = 0;
        }

        private static void AdjustColOnDelete(ref int col, int posLine, int eventLine, int deletedFirst, int deletedLast, int length)
        {
            if (posLine != eventLine) return;
            if (deletedLast <= col) { col -= length; return; }
            if (deletedFirst < col) col -= Math.Min(col - deletedFirst, length);
        }
    }
}
