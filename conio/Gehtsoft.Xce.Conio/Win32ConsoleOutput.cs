using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Gehtsoft.Xce.Conio
{
    internal sealed class Win32ConsoleOutput : IConsoleOutput
    {
        private readonly static uint gCSBISize = (uint)Marshal.SizeOf<Win32.CONSOLE_SCREEN_BUFFER_INFO_EX>();
        private IntPtr mViewHandle, mMapPtr;

        public int mScreenBufferRows, mScreenBufferColumns;
        public int mWindowTop, mWindowLeft, mWindowRows, mWindowColumns;

        public int BufferRows => mScreenBufferRows;
        public int BufferColumns => mScreenBufferColumns;
        public int VisibleRows => mWindowRows;
        public int VisibleColumns => mWindowColumns;

        public bool SupportsTrueColor { get; }
        public bool SupportsReading => true;

        public ConioMode Mode => ConioMode.Win32;
        public IConsoleCursor Cursor { get; }

        public Win32ConsoleOutput(bool enableTrueColor = true)
        {
            Cursor = new Win32ConsoleCursor(this);

            UpdateSize();
            if (enableTrueColor)
            {
                SupportsTrueColor = false;
                mViewHandle = mMapPtr = IntPtr.Zero;
                var rgbChannelName = string.Format("Console_annotationInfo_{0:x}_{1:x}", 32, Win32.GetConsoleWindow());
                mViewHandle = Win32.OpenFileMapping(Win32.FileMapAccess.FileMapAllAccess, false, rgbChannelName);
                if (mViewHandle != IntPtr.Zero)
                {
                    mMapPtr = Win32.MapViewOfFile(mViewHandle, Win32.FileMapAccess.FileMapAllAccess, 0, 0, 0);
                    if (mMapPtr != IntPtr.Zero)
                        SupportsTrueColor = true;
                    else
                    {
                        Win32.CloseHandle(mViewHandle);
                        mViewHandle = IntPtr.Zero;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (mViewHandle != IntPtr.Zero)
            {
                Win32.UnmapViewOfFile(mMapPtr);
                Win32.CloseHandle(mViewHandle);
                mMapPtr = mViewHandle = IntPtr.Zero;
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

            if (SupportsTrueColor && row != mWindowTop && column != mWindowLeft &&
                rows != mWindowRows && columns != mWindowColumns)
                throw new InvalidOperationException("In true color mode the canvas must exactly match the visible window size");

            Canvas canvas = new Canvas(rows, columns);

            //output true color
            if (SupportsTrueColor)
            {
                Win32.AnnotationHeader header = Marshal.PtrToStructure<Win32.AnnotationHeader>(mMapPtr);
                header.Locked = 1;
                Marshal.StructureToPtr(header, mMapPtr, true);

                int offset = header.StructSize;
                int size = canvas.Rows * canvas.Columns;
                int[] fg = canvas.ForegroundColor.Raw;
                int[] bg = canvas.BackgroundColor.Raw;
                int[] style = canvas.Style.Raw;

                Win32.AnnotationInfo info;
                for (int i = 0; i < size; i++)
                {
                    info = MarshalEx.PtrToBitFieldStruct<Win32.AnnotationInfo>(mMapPtr, offset + i * 32);

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

                header.Locked = 0;
                Marshal.StructureToPtr(header, mMapPtr, true);
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

        private readonly static Win32.AnnotationHeader ZEROHEADER = new Win32.AnnotationHeader();

        public void PaintCanvasToBuffer(Canvas canvas, int bufferRow = 0, int bufferColumn = 0)
        {
            if (SupportsTrueColor && bufferRow != mWindowTop && bufferColumn != mWindowLeft &&
                canvas.Rows != mWindowRows && canvas.Columns != mWindowColumns)
                throw new InvalidOperationException("In true color mode the canvas must exactly match the visible window size");

#pragma warning disable IDE0059 // Unnecessary assignment of a value: false positive
            Win32.AnnotationHeader header = ZEROHEADER;
#pragma warning restore IDE0059 
            try
            {
                if (SupportsTrueColor)
                {
                    header = Marshal.PtrToStructure<Win32.AnnotationHeader>(mMapPtr);
                    header.Locked = 1;
                    Marshal.StructureToPtr(header, mMapPtr, true);

                    int offset = header.StructSize;
                    int size = canvas.Rows * canvas.Columns;

                    int[] fg = canvas.ForegroundColor.Raw;
                    int[] bg = canvas.BackgroundColor.Raw;
                    int[] style = canvas.Style.Raw;
                    Win32.AnnotationInfo info = new Win32.AnnotationInfo();
                    for (int i = 0; i < size; i++)
                    {
                        int canvasRow = i / canvas.Columns;
                        int canvasColumn = i - canvasRow * canvas.Columns;
                        if (fg[i] != -1 && bg[i] != -1)
                        {
                            info.fg_valid = 1;
                            info.fg_color = fg[i];
                            info.bk_valid = 1;
                            info.bk_color = bg[i];
                        }
                        else
                        {
                            info.fg_valid = 0;
                            info.fg_color = 0;
                            info.bk_valid = 0;
                            info.bk_color = 0;
                        }

                        if (style[i] >= 0 && style[i] < 512)
                            info.style = style[i];
                        else
                            info.style = 0;

                        int cellOffset = (bufferRow + canvasRow) * mScreenBufferColumns + bufferColumn + canvasColumn;
                        int valueOffset = offset + cellOffset * 32;
                        MarshalEx.BitFieldStructToPtr(info, mMapPtr, valueOffset);
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
            finally
            {
                if (SupportsTrueColor)
                {
                    header = Marshal.PtrToStructure<Win32.AnnotationHeader>(mMapPtr);
                    header.FlushCounter++;
                    Marshal.StructureToPtr(header, mMapPtr, true);
                    header.Locked = 0;
                    Marshal.StructureToPtr(header, mMapPtr, true);
                }
            }
        }

        public void PaintCanvasToScreen(Canvas canvas, int screenRow = 0, int screenColumn = 0) => PaintCanvasToBuffer(canvas, mWindowTop + screenRow, mWindowLeft + screenColumn);
        public void Clear()
        {
            Console.Clear();

            if (SupportsTrueColor)
            {
                var header = Marshal.PtrToStructure<Win32.AnnotationHeader>(mMapPtr);
                header.Locked = 1;
                int offset = header.StructSize;
                int size = header.BufferSize / 32;
                Win32.AnnotationInfo info = new Win32.AnnotationInfo();
                for (int i = 0; i < size; i++)
                    MarshalEx.BitFieldStructToPtr(info, mMapPtr, offset + i * 32);

                header.FlushCounter++;
                Marshal.StructureToPtr(header, mMapPtr, true);
                header.Locked = 0;
                Marshal.StructureToPtr(header, mMapPtr, true);
            }
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
