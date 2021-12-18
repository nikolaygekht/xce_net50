namespace Gehtsoft.Xce.TextBuffer.Undo
{
    public class UndoActionInsertLine : UndoAction
    {
        private readonly char[] mLine;
        private readonly int mPosition;

        public UndoActionInsertLine(int position, char[] line, TextBuffer buffer) : base(buffer)
        {
            mLine = line;
            mPosition = position;
        }

        public override void Undo()
        {
            Buffer.RemoveLine(mPosition, true);
            Buffer.Status = Status;
        }

        public override void Redo()
        {
            Buffer.Status = Status;
            Buffer.InsertLine(mPosition, mLine, true);
        }
    }
}


