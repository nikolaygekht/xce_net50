namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Placeholder undo action recorded for edits that touch only the implicit
    /// "infinite empty" tail of the buffer (past real content) or are otherwise
    /// empty by construction. Per design D2/D3 every edit pushes an undo entry
    /// so callers don't need to special-case "did anything change?" — Undo and
    /// Redo of this entry are intentional no-ops.
    /// </summary>
    internal sealed class NoOpUndoAction : IUndoAction
    {
        public static readonly NoOpUndoAction Instance = new NoOpUndoAction();

        private NoOpUndoAction() { }

        public void Undo() { }
        public void Redo() { }
    }
}
