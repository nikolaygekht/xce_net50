using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Scintilla.CellBuffer
{
    /// <summary>
    /// Simple list class.
    ///
    /// A simple vector is an analogues of a list, but with ability to move the blocks of content inside the vector fast
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SimpleList<T> : IReadOnlyList<T>
    {
        private T[] mContent;
        private int mCapacity;
        private int mLength;

        private sealed class SimpleListEnumerator : IEnumerator<T>
        {
            private readonly SimpleList<T> mList;
            private int mPosition;

            public SimpleListEnumerator(SimpleList<T> list)
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
                return mPosition < mList.Count;
            }

            public void Reset()
            {
                mPosition = -1;
            }
        }

        /// <summary>
        /// Returns the number of items
        /// </summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => mLength;
        }

        /// <summary>
        /// The capacity of the vector
        /// </summary>
        public int Capacity
        {
            get => mCapacity;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private set
            {
                if (mCapacity < mLength)
                    throw new ArgumentException("Capacity should not be less than the size", nameof(value));
                T[] newarray = new T[value];
                if (mContent != null)
                    Array.Copy(mContent, newarray, mLength);
                mContent = newarray;
                mCapacity = value;
            }
        }

        int IReadOnlyCollection<T>.Count => throw new NotImplementedException();

        T IReadOnlyList<T>.this[int index] => throw new NotImplementedException();

        /// <summary>
        /// Gets or sets an item
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= mLength)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return mContent[index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (index < 0 || index >= mLength)
                    throw new ArgumentOutOfRangeException(nameof(index));
                mContent[index] = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SimpleList()
        {
            Capacity = 32;
            mLength = 0;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public SimpleList(T[] initialContent)
        {
            EnsureCapacity(initialContent.Length);
            Array.Copy(initialContent, mContent, initialContent.Length);
            mLength = initialContent.Length;
        }

        /// <summary>
        /// Ensures that the array is capable to keep the specified number of items
        /// </summary>
        /// <param name="size"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int size)
        {
            if (Capacity < size)
            {
                if (size <= 1024)
                    Capacity = size + (32 - size % 32);
                else
                    Capacity = size * 2;
            }
        }

        /// <summary>
        /// Adds an element to the content
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(T value)
        {
            EnsureCapacity(mLength + 1);
            mContent[mLength] = value;
            mLength++;
        }

        /// <summary>
        /// Inserts an element to the content at the specified position
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InsertAt(int index, T value)
        {
            if (index == mLength)
            {
                Add(value);
                return;
            }

            EnsureCapacity(mLength + 1);
            Array.Copy(mContent, index, mContent, index + 1, mLength - index);
            mLength++;
            mContent[index] = value;
        }

        /// <summary>
        /// Remove element at the specified position.
        /// </summary>
        /// <param name="index"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveAt(int index, int count = 1)
        {
            if (index < 0 || index >= mLength)
                throw new ArgumentOutOfRangeException(nameof(index));

            Array.Copy(mContent, index + count, mContent, index, mLength - count - index);
            mLength -= count;
        }

        /// <summary>
        /// Adds specified number of empty elements to the end of the vector
        /// </summary>
        /// <param name="count"></param>
        public void AddEmptyElements(int count)
        {
            if (count <= 0)
                throw new ArgumentException("The number of elements should be greater than 0", nameof(count));
            EnsureCapacity(mLength + count);
            for (int i = 0; i < count; i++)
                mContent[i + mLength] = default;
            mLength += count;
        }

        /// <summary>
        /// Moves the content of the buffer
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="destinationIndex"></param>
        /// <param name="count"></param>
        public void Move(int sourceIndex, int destinationIndex, int count)
        {
            Array.Copy(mContent, sourceIndex, mContent, destinationIndex, count);
        }

        internal void AdjustLength(int newLength)
        {
            mLength = newLength;
        }

        /// <summary>
        /// Copies content to array
        /// </summary>
        /// <param name="sourceIndex"></param>
        /// <param name="length"></param>
        /// <param name="target"></param>
        /// <param name="targetIndex"></param>
        public void ToArray(int sourceIndex, int length, T[] target, int targetIndex)
            => Array.Copy(mContent, sourceIndex, target, targetIndex, length);

        public T[] ToArray()
        {
            T[] r = new T[Count];
            ToArray(0, Count, r, 0);
            return r;
        }

        public IEnumerator<T> GetEnumerator() => new SimpleListEnumerator(this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
