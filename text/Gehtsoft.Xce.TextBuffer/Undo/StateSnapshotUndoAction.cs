using System;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Wraps a data-changing IUndoAction with cursor/block/marker state snapshots
    /// taken before and after the action. On Undo/Redo, the inner action runs first
    /// (firing all callbacks and letting state self-adjust noisily); the snapshot is
    /// then restored on top, overwriting any drift back to the exact captured state.
    ///
    /// The "inner first, then restore" order is important: callbacks fired during
    /// inner.Undo()/Redo() will mutate cursor/block/markers based on the events they
    /// see. Restoring the snapshot afterwards is the only way to guarantee the
    /// captured state survives.
    /// </summary>
    internal sealed class StateSnapshotUndoAction : IUndoAction
    {
        private readonly TextBuffer mBuffer;
        private readonly IUndoAction mInner;
        private readonly BufferStateSnapshot mBefore;
        private readonly BufferStateSnapshot mAfter;

        public StateSnapshotUndoAction(TextBuffer buffer, IUndoAction inner, BufferStateSnapshot before, BufferStateSnapshot after)
        {
            mBuffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            mInner = inner ?? throw new ArgumentNullException(nameof(inner));
            mBefore = before ?? throw new ArgumentNullException(nameof(before));
            mAfter = after ?? throw new ArgumentNullException(nameof(after));
        }

        public void Undo()
        {
            mInner.Undo();
            mBuffer.RestoreStateFromSnapshot(mBefore);
        }

        public void Redo()
        {
            mInner.Redo();
            mBuffer.RestoreStateFromSnapshot(mAfter);
        }
    }
}
