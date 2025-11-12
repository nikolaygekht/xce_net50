using System;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Callback interface for text buffer change notifications
    /// </summary>
    public interface ITextBufferCallback
    {
        /// <summary>
        /// Called when one or more lines are inserted into the buffer
        /// </summary>
        /// <param name="lineIndex">The index where lines were inserted</param>
        /// <param name="count">The number of lines inserted</param>
        void OnLinesInserted(int lineIndex, int count);

        /// <summary>
        /// Called when one or more lines are deleted from the buffer
        /// </summary>
        /// <param name="lineIndex">The index where lines were deleted</param>
        /// <param name="count">The number of lines deleted</param>
        void OnLinesDeleted(int lineIndex, int count);

        /// <summary>
        /// Called when a substring is inserted into a line
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="columnIndex">The column index where the substring was inserted</param>
        /// <param name="length">The length of the inserted substring</param>
        void OnSubstringInserted(int lineIndex, int columnIndex, int length);

        /// <summary>
        /// Called when a substring is deleted from a line
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="columnIndex">The column index where the substring was deleted</param>
        /// <param name="length">The length of the deleted substring</param>
        void OnSubstringDeleted(int lineIndex, int columnIndex, int length);
    }
}
