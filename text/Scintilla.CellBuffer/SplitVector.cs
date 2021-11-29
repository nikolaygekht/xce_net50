using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Scintilla.CellBuffer
{
    /// <summary>
    /// Split list class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SplitList<T> : IReadOnlyList<T>
    {
        private sealed class SplitListEnumerator : IEnumerator<T>
        {
            private readonly SplitList<T> mList;
            private int mPosition;

            public SplitListEnumerator(SplitList<T> list)
            {
                mList = list;
                mPosition = -1;
            }

            public T Current => mList[mPosition];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                //nothing to dispose
            }

            public bool MoveNext()
            {
                mPosition++;
                return mPosition <= mList.Count;
            }

            public void Reset()
            {
                mPosition = -1;
            }
        }

        private readonly SimpleList<T> mContent = new SimpleList<T>();
        private const int DefaultGapSize = 16;

        /// <summary>
        /// The number of elements in the vector
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => mContent.Count - GapLength;
        }

        public T this[int index]
        {
            get
            {
                if (index < Part1Length)
                    return mContent[index];
                else
                    return mContent[index + GapLength];
            }
            set
            {
                if (index < Part1Length)
                    mContent[index] = value;
                else
                    mContent[index + GapLength] = value;
            }
        }

        internal int Part1Length { get; private set; }

        internal int GapLength { get; private set; }

        /// <summary>
        /// The size of the second part of the split list
        /// </summary>
        internal int Part2Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Count - Part1Length;
        }

        /// <summary>
        /// The constructor
        /// </summary>
        public SplitList()
        {
            mContent.AddEmptyElements(DefaultGapSize);
            Part1Length = 0;
            GapLength = DefaultGapSize;
        }

        /// <summary>
        /// Moves cap to the specified position
        /// </summary>
        /// <param name="position"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GapTo(int position)
        {
            if (position == Part1Length)
                return;

            mContent.EnsureCapacity(Count + DefaultGapSize);
            //close gap
            mContent.Move(Part1Length, Part1Length + GapLength, Part2Length);
            //create gap at new location
            mContent.Move(position, position + DefaultGapSize, Part2Length);
            Part1Length = position;
            GapLength = DefaultGapSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RoomFor(int count)
        {
            if (count <= 0)
                return;

            if (GapLength < count)
            {
                int required = count - GapLength;
                mContent.EnsureCapacity(Count + count);
                mContent.Move(Part1Length + GapLength, Part1Length + GapLength + required, Part2Length);
                GapLength += count;
            }
        }

        public void InsertAt(int index, T value)
        {
            GapTo(index);
            RoomFor(1);
            mContent[Part1Length] = value;
            Part1Length++;
            GapLength--;
        }

        public void InsertAt(int index, ICollection<T> values)
        {
            GapTo(index);
            RoomFor(values.Count);
            foreach (var value in values)
                mContent[Part1Length++] = value;
            GapLength -= values.Count;
        }

        public void RemoveAt(int index, int count = 1)
        {
            GapTo(index);
            if (count > Part2Length)
                throw new ArgumentException("Too much elements to delete", nameof(count));
            GapLength += count;
        }

        public void ToArray(int sourceIndex, int length, T[] target, int targetIndex)
        {
            if (sourceIndex + length <= Part1Length)
                mContent.ToArray(sourceIndex, length, target, targetIndex);
            else if (sourceIndex > Part1Length)
                mContent.ToArray(sourceIndex + GapLength, length, target, targetIndex);
            else
            {
                var inPart1 = Part1Length - sourceIndex;
                ToArray(sourceIndex, inPart1, target, targetIndex);
                ToArray(sourceIndex + inPart1, length - inPart1, target, targetIndex + inPart1);
            }
        }

        public IEnumerator<T> GetEnumerator() => new SplitListEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
