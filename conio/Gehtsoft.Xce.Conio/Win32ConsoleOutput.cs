using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Gehtsoft.Xce.Conio
{

    class Win32ConsoleOutput : IConsoleOutput
    {
        private static uint gCSBISize = (uint)Marshal.SizeOf<Win32.CONSOLE_SCREEN_BUFFER_INFO_EX>();
        private static uint gAnnotationInfoSize = (uint)Marshal.SizeOf<Win32.AnnotationHeader>();

        public int mScreenBufferRows, mScreenBufferColumns;
        public int mWindowTop, mWindowLeft, mWindowRows, mWindowColumns;

        public int BufferRows => mScreenBufferRows;
        public int BufferColumns => mScreenBufferColumns;
        public int VisibleRows => mWindowRows;
        public int VisibleColumns => mWindowColumns;

        private bool mHasTrueColor = false;
        private string mRgbChannelName;

        public bool SupportsTrueColor => mHasTrueColor;
        public bool SupportsReading => true;

        public ConioMode Mode => ConioMode.Win32;
        public IConsoleCursor Cursor { get; }

        public Win32ConsoleOutput(bool enableTrueColor = true)
        {
            Cursor = new Win32ConsoleCursor(this);

            UpdateSize();
            if (enableTrueColor)
            {
                mRgbChannelName = string.Format("Console_annotationInfo_{0:x}_{1:x}", 32, Win32.GetConsoleWindow());
                IntPtr channel = Win32.OpenFileMapping(Win32.FileMapAccess.FileMapRead | Win32.FileMapAccess.FileMapWrite, false, mRgbChannelName);
                if (channel != IntPtr.Zero)
                {
                    mHasTrueColor = true;
                    Win32.CloseHandle(channel);
                }
            }
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

        public Canvas BufferToCanvas(int row = 0, int column = 0, int rows = -1, int columns = -1)
        {
            if (rows == -1)
                rows = mScreenBufferRows - row;
            if (columns == -1)
                columns = mScreenBufferColumns - column;

            if (mHasTrueColor && row != mWindowTop && column != mWindowLeft &&
                rows != mWindowRows && columns != mWindowColumns)
                throw new InvalidOperationException("In true color mode the canvas must exactly match the visible window size");

            Canvas canvas = new Canvas(rows, columns);

            //output true color
            if (mHasTrueColor)
            {
                IntPtr handle = Win32.OpenFileMapping(Win32.FileMapAccess.FileMapRead | Win32.FileMapAccess.FileMapWrite, false, mRgbChannelName);
                if (handle != IntPtr.Zero)
                {
                    IntPtr viewPtr = Win32.MapViewOfFile(handle, Win32.FileMapAccess.FileMapRead | Win32.FileMapAccess.FileMapWrite, 0, 0, 0);
                    try
                    {
                        Win32.AnnotationHeader header = Marshal.PtrToStructure<Win32.AnnotationHeader>(viewPtr);
                        int offset = header.StructSize;
                        int size = canvas.Rows * canvas.Columns;
                        int[] fg = canvas.ForegroundColor.Raw;
                        int[] bg = canvas.BackgroundColor.Raw;
                        int[] style = canvas.Style.Raw;
                        Win32.AnnotationInfo info;
                        for (int i = 0; i < size; i++)
                        {
                            info = MarshalEx.PtrToBitFieldStruct<Win32.AnnotationInfo>(viewPtr, offset + i * 32);

                            if (info.fg_valid != 0)
                                fg[i] = info.fg_color;
                            else
                                fg[i] = -1;

                            if (info.bk_valid != 0)
                                bg[i] = info.bk_color;
                            else
                                bg[i] = -1;

                            style[i] = info.style;
                        }
                    }
                    finally
                    {
                        Win32.UnmapViewOfFile(viewPtr);
                        Win32.CloseHandle(handle);
                    }
                }
            }


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

        public void PaintCanvasToBuffer(Canvas canvas, int bufferRow, int bufferColumn)
        {
            if (mHasTrueColor && bufferRow != mWindowTop && bufferColumn != mWindowLeft &&
                canvas.Rows != mWindowRows && canvas.Columns != mWindowColumns)
                throw new InvalidOperationException("In true color mode the canvas must exactly match the visible window size");

            //output true color
            if (mHasTrueColor)
            {
                IntPtr handle = Win32.OpenFileMapping(Win32.FileMapAccess.FileMapRead | Win32.FileMapAccess.FileMapWrite, false, mRgbChannelName);
                if (handle != IntPtr.Zero)
                {
                    IntPtr viewPtr = Win32.MapViewOfFile(handle, Win32.FileMapAccess.FileMapRead | Win32.FileMapAccess.FileMapWrite, 0, 0, 0);
                    try
                    {
                        Win32.AnnotationHeader header = Marshal.PtrToStructure<Win32.AnnotationHeader>(viewPtr);
                        int offset = header.StructSize;
                        int size = canvas.Rows * canvas.Columns;
                        int[] fg = canvas.ForegroundColor.Raw;
                        int[] bg = canvas.BackgroundColor.Raw;
                        int[] style = canvas.Style.Raw;
                        Win32.AnnotationInfo info = new Win32.AnnotationInfo();
                        for (int i = 0; i < size; i++)
                        {
                            if (fg[i] == -1)
                            {
                                info.fg_valid = 0;
                            }
                            else
                            {
                                info.fg_valid = 1;
                                info.fg_color = fg[i];
                            }
                            if (bg[i] == -1)
                            {
                                info.bk_valid = 0;
                            }
                            else
                            {
                                info.bk_valid = 1;
                                info.bk_color = bg[i];
                            }
                            if (style[i] >= 0 && style[i] < 512)
                                info.style = style[i];
                            else
                                info.style = 0;
                            MarshalEx.BitFieldStructToPtr(info, viewPtr, offset + i * 32);
                        }
                    }
                    finally
                    {
                        Win32.UnmapViewOfFile(viewPtr);
                        Win32.CloseHandle(handle);
                    }
                }
            }

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
        public void Clear()
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
            if (mSave != null && mSave.Rows == this.VisibleRows && mSave.Columns == this.VisibleColumns)
                PaintCanvasToScreen(mSave);
            else
                Console.Clear();
        }
    }
}
