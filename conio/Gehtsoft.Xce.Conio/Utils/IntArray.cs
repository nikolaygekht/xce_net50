using System;
using System.Runtime.InteropServices;

namespace Gehtsoft.Xce.Conio
{
    internal class IntArray 
    {
        private int mRows, mColumns, mSize;
        private int[] mMemory;

        public int Count => mSize;

        public int Rows => mRows;

        public int Columns => mColumns;

        internal int[] Raw => mMemory;


        public int this[int index]
        {
            get
            {
                if (index < 0 || index >= mSize)
                    throw new IndexOutOfRangeException();
                return mMemory[index];
            }
            set
            {
                if (index < 0 || index >= mSize)
                    throw new IndexOutOfRangeException();
                mMemory[index] = value;
            }
        }

        public int this[int row, int column]
        {
            get => this[row * mColumns + column];
            set => this[row * mColumns + column] = value;
        }

        public IntArray(int rows, int columns)
        {
            mRows = rows;
            mColumns = columns;
            mSize = mRows * mColumns;
            mMemory = new int[mSize];
            for (int i = 0; i < mSize; i++)
                this[i] = -1;
        }

        public TemporaryPointer GetPointer()
        {
            return new TemporaryPointer(mMemory);
        }
    }

}
