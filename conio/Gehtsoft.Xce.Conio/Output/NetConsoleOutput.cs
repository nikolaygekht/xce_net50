using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gehtsoft.Xce.Conio.Output
{
    internal sealed class NetConsoleOutput : ConsoleBasedOutput
    {
        public NetConsoleOutput() : base()
        {
        }

        private sealed class BufferWriter
        {
            private ushort mCurrentAttribute;
            private readonly StringBuilder mCurrentString = new StringBuilder();
            private int mCurrentRow, mCurrentStringStart;
            private readonly int mScreenBufferRow, mScreenBufferColumn;

            public BufferWriter(int row, int column)
            {
                mScreenBufferRow = row;
                mScreenBufferColumn = column;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void NewCanvasRow(int row)
            {
                mCurrentRow = row;
                mCurrentStringStart = -1;
                mCurrentString.Clear();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void NewCanvasCell(int column, Win32.CHAR_INFO cell)
            {
                if (column == 0 || mCurrentAttribute != cell.Attributes)
                {
                    if (mCurrentString.Length > 0 && mCurrentStringStart >= 0)
                    {
                        Console.SetCursorPosition(mCurrentStringStart + mScreenBufferColumn, mCurrentRow + mScreenBufferRow);
                        Console.ForegroundColor = (ConsoleColor)(mCurrentAttribute & 0xf);
                        Console.BackgroundColor = (ConsoleColor)(mCurrentAttribute >> 4 & 0xf);
                        Console.Write(mCurrentString.ToString());
                    }
                    mCurrentStringStart = column;
                    mCurrentAttribute = cell.Attributes;

                    if (mCurrentString.Length > 0)
                        mCurrentString.Clear();

                    mCurrentString.Append((char)cell.UnicodeChar);
                }
                else
                {
                    mCurrentString.Append((char)cell.UnicodeChar);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnRowEnd()
            {
                if (mCurrentString.Length > 0)
                {
                    Console.SetCursorPosition(mCurrentStringStart + mScreenBufferColumn, mCurrentRow + mScreenBufferRow);
                    Console.ForegroundColor = (ConsoleColor)(mCurrentAttribute & 0xf);
                    Console.BackgroundColor = (ConsoleColor)(mCurrentAttribute >> 4 & 0xf);
                    Console.Write(mCurrentString.ToString());
                }
            }
        }

        public override void PaintCanvasToBuffer(Canvas canvas, int bufferRow = 0, int bufferColumn = 0)
        {
            var v = Cursor.CursorVisible;
            var top = Console.WindowTop;
            Cursor.CursorVisible = false;

            BufferWriter writer = new BufferWriter(bufferRow, bufferColumn);

            for (int r = 0; r < canvas.Rows; r++)
            {
                writer.NewCanvasRow(r);
                for (int c = 0; c < canvas.Columns; c++)
                {
                    var cell = canvas.Data[r, c];
                    writer.NewCanvasCell(c, cell);
                }
                writer.OnRowEnd();
            }

            if (mWindows)
                Console.WindowTop = top;
            else
                Console.SetCursorPosition(0, top);

            Cursor.CursorVisible = v;
        }
    }
}
