using System.Collections;
using System.Collections.Generic;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// An interface to an undo action
    /// </summary>
    public interface IUndoAction
    {
        /// <summary>
        /// The associated buffer
        /// </summary>
        public TextBuffer Buffer { get; }

        /// <summary>
        /// Status of the buffer
        /// </summary>
        public TextBufferStatus Status { get; }

        /// <summary>
        /// Undo action in the text buffer
        /// </summary>
        /// <param name="buffer"></param>
        void Undo();

        /// <summary>
        /// Redo the action in the text buffer
        /// </summary>
        /// <param name="buffer"></param>
        void Redo();
    }
}


