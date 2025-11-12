using System;
using System.Collections.Generic;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// Undo transaction that groups multiple undo actions together
    /// </summary>
    internal class UndoTransaction : IUndoAction
    {
        private readonly List<IUndoAction> mActions = new List<IUndoAction>();

        /// <summary>
        /// Adds an action to this transaction
        /// </summary>
        public void AddAction(IUndoAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));
            mActions.Add(action);
        }

        /// <summary>
        /// Gets the number of actions in this transaction
        /// </summary>
        public int Count => mActions.Count;

        /// <summary>
        /// Undoes all actions in reverse order
        /// </summary>
        public void Undo()
        {
            // Undo in reverse order (last action first)
            for (int i = mActions.Count - 1; i >= 0; i--)
            {
                mActions[i].Undo();
            }
        }

        /// <summary>
        /// Redoes all actions in original order
        /// </summary>
        public void Redo()
        {
            // Redo in original order (first action first)
            for (int i = 0; i < mActions.Count; i++)
            {
                mActions[i].Redo();
            }
        }
    }
}
