using System;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Undo action for substring deletion
    /// </summary>
    internal class DeleteSubstringUndoAction : IUndoAction
    {
        private readonly TextBuffer mBuffer;
        private readonly int mLineIndex;
        private readonly int mColumnIndex;
        private readonly string mDeletedText;

        public DeleteSubstringUndoAction(TextBuffer buffer, int lineIndex, int columnIndex, ReadOnlySpan<char> deletedText)
        {
            mBuffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            mLineIndex = lineIndex;
            mColumnIndex = columnIndex;
            mDeletedText = new string(deletedText);
        }

        public void Undo()
        {
            // Undo delete = insert the substring back
            mBuffer.InsertSubstringInternal(mLineIndex, mColumnIndex, mDeletedText.AsSpan(), suppressUndo: true);
        }

        public void Redo()
        {
            // Redo delete = delete the substring again
            mBuffer.DeleteSubstringInternal(mLineIndex, mColumnIndex, mDeletedText.Length, suppressUndo: true);
        }
    }
}
