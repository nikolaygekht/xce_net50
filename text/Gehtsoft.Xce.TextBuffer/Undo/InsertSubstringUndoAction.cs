using System;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Undo action for substring insertion
    /// </summary>
    internal class InsertSubstringUndoAction : IUndoAction
    {
        private readonly TextBuffer mBuffer;
        private readonly int mLineIndex;
        private readonly int mColumnIndex;
        private readonly string mText;
        private readonly int mAutoAddedLines;
        private readonly int mAutoAddedSpaces;

        public InsertSubstringUndoAction(TextBuffer buffer, int lineIndex, int columnIndex, ReadOnlySpan<char> text, int autoAddedLines, int autoAddedSpaces)
        {
            mBuffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            mLineIndex = lineIndex;
            mColumnIndex = columnIndex;
            mText = new string(text);
            mAutoAddedLines = autoAddedLines;
            mAutoAddedSpaces = autoAddedSpaces;
        }

        public void Undo()
        {
            // Undo insert = delete the substring and any auto-added content
            // First delete the inserted text
            mBuffer.DeleteSubstringInternal(mLineIndex, mColumnIndex, mText.Length, suppressUndo: true);

            // Then delete any auto-added spaces
            if (mAutoAddedSpaces > 0)
            {
                int spacesStartColumn = mColumnIndex - mAutoAddedSpaces;
                mBuffer.DeleteSubstringInternal(mLineIndex, spacesStartColumn, mAutoAddedSpaces, suppressUndo: true);
            }

            // Finally delete any auto-added lines
            if (mAutoAddedLines > 0)
            {
                int startLine = mBuffer.LinesCount - mAutoAddedLines;
                for (int i = 0; i < mAutoAddedLines; i++)
                {
                    mBuffer.DeleteLineInternal(startLine, suppressUndo: true);
                }
            }
        }

        public void Redo()
        {
            // Redo insert = insert the substring again (which will auto-add lines/spaces again)
            mBuffer.InsertSubstringInternal(mLineIndex, mColumnIndex, mText.AsSpan(), suppressUndo: true);
        }
    }
}
