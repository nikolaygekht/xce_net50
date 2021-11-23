using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;


namespace Gehtsoft.Xce.Conio
{

    internal class CharInfoArray 
    {
        private int mRows, mColumns, mSize;
        private static int gCharInfoSize;
        private Win32.CHAR_INFO[] mMemory;

        public int Count => mSize;

        public int Rows => mRows;

        public int Columns => mColumns;

        internal Win32.CHAR_INFO[] Raw => mMemory;

        public ref Win32.CHAR_INFO this[int index]
        {
            get
            {
                if (index < 0 || index >= mSize)
                    throw new IndexOutOfRangeException();
                return ref mMemory[index];
            }
        }

        public ref Win32.CHAR_INFO this[int row, int column]
        {
            get
            {
                if (row < 0 || row >= mRows)
                    throw new ArgumentOutOfRangeException(nameof(row));
                if (column < 0 || column >= mColumns)
                    throw new ArgumentOutOfRangeException(nameof(column));
                return ref this[row * mColumns + column];
            }
            
        }

        static CharInfoArray()
        {
            gCharInfoSize = Marshal.SizeOf<Win32.CHAR_INFO>();
        }

        public CharInfoArray(int rows, int columns)
        {
            mRows = rows;
            mColumns = columns;
            mSize = mRows * mColumns;
            mMemory = new Win32.CHAR_INFO[mSize * gCharInfoSize];
        }

        public TemporaryPointer GetPointer()
        {
            return new TemporaryPointer(mMemory);
        }

    }

}
