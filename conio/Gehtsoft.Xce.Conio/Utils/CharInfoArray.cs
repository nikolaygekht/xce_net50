using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Gehtsoft.Xce.Conio
{
    internal class CharInfoArray
    {
        private readonly static int gCharInfoSize = Marshal.SizeOf<Win32.CHAR_INFO>();

        public int Count { get; }

        public int Rows { get; }

        public int Columns { get; }

        internal Win32.CHAR_INFO[] Raw { get; }

        public ref Win32.CHAR_INFO this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return ref Raw[index];
            }
        }

        public ref Win32.CHAR_INFO this[int row, int column]
        {
            get
            {
                if (row < 0 || row >= Rows)
                    throw new ArgumentOutOfRangeException(nameof(row));
                if (column < 0 || column >= Columns)
                    throw new ArgumentOutOfRangeException(nameof(column));
                return ref this[row * Columns + column];
            }
        }

        public CharInfoArray(int rows, int columns)
        {
            Rows = rows;
            Columns = columns;
            Count = Rows * Columns;
            Raw = new Win32.CHAR_INFO[Count * gCharInfoSize];
        }

        public TemporaryPointer GetPointer()
        {
            return new TemporaryPointer(Raw);
        }
    }
}
