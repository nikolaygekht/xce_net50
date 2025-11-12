using System;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Undo action for line insertion
    /// </summary>
    internal class InsertLineUndoAction : IUndoAction
    {
        private readonly TextBuffer mBuffer;
        private readonly int mLineIndex;
        private readonly string mText;
        private readonly int mAutoAddedLines;

        public InsertLineUndoAction(TextBuffer buffer, int lineIndex, ReadOnlySpan<char> text, int autoAddedLines)
        {
            mBuffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            mLineIndex = lineIndex;
            mText = new string(text);
            mAutoAddedLines = autoAddedLines;
        }

        public void Undo()
        {
            // Undo insert = delete the inserted line
            mBuffer.DeleteLineInternal(mLineIndex, suppressUndo: true);

            // Then delete any auto-added lines that were added before the insertion point
            // Auto-added lines are at the end of the buffer before the actual insertion
            if (mAutoAddedLines > 0)
            {
                int startLine = mLineIndex - mAutoAddedLines;
                for (int i = 0; i < mAutoAddedLines; i++)
                {
                    mBuffer.DeleteLineInternal(startLine, suppressUndo: true);
                }
            }
        }

        public void Redo()
        {
            // Redo insert = insert the line again (which will auto-add lines again)
            mBuffer.InsertLineInternal(mLineIndex, mText.AsSpan(), suppressUndo: true);
        }
    }
}
