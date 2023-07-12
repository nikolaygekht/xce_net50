using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gehtsoft.Xce.Conio.Output
{
    internal class VtConsoleOutput : ConsoleBasedOutput
    {
        protected Canvas mSavedCanvas;

        public VtConsoleOutput() : base()
        {
        }

        private sealed class BufferWriter
        {
            private readonly StringBuilder mCurrentString = new StringBuilder();
            private int mCurrentColumn;
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
                mCurrentColumn = -1;
                mCurrentString.Clear();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void NewCanvasCell(int column, string content)
            {
                if (column != mCurrentColumn)
                {
                    if (mCurrentString.Length > 0 && mCurrentStringStart >= 0)
                    {
                        Console.SetCursorPosition(mCurrentStringStart + mScreenBufferColumn, mCurrentRow + mScreenBufferRow);
                        Console.Write(mCurrentString.ToString());
                    }
                    mCurrentStringStart = column;
                    mCurrentColumn = column + 1;

                    if (mCurrentString.Length > 0)
                        mCurrentString.Clear();

                    mCurrentString.Append(content);
                }
                else
                {
                    mCurrentColumn++;
                    mCurrentString.Append(content);
                    
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void OnRowEnd()
            {
                if (mCurrentString.Length > 0)
                {
                    Console.SetCursorPosition(mCurrentStringStart + mScreenBufferColumn, mCurrentRow + mScreenBufferRow);
                    Console.Write(mCurrentString.ToString());
                }
            }
        }

        public override void PaintCanvasToBuffer(Canvas canvas, int bufferRow = 0, int bufferColumn = 0)
        {
            if (mSavedCanvas == null || mSavedCanvas.Rows != VisibleRows || mSavedCanvas.Columns != VisibleColumns)
                mSavedCanvas = new Canvas(VisibleRows, VisibleColumns);

            var cursorVisible = Cursor.CursorVisible;
            Cursor.GetCursorPosition(out int cursorRow, out int cursorColumn);
            Cursor.CursorVisible = false;

            var writer = new BufferWriter(bufferRow, bufferColumn);

            for (int i = 0; i < canvas.Rows && i < VisibleRows; i++)
            {
                writer.NewCanvasRow(i);
                for (int j = 0; j < canvas.Columns && j < VisibleColumns; j++)
                {
                    var newCell = canvas.Get(i, j);
                    var oldCell = mSavedCanvas.Get(i, j);
                    
                    if (newCell.Equals(oldCell))
                        continue;

                    var color = ColorToEscapeCode(newCell.Color);
                    color.Append(newCell.Character);

                    writer.NewCanvasCell(j, color.ToString());
                    Cursor.SetCursorPosition(i - (bufferRow - Console.WindowTop), j - (bufferColumn - Console.WindowLeft));
                    mSavedCanvas.Write(i, j, newCell);
                }
                writer.OnRowEnd();
            }

            Cursor.SetCursorPosition(cursorRow, cursorColumn);
            Cursor.CursorVisible = cursorVisible;
        }

        protected virtual StringBuilder ColorToEscapeCode(CanvasColor color)
        {
            StringBuilder code = new StringBuilder();

            ushort paletteColor = color.PalColor;
            ushort fb = (ushort)(paletteColor & 0xf);
            ushort bg = (ushort)((paletteColor & 0xf0) >> 4);

            switch (fb)
            {
                case 0x0:
                    code.Append("\x1b[30m");
                    break;
                case 0x1:
                    code.Append("\x1b[34m");
                    break;
                case 0x2:
                    code.Append("\x1b[32m");
                    break;
                case 0x3:
                    code.Append("\x1b[36m");
                    break;
                case 0x4:
                    code.Append("\x1b[31m");
                    break;
                case 0x5:
                    code.Append("\x1b[35m");
                    break;
                case 0x6:
                    code.Append("\x1b[33m");
                    break;
                case 0x7:
                    code.Append("\x1b[37m");
                    break;
                case 0x8:
                    code.Append("\x1b[90m");
                    break;
                case 0x9:
                    code.Append("\x1b[94m");
                    break;
                case 0xa:
                    code.Append("\x1b[92m");
                    break;
                case 0xb:
                    code.Append("\x1b[96m");
                    break;
                case 0xc:
                    code.Append("\x1b[91m");
                    break;
                case 0xd:
                    code.Append("\x1b[95m");
                    break;
                case 0xe:
                    code.Append("\x1b[93m");
                    break;
                case 0xf:
                    code.Append("\x1b[97m");
                    break;
            }
            switch (bg)
            {
                case 0x0:
                    code.Append("\x1b[40m");
                    break;
                case 0x1:
                    code.Append("\x1b[44m");
                    break;
                case 0x2:
                    code.Append("\x1b[42m");
                    break;
                case 0x3:
                    code.Append("\x1b[46m");
                    break;
                case 0x4:
                    code.Append("\x1b[41m");
                    break;
                case 0x5:
                    code.Append("\x1b[45m");
                    break;
                case 0x6:
                    code.Append("\x1b[43m");
                    break;
                case 0x7:
                    code.Append("\x1b[47m");
                    break;
                case 0x8:
                    code.Append("\x1b[100m");
                    break;
                case 0x9:
                    code.Append("\x1b[104m");
                    break;
                case 0xa:
                    code.Append("\x1b[102m");
                    break;
                case 0xb:
                    code.Append("\x1b[106m");
                    break;
                case 0xc:
                    code.Append("\x1b[101m");
                    break;
                case 0xd:
                    code.Append("\x1b[105m");
                    break;
                case 0xe:
                    code.Append("\x1b[103m");
                    break;
                case 0xf:
                    code.Append("\x1b[107m");
                    break;
            }

            return code;
        }
    }
}
