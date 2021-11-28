using System;
using System.Runtime.InteropServices;

namespace Gehtsoft.Xce.Conio
{
    internal class IntArray
    {
        public int Count { get; }

        public int Rows { get; }

        public int Columns { get; }

        internal int[] Raw { get; }

        public int this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return Raw[index];
            }
            set
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                Raw[index] = value;
            }
        }

        public int this[int row, int column]
        {
            get => this[row * Columns + column];
            set => this[row * Columns + column] = value;
        }

        public IntArray(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            Count = Rows * Columns;
            Raw = new int[Count];
            for (int i = 0; i < Count; i++)
                this[i] = -1;
        }

        public TemporaryPointer GetPointer()
        {
            return new TemporaryPointer(Raw);
        }
    }
}
