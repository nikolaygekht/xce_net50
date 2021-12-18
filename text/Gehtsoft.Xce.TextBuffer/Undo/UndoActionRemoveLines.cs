namespace Gehtsoft.Xce.TextBuffer.Undo
{
    public class UndoActionRemoveLines : UndoAction
    {
        private readonly char[][] mLines;
        private readonly int mPosition, mCount;

        public UndoActionRemoveLines(int position, int count, TextBuffer buffer) : base(buffer)
        {
            mLines = new char[count][];
            for (int i = 0; i < count; i++)
                buffer.GetLine(position + i, out mLines[i]);
            mPosition = position;
            mCount = count;
        }

        public override void Undo()
        {
            for (int i = 0; i < mCount; i++)
                Buffer.InsertLine(mPosition + i, mLines[i], true);
            Buffer.Status = Status;
        }

        public override void Redo()
        {
            Buffer.Status = Status;
            Buffer.RemoveLines(mPosition, mCount, true);
        }
    }
}


