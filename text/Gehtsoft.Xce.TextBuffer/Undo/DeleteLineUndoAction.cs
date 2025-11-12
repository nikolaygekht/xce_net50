using System;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Undo action for line deletion
    /// </summary>
    internal class DeleteLineUndoAction : IUndoAction
    {
        private readonly TextBuffer mBuffer;
        private readonly int mLineIndex;
        private readonly string mDeletedText;

        public DeleteLineUndoAction(TextBuffer buffer, int lineIndex, ReadOnlySpan<char> deletedText)
        {
            mBuffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            mLineIndex = lineIndex;
            mDeletedText = new string(deletedText);
        }

        public void Undo()
        {
            // Undo delete = insert the line back
            mBuffer.InsertLineInternal(mLineIndex, mDeletedText.AsSpan(), suppressUndo: true);
        }

        public void Redo()
        {
            // Redo delete = delete the line again
            mBuffer.DeleteLineInternal(mLineIndex, suppressUndo: true);
        }
    }
}
