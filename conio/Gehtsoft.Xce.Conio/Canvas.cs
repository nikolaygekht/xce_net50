using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace Gehtsoft.Xce.Conio
{
    public class Canvas
    {
        internal CharInfoArray Data { get; }
        internal IntArray ForegroundColor { get; }
        internal IntArray BackgroundColor { get; }
        internal IntArray Style { get; }

        public int Rows { get; }
        public int Columns { get; }

        public Canvas(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            Data = new CharInfoArray(rows, columns);
            ForegroundColor = new IntArray(rows, columns);
            BackgroundColor = new IntArray(rows, columns);
            Style = new IntArray(rows, columns);
        }

        public void Write(int row, int column, char chr)
        {
            if (row < 0 || column < 0 || row >= Rows || column >= Columns)
                return;
            Data[row, column].UnicodeChar = chr;
        }

        private void WriteRgb(int row, int column, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= Rows || column >= Columns)
                return;

            int idx = row * Columns + column;
            int fg = color.RgbFg;
            int bg = color.RgbBg;
            int s = (int)color.Style;
            if (!CanvasColor.IsValid(fg))
                fg = -1;
            if (!CanvasColor.IsValid(bg))
                bg = -1;
            ForegroundColor[idx] = fg;
            BackgroundColor[idx] = bg;
            Style[idx] = s;
        }

        public void Write(int row, int column, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= Rows || column >= Columns)
                return;

            Data[row, column].Attributes = color.PalColor;
            WriteRgb(row, column, color);
        }
        public void Write(int row, int column, CanvasCell cell) => Write(row, column, cell.Character, cell.Color);

        public void WriteForeground(int row, int column, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= Rows || column >= Columns)
                return;

            ref Win32.CHAR_INFO c = ref Data[row, column];
            c.Attributes = (ushort)((c.Attributes & 0xf0) | (color.PalColor & 0x0f));

            if (CanvasColor.IsValid(color.RgbFg))
                ForegroundColor[row, column] = color.RgbFg;
            else
                ForegroundColor[row, column] = -1;
        }

        public void WriteBackground(int row, int column, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= Rows || column >= Columns)
                return;

            ref Win32.CHAR_INFO c = ref Data[row, column];
            c.Attributes = (ushort)((c.Attributes & 0x0f) | (color.PalColor & 0xf0));

            if (CanvasColor.IsValid(color.RgbBg))
                BackgroundColor[row, column] = color.RgbBg;
            else
                BackgroundColor[row, column] = -1;
        }

        public void Write(int row, int column, char chr, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= Rows || column >= Columns)
                return;

            ref Win32.CHAR_INFO c = ref Data[row, column];
            c.Attributes = color.PalColor;
            c.UnicodeChar = chr;
            WriteRgb(row, column, color);
        }

        private void Write(int row, int column, int offset, int length, CanvasColor color, Func<int> lengthAction, Func<int, char> charAction)
        {
            if (row >= 0 && row < Rows)
            {
                if (column < 0)
                {
                    length += column;
                    offset -= column;
                    column = 0;
                }

                if (offset >= lengthAction())
                    return;

                if (length > lengthAction() - offset)
                    length = lengthAction() - offset;

                if (length <= 0)
                    return;

                int limit = column + length;
                if (limit > Columns)
                    limit = Columns;

                int tbase = offset;
                for (int i = column; i < limit; i++, tbase++)
                {
                    if (color == null)
                        Write(row, i, charAction(tbase));
                    else
                        Write(row, i, charAction(tbase), color);
                }
            }
        }

        public void Write(int row, int column, string text, int offset, int length) => Write(row, column, offset, length, null, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, string text, int offset, int length, CanvasColor c) => Write(row, column, offset, length, c, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, char[] text, int offset, int length) => Write(row, column, offset, length, null, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, char[] text, int offset, int length, CanvasColor c) => Write(row, column, offset, length, c, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, StringBuilder text, int offset, int length) => Write(row, column, offset, length, null, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, StringBuilder text, int offset, int length, CanvasColor c) => Write(row, column, offset, length, c, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, string text) => Write(row, column, 0, text.Length, null, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, string text, CanvasColor c) => Write(row, column, 0, text.Length, c, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, char[] text) => Write(row, column, 0, text.Length, null, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, char[] text, CanvasColor c) => Write(row, column, 0, text.Length, c, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, StringBuilder text) => Write(row, column, 0, text.Length, null, () => text.Length, (i) => text[i]);
        public void Write(int row, int column, StringBuilder text, CanvasColor c) => Write(row, column, 0, text.Length, c, () => text.Length, (i) => text[i]);

        private void Fill(int row, int column, int rows, int columns, Action<int, int> action)
        {
            if (row < 0)
            {
                rows += row;
                row = 0;
            }

            if (column < 0)
            {
                columns += column;
                column = 0;
            }

            if (row >= Rows)
                return;
            if (rows <= 0 || columns <= 0)
                return;

            int rowLimit = row + rows;
            if (rowLimit > Rows)
                rowLimit = Rows;

            int colLimit = column + columns;
            if (colLimit > Columns)
                colLimit = Columns;
            int i, j;
            for (i = row; i < rowLimit; i++)
            {
                for (j = column; j < colLimit; j++)
                    action(i, j);
            }
        }

        public void Fill(int row, int column, int rows, int columns, char chr) => Fill(row, column, rows, columns, (r, c) => Write(r, c, chr));
        public void Fill(int row, int column, int rows, int columns, CanvasColor color) => Fill(row, column, rows, columns, (r, c) => Write(r, c, color));
        public void Fill(int row, int column, int rows, int columns, char chr, CanvasColor color) => Fill(row, column, rows, columns, (r, c) => Write(r, c, chr, color));
        public void Fill(int row, int column, int rows, int columns, CanvasCell cell) => Fill(row, column, rows, columns, (r, c) => Write(r, c, cell));
        public void FillFg(int row, int column, int rows, int columns, CanvasColor cell) => Fill(row, column, rows, columns, (r, c) => WriteForeground(r, c, cell));
        public void FillBg(int row, int column, int rows, int columns, CanvasColor cell) => Fill(row, column, rows, columns, (r, c) => WriteBackground(r, c, cell));

        public void Box(int row, int column, int rows, int columns, BoxBorder border, CanvasColor color)
        {
            if (rows >= 2 && columns >= 2)
            {
                //borders
                Fill(row, column + 1, 1, columns - 2, border.Top, color);
                Fill(row + rows - 1, column + 1, 1, columns - 2, border.Bottom, color);
                Fill(row + 1, column, rows - 2, 1, border.Left, color);
                Fill(row + 1, column + columns - 1, rows - 2, 1, border.Right, color);
                //edges
                Write(row, column, border.TopLeft, color);
                Write(row, column + columns - 1, border.TopRight, color);
                Write(row + rows - 1, column, border.BottomLeft, color);
                Write(row + rows - 1, column + columns - 1, border.BottomRight, color);
            }
        }

        public void Box(int row, int column, int rows, int columns, BoxBorder border, CanvasColor color, char fillchar)
        {
            Box(row, column, rows, columns, border, color);
            if (rows > 2 && columns > 2)
                Fill(row + 1, column + 1, rows - 2, columns - 2, fillchar, color);
        }

        public void Paint(int dstRow, int dstColumn, Canvas srcCanvas)
        {
            int srcRows = srcCanvas.Rows;
            int srcColumns = srcCanvas.Columns;
            int srcColumnsOrg = srcColumns;
            int srcRow = 0;
            int srcColumn = 0;

            if (dstRow < 0)
            {
                srcRows += dstRow;
                srcRow += -dstRow;
                dstRow = 0;
            }

            if (dstColumn < 0)
            {
                srcColumns += dstColumn;
                srcColumn += -dstColumn;
                dstColumn = 0;
            }

            if (dstRow >= Rows)
                return;

            if (srcColumns > Columns - dstColumn)
                srcColumns = Columns - dstColumn;

            if (srcRows > Rows - dstRow)
                srcRows = Rows - dstRow;

            if (srcRows <= 0 || srcColumns <= 0)
                return;

            int i, j;

            for (i = dstRow, j = 0; j < srcRows; i++, j++)
            {
                int dstOffset = i * Columns + dstColumn;
                int srcOffset = (j + srcRow) * srcColumnsOrg + srcColumn;

                Array.Copy(srcCanvas.Data.Raw, srcOffset, Data.Raw, dstOffset, srcColumns);
                Array.Copy(srcCanvas.ForegroundColor.Raw, srcOffset, ForegroundColor.Raw, dstOffset, srcColumns);
                Array.Copy(srcCanvas.BackgroundColor.Raw, srcOffset, BackgroundColor.Raw, dstOffset, srcColumns);
                Array.Copy(srcCanvas.Style.Raw, srcOffset, Style.Raw, dstOffset, srcColumns);
            }
        }

        public CanvasCell Get(int row, int column)
        {
            ref Win32.CHAR_INFO ci = ref Data[row, column];
            int fg = ForegroundColor[row, column];
            int bg = BackgroundColor[row, column];
            int s = Style[row, column];
            return new CanvasCell((char)ci.UnicodeChar, new CanvasColor(ci.Attributes, fg, bg, (CanvasColor.ConsoleStyle)s));
        }
    }
}
