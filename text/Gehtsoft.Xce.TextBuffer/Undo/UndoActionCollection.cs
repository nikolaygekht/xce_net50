using System;
using System.Collections;
using System.Collections.Generic;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    /// <summary>
    /// The collection of undo actions
    /// </summary>
    public class UndoActionCollection : IReadOnlyCollection<IUndoAction>
    {
        private readonly LinkedList<IUndoAction> mActions = new LinkedList<IUndoAction>();
        private readonly TextBuffer mBuffer;

        public UndoActionCollection(TextBuffer buffer)
        {
            mBuffer = buffer;
        }

        /// <summary>
        /// Gets number of actions in the collection
        /// </summary>
        public int Count => mActions.Count;

        /// <summary>
        /// Checks whether the collection list is empty.
        /// </summary>
        public bool IsEmpty => mActions.Count == 0;

        public IDisposable BeginTransaction()
        {
            var transaction = new UndoTransaction(mBuffer);
            var closer = transaction.GetCloser();
            Push(transaction);
            return closer;
        }

        /// <summary>
        /// Adds a new action
        /// </summary>
        /// <param name="action"></param>
        public void Push(IUndoAction action)
        {
            if (mActions.Last?.Value is UndoTransaction transaction && !transaction.IsClosed)
                transaction.Push(action);
            else
                mActions.AddLast(action);
        }

        public bool IsInTransaction => Peek() is UndoTransaction transaction && !transaction.IsClosed;

        /// <summary>
        /// Peeks the action at the bottom of the collection
        /// </summary>
        /// <returns></returns>
        public IUndoAction Peek() => mActions.Last?.Value;

        /// <summary>
        /// Pops the action from the bottom
        /// </summary>
        /// <returns></returns>
        public IUndoAction Pop()
        {
            var r = mActions.Last?.Value;
            if (mActions.Last != null)
                mActions.RemoveLast();
            return r;
        }

        /// <summary>
        /// Returns the enumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IUndoAction> GetEnumerator()
        {
            return ((IEnumerable<IUndoAction>)mActions).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)mActions).GetEnumerator();
        }

        public void ForEachForward(Action<IUndoAction> action)
        {
            var n = mActions.First;
            while (n != null)
            {
                action(n.Value);
                n = n.Next;
            }
        }

        public void ForEachBackward(Action<IUndoAction> action)
        {
            var n = mActions.Last;
            while (n != null)
            {
                action(n.Value);
                n = n.Previous;
            }
        }
    }
}


