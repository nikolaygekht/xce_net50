using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Gehtsoft.Xce.Conio.Output
{
    internal class Win32ConsoleOutput : IConsoleOutput
    {
        private readonly static uint gCSBISize = (uint)Marshal.SizeOf<Win32.CONSOLE_SCREEN_BUFFER_INFO_EX>();

        public int mScreenBufferRows, mScreenBufferColumns;
        public int mWindowTop, mWindowLeft, mWindowRows, mWindowColumns;

        public int BufferRows => mScreenBufferRows;
        public int BufferColumns => mScreenBufferColumns;
        public int VisibleRows => mWindowRows;
        public int VisibleColumns => mWindowColumns;

        public bool SupportsTrueColor { get; protected set; }
        public bool SupportsReading => true;

        public virtual ConioOutputMode Mode
        { 
            get => ConioOutputMode.Win32;
        }

        public IConsoleCursor Cursor { get; }

        public Win32ConsoleOutput()
        {
            Cursor = new Win32ConsoleCursor(this);
            UpdateSize();
        }

        ~Win32ConsoleOutput()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual bool Dispose(bool disposing)
        {
            // Nothing to dispose
            return true;
        }

        public void UpdateSize()
        {
            Win32.CONSOLE_SCREEN_BUFFER_INFO_EX sbi = new Win32.CONSOLE_SCREEN_BUFFER_INFO_EX() { cbSize = gCSBISize };
            Win32.GetConsoleScreenBufferInfoEx(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), ref sbi);
            mScreenBufferRows = sbi.dwSize.Y;
            mScreenBufferColumns = sbi.dwSize.X;
            mWindowTop = sbi.srWindow.Top;
            mWindowLeft = sbi.srWindow.Left;
            mWindowRows = sbi.srWindow.Bottom - sbi.srWindow.Top + 1;
            mWindowColumns = sbi.srWindow.Right - sbi.srWindow.Left + 1;
        }

        public virtual Canvas BufferToCanvas(int row = 0, int column = 0, int rows = -1, int columns = -1)
        {
            if (rows == -1)
                rows = mScreenBufferRows - row;
            if (columns == -1)
                columns = mScreenBufferColumns - column;

            Canvas canvas = new Canvas(rows, columns);

            using (var pointer = canvas.Data.GetPointer())
            {
                Win32.COORD canvaSize = new Win32.COORD()
                {
                    X = (short)canvas.Columns,
                    Y = (short)canvas.Rows,
                };

                Win32.COORD canvaCoord = new Win32.COORD()
                {
                    X = 0,
                    Y = 0
                };

                Win32.SMALL_RECT region = new Win32.SMALL_RECT()
                {
                    Top = (short)row,
                    Bottom = (short)(row + rows - 1),
                    Left = (short)column,
                    Right = (short)(column + columns - 1),
                };
                Win32.ReadConsoleOutput(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), pointer.Pointer, canvaSize, canvaCoord, ref region);
            }
            return canvas;
        }

        public Canvas ScreenToCanvas() => BufferToCanvas(mWindowTop, mWindowLeft, mWindowRows, mWindowColumns);

        public virtual void PaintCanvasToBuffer(Canvas canvas, int bufferRow = 0, int bufferColumn = 0)
        {
            //output regular console
            using (var pointer = canvas.Data.GetPointer())
            {
                Win32.COORD canvaSize = new Win32.COORD()
                {
                    Y = (short)canvas.Rows,
                    X = (short)canvas.Columns,
                };

                Win32.COORD canvaCoord = new Win32.COORD()
                {
                    X = 0,
                    Y = 0
                };

                Win32.SMALL_RECT region = new Win32.SMALL_RECT()
                {
                    Top = (short)bufferRow,
                    Bottom = (short)(bufferRow + canvas.Rows - 1),
                    Left = (short)bufferColumn,
                    Right = (short)(bufferColumn + canvas.Columns - 1),
                };

                Win32.WriteConsoleOutput(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), pointer.Pointer, canvaSize, canvaCoord, ref region);
            }
        }

        public void PaintCanvasToScreen(Canvas canvas, int screenRow = 0, int screenColumn = 0) => PaintCanvasToBuffer(canvas, mWindowTop + screenRow, mWindowLeft + screenColumn);

        public virtual void Clear()
        {
            Console.Clear();
        }

        private Canvas mSave;
        private ConsoleColor mForegroundColor = ConsoleColor.Gray, mBackgroundColor = ConsoleColor.Black;

        public void CaptureOnStart()
        {
            mForegroundColor = Console.ForegroundColor;
            mBackgroundColor = Console.BackgroundColor;
            mSave = ScreenToCanvas();
        }

        public void ReleaseOnFinish()
        {
            Console.ForegroundColor = mForegroundColor;
            Console.BackgroundColor = mBackgroundColor;
            if (mSave != null && mSave.Rows == VisibleRows && mSave.Columns == VisibleColumns)
                PaintCanvasToScreen(mSave);
            else
                Console.Clear();
        }
    }

}
