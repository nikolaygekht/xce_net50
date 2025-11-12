namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Interface for undo/redo actions
    /// </summary>
    public interface IUndoAction
    {
        /// <summary>
        /// Undoes the action
        /// </summary>
        void Undo();

        /// <summary>
        /// Redoes the action
        /// </summary>
        void Redo();
    }
}
