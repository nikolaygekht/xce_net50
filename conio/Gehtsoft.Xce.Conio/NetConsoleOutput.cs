using System;
using System.Text;

namespace Gehtsoft.Xce.Conio
{
    class NetConsoleOutput : IConsoleOutput
    {
        private bool mWindows; 
        public int BufferRows => Console.BufferHeight;

        public int BufferColumns => Console.BufferWidth;

        public int VisibleRows => Console.WindowHeight;

        public int VisibleColumns => Console.WindowWidth;

        internal int WindowLeft => Console.WindowLeft;

        internal int WindowTop => Console.WindowTop;

        public bool SupportsTrueColor => false;

        public bool SupportsReading => false;

        public ConioMode Mode => ConioMode.CompatibleConsole;

        public IConsoleCursor Cursor { get; } 

        public NetConsoleOutput()
        {
            Cursor = new NetConsoleCursor(this);
            var os = Environment.OSVersion;
            mWindows = os.Platform == PlatformID.Win32NT;

        }

        public Canvas BufferToCanvas(int row = 0, int column = 0, int rows = -1, int columns = -1)
        {
            throw new NotImplementedException();
        }

        public Canvas ScreenToCanvas()
        {
            throw new NotImplementedException();
        }

        public void PaintCanvasToScreen(Canvas canvas, int screenRow, int screenColumn) => PaintCanvasToBuffer(canvas, Console.WindowTop + screenRow, Console.WindowLeft + screenColumn);


        public void PaintCanvasToBuffer(Canvas canvas, int bufferRow, int bufferColumn)
        {
            var v = Cursor.CursorVisible;
            var top = Console.WindowTop;
            Cursor.CursorVisible = false;
            for (int r = 0; r < canvas.Rows; r++)
            {
                int s = -1;
                ushort attr = 0xffff;
                StringBuilder b = null;
                for (int c = 0; c < canvas.Columns; c++)
                {
                    var cell = canvas.Data[r, c];
                    if (c == 0 || attr != cell.Attributes)
                    {
                        if (b != null && s >= 0)
                        {
                            Console.SetCursorPosition(s + Console.WindowLeft, r + Console.WindowTop);
                            Console.ForegroundColor = (ConsoleColor)(attr & 0xf);
                            Console.BackgroundColor = (ConsoleColor)((attr >> 4) & 0xf);
                            Console.Write(b.ToString());
                        }
                        s = c;
                        attr = cell.Attributes;
                        b = new StringBuilder();
                        b.Append((char)cell.UnicodeChar);
                    }
                    else
                        b.Append((char)cell.UnicodeChar);
                }
                if (b.Length > 0)
                {
                    Console.SetCursorPosition(s + Console.WindowLeft, r + Console.WindowTop);
                    Console.ForegroundColor = (ConsoleColor)(attr & 0xf);
                    Console.BackgroundColor = (ConsoleColor)((attr >> 4) & 0xf);
                    Console.Write(b.ToString());
                }
            }
            if (mWindows)
                Console.WindowTop = top;
            else
                Console.SetCursorPosition(0, top);
            Cursor.CursorVisible = v;
        }

        public void UpdateSize()
        {
        }

        public void Clear() => Console.Clear();

        private ConsoleColor mDefaultFg = ConsoleColor.Gray, mDefaultBg = ConsoleColor.Black;

        public void CaptureOnStart()
        {
            mDefaultFg = Console.ForegroundColor;
            mDefaultBg = Console.BackgroundColor;

        }

        public void ReleaseOnFinish()
        {
            Console.ForegroundColor = mDefaultFg;
            Console.BackgroundColor = mDefaultBg;
            Console.Clear();
        }
    }
}
