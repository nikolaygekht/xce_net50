using Gehtsoft.Xce.Conio.Drawing;
using System;

namespace Gehtsoft.Xce.Conio.Output
{
    internal abstract class ConsoleBasedOutput : IConsoleOutput
    {
        protected readonly bool mWindows;

        public int BufferRows => Console.BufferHeight;

        public int BufferColumns => Console.BufferWidth;

        public int VisibleRows => Console.WindowHeight;

        public int VisibleColumns => Console.WindowWidth;

        internal int WindowLeft => Console.WindowLeft;

        internal int WindowTop => Console.WindowTop;

        public bool SupportsTrueColor => false;

        public bool SupportsReading => false;

        public ConioOutputMode Mode => ConioOutputMode.NetConsole;

        public IConsoleCursor Cursor { get; }

        public ConsoleBasedOutput()
        {
            Cursor = new ConsoleCursor(this);
            var os = Environment.OSVersion;
            mWindows = os.Platform == PlatformID.Win32NT;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Nothing to do here
        }

        public virtual Canvas BufferToCanvas(int row = 0, int column = 0, int rows = -1, int columns = -1)
        {
            throw new NotImplementedException();
        }

        public virtual Canvas ScreenToCanvas()
        {
            throw new NotImplementedException();
        }

        public virtual void PaintCanvasToScreen(Canvas canvas, int screenRow = 0, int screenColumn = 0) => PaintCanvasToBuffer(canvas, Console.WindowTop + screenRow, Console.WindowLeft + screenColumn);

        public abstract void PaintCanvasToBuffer(Canvas canvas, int bufferRow = 0, int bufferColumn = 0);

        public virtual void UpdateSize()
        {
            //nothing to do for ordinary console
        }

        public virtual void Clear() => Console.Clear();

        private ConsoleColor mDefaultFg = ConsoleColor.Gray, mDefaultBg = ConsoleColor.Black;

        public virtual void CaptureOnStart()
        {
            mDefaultFg = Console.ForegroundColor;
            mDefaultBg = Console.BackgroundColor;
        }

        public virtual void ReleaseOnFinish()
        {
            Console.ForegroundColor = mDefaultFg;
            Console.BackgroundColor = mDefaultBg;
            Console.Clear();
        }
    }
}
