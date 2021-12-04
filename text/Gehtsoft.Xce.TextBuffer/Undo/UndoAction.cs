namespace Gehtsoft.Xce.TextBuffer.Undo
{
    public abstract class UndoAction : IUndoAction
    {
        /// <summary>
        /// The associated buffer
        /// </summary>
        public TextBuffer Buffer { get; }

        /// <summary>
        /// Status of the buffer
        /// </summary>
        public TextBufferStatus Status { get; }

        protected UndoAction(TextBuffer buffer)
        {
            Buffer = buffer;
            Status = buffer.Status.Clone();
        }

        /// <summary>
        /// Undo action in the text buffer
        /// </summary>
        /// <param name="buffer"></param>
        public abstract void Undo();

        /// <summary>
        /// Redo the action in the text buffer
        /// </summary>
        /// <param name="buffer"></param>
        public abstract void Redo();
    }
}


