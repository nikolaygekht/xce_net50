using System;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Thrown when an undo or redo action raises an exception. The buffer cannot
    /// reliably continue replaying history once an action has partially applied,
    /// so on this exception both the undo and redo stacks are cleared. The
    /// inner exception is the original failure thrown by the action.
    /// </summary>
    public class UndoCorruptedException : Exception
    {
        public UndoCorruptedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
