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
        private static int gCharInfoSize;

        private int mRows, mColumns;
        private CharInfoArray mData;
        private IntArray mForegroundColor, mBackgroundColor, mStyle;
        
        internal CharInfoArray Data => mData;
        internal IntArray ForegroundColor => mForegroundColor;
        internal IntArray BackgroundColor => mBackgroundColor; 
        internal IntArray Style => mStyle;

        public int Rows => mRows;
        public int Columns => mColumns;

        static Canvas()
        {
            gCharInfoSize = Marshal.SizeOf<Win32.CHAR_INFO>();
        }

        public Canvas(int rows, int columns)
        {
            mRows = rows;
            mColumns = columns;
            mData = new CharInfoArray(rows, columns);
            mForegroundColor = new IntArray(rows, columns);
            mBackgroundColor = new IntArray(rows, columns);
            mStyle = new IntArray(rows, columns);
        }


        public void Write(int row, int column, char chr)
        {
            if (row < 0 || column < 0 || row >= mRows || column >= mColumns)
                return;
            mData[row, column].UnicodeChar = chr;
        }

        private void WriteRgb(int row, int column, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= mRows || column >= mColumns)
                return;

            int idx = row * mColumns + column;
            int fg = color.RgbFg;
            int bg = color.RgbBg;
            int s = (int)color.Style;
            if (!CanvasColor.IsValid(fg))
                fg = -1;
            if (!CanvasColor.IsValid(bg))
                bg = -1;
            mForegroundColor[idx] = fg;
            mBackgroundColor[idx] = bg;
            mStyle[idx] = s;
        }

        public void Write(int row, int column, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= mRows || column >= mColumns)
                return;

            mData[row, column].Attributes = color.PalColor;
            WriteRgb(row, column, color);
        }
        public void Write(int row, int column, CanvasCell cell) => Write(row, column, cell.Character, cell.Color);

        public void WriteForeground(int row, int column, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= mRows || column >= mColumns)
                return;

            ref Win32.CHAR_INFO c = ref mData[row, column];
            c.Attributes =(ushort)((c.Attributes & 0xf0) | (color.PalColor & 0x0f));

            if (CanvasColor.IsValid(color.RgbFg))
                mForegroundColor[row, column] = color.RgbFg;
            else
                mForegroundColor[row, column] = -1;
        }

        public void WriteBackground(int row, int column, CanvasColor color)
        {
            if (row < 0 || column < 0 || row >= mRows || column >= mColumns)
                return;

            ref Win32.CHAR_INFO c = ref mData[row, column];
            c.Attributes = (ushort)((c.Attributes & 0x0f) | (color.PalColor & 0xf0));

            if (CanvasColor.IsValid(color.RgbBg))
                mBackgroundColor[row, column] = color.RgbBg;
            else
                mBackgroundColor[row, column] = -1;
        }

        public void Write(int row, int column, char chr, CanvasColor color) 
        {
            if (row < 0 || column < 0 || row >= mRows || column >= mColumns)
                return;

            ref Win32.CHAR_INFO c = ref mData[row, column];
            c.Attributes = color.PalColor;
            c.UnicodeChar = chr;
            WriteRgb(row, column, color);
        }


        private void Write(int row, int column, int offset, int length, CanvasColor color, Func<int> lengthAction, Func<int, char> charAction)
        {
            if (row >= 0 && row < mRows)
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
                if (limit > mColumns)
                    limit = mColumns;

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

            if (row >= mRows)
                return;
            if (rows <= 0 || columns <= 0)
                return;

            int rowLimit = row + rows;
            if (rowLimit > mRows)
                rowLimit = mRows;

            int colLimit = column + columns;
            if (colLimit > mColumns)
                colLimit = mColumns;
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
        public void fillFg(int row, int column, int rows, int columns, CanvasColor cell) => Fill(row, column, rows, columns, (r, c) => WriteForeground(r, c, cell));
        public void fillBg(int row, int column, int rows, int columns, CanvasColor cell) => Fill(row, column, rows, columns, (r, c) => WriteBackground(r, c, cell));

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

            if (dstRow >= mRows)
                return;

            if (srcColumns > mColumns - dstColumn)
                srcColumns = mColumns - dstColumn;

            if (srcRows > mRows - dstRow)
                srcRows = mRows - dstRow;

            if (srcRows <= 0 || srcColumns <= 0)
                return;

            int i, j;

            for (i = dstRow, j = 0; j < srcRows; i++, j++)
            {
                int dstOffset = i * mColumns + dstColumn;
                int srcOffset = (j + srcRow) * srcColumnsOrg + srcColumn;

                Array.Copy(srcCanvas.Data.Raw, srcOffset, mData.Raw, dstOffset, srcColumns);
                Array.Copy(srcCanvas.ForegroundColor.Raw, srcOffset, mForegroundColor.Raw, dstOffset, srcColumns);
                Array.Copy(srcCanvas.BackgroundColor.Raw, srcOffset, mBackgroundColor.Raw, dstOffset, srcColumns);
                Array.Copy(srcCanvas.Style.Raw, srcOffset, mStyle.Raw, dstOffset, srcColumns);
            }
        }

        public CanvasCell Get(int row, int column)
        {
            ref Win32.CHAR_INFO ci = ref mData[row, column];
            int fg = mForegroundColor[row, column];
            int bg = mBackgroundColor[row, column];
            int s = mStyle[row, column];
            return new CanvasCell((char)ci.UnicodeChar, new CanvasColor(ci.Attributes, fg, bg, (CanvasColor.ConsoleStyle)s));
        }
    }

    
}
