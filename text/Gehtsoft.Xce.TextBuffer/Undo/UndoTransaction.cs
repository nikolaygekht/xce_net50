using System;

namespace Gehtsoft.Xce.TextBuffer.Undo
{
    public class UndoTransaction : UndoAction
    {
        private readonly UndoActionCollection mActions;
        private bool mIsClosed;
        public bool IsClosed => mIsClosed;

        private sealed class UndoTransactionCloser : IDisposable
        {
            private readonly UndoTransaction mTransaction;

            public UndoTransactionCloser(UndoTransaction transaction)
            {
                mTransaction = transaction;
            }

            public void Dispose()
            {
                mTransaction.mIsClosed = true;
            }
        }

        public void Push(IUndoAction action)
        {
            mActions.Push(action);
        }

        internal int Count => mActions.Count;

        internal IUndoAction Peek() => mActions.Peek();

        public IDisposable GetCloser() => new UndoTransactionCloser(this);

        public UndoTransaction(TextBuffer buffer) : base(buffer)
        {
            mActions = new UndoActionCollection(buffer);
        }

        public override void Redo()
        {
            mActions.ForEachForward(a => a.Redo());
        }

        public override void Undo()
        {
            mActions.ForEachBackward(a => a.Undo());
        }
    }
}


