namespace Gehtsoft.Xce.Conio
{
    public interface IConsoleOutput
   {
        ConioMode Mode { get; }
        int BufferRows { get; }
        int BufferColumns { get; }
        int VisibleRows { get; }
        int VisibleColumns { get; }
        bool SupportsTrueColor { get; }
        bool SupportsReading { get; }
        IConsoleCursor Cursor { get; }
        void UpdateSize();

        Canvas BufferToCanvas(int row = 0, int column = 0, int rows = -1, int columns = -1);
        Canvas ScreenToCanvas();
        void PaintCanvasToBuffer(Canvas canvas, int bufferRow = 0, int bufferColumn = 0);
        void PaintCanvasToScreen(Canvas canvas, int screenRow = 0, int screenColumn = 0);
        void Clear();
        void CaptureOnStart();
        void ReleaseOnFinish();
    }
}
