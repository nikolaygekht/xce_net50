using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Collection of text buffer callbacks that can invoke all registered callbacks
    /// </summary>
    public class TextBufferCallbackCollection
    {
        private readonly List<ITextBufferCallback> mCallbacks;
        private readonly object mLock;

        public TextBufferCallbackCollection(object lockObject)
        {
            mCallbacks = new List<ITextBufferCallback>();
            mLock = lockObject ?? throw new ArgumentNullException(nameof(lockObject));
        }

        /// <summary>
        /// Adds a callback to the collection
        /// </summary>
        /// <param name="callback">The callback to add</param>
        public void Add(ITextBufferCallback callback)
        {
            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            lock (mLock)
            {
                if (!mCallbacks.Contains(callback))
                    mCallbacks.Add(callback);
            }
        }

        /// <summary>
        /// Removes a callback from the collection
        /// </summary>
        /// <param name="callback">The callback to remove</param>
        /// <returns>True if the callback was removed, false if it wasn't in the collection</returns>
        public bool Remove(ITextBufferCallback callback)
        {
            lock (mLock)
            {
                return mCallbacks.Remove(callback);
            }
        }

        /// <summary>
        /// Removes all callbacks from the collection
        /// </summary>
        public void Clear()
        {
            lock (mLock)
            {
                mCallbacks.Clear();
            }
        }

        /// <summary>
        /// Gets the number of callbacks in the collection
        /// </summary>
        public int Count
        {
            get
            {
                lock (mLock)
                {
                    return mCallbacks.Count;
                }
            }
        }

        /// <summary>
        /// Invokes OnLinesInserted on all registered callbacks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnLinesInserted(int lineIndex, int count)
        {
            // Create a snapshot to allow callbacks to modify the collection during iteration
            ITextBufferCallback[] snapshot;
            lock (mLock)
            {
                if (mCallbacks.Count == 0)
                    return;
                snapshot = mCallbacks.ToArray();
            }

            // Invoke callbacks outside the lock to avoid deadlock if callback modifies collection
            foreach (var callback in snapshot)
                callback.OnLinesInserted(lineIndex, count);
        }

        /// <summary>
        /// Invokes OnLinesDeleted on all registered callbacks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnLinesDeleted(int lineIndex, int count)
        {
            // Create a snapshot to allow callbacks to modify the collection during iteration
            ITextBufferCallback[] snapshot;
            lock (mLock)
            {
                if (mCallbacks.Count == 0)
                    return;
                snapshot = mCallbacks.ToArray();
            }

            // Invoke callbacks outside the lock to avoid deadlock if callback modifies collection
            foreach (var callback in snapshot)
                callback.OnLinesDeleted(lineIndex, count);
        }

        /// <summary>
        /// Invokes OnSubstringInserted on all registered callbacks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnSubstringInserted(int lineIndex, int columnIndex, int length)
        {
            // Create a snapshot to allow callbacks to modify the collection during iteration
            ITextBufferCallback[] snapshot;
            lock (mLock)
            {
                if (mCallbacks.Count == 0)
                    return;
                snapshot = mCallbacks.ToArray();
            }

            // Invoke callbacks outside the lock to avoid deadlock if callback modifies collection
            foreach (var callback in snapshot)
                callback.OnSubstringInserted(lineIndex, columnIndex, length);
        }

        /// <summary>
        /// Invokes OnSubstringDeleted on all registered callbacks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvokeOnSubstringDeleted(int lineIndex, int columnIndex, int length)
        {
            // Create a snapshot to allow callbacks to modify the collection during iteration
            ITextBufferCallback[] snapshot;
            lock (mLock)
            {
                if (mCallbacks.Count == 0)
                    return;
                snapshot = mCallbacks.ToArray();
            }

            // Invoke callbacks outside the lock to avoid deadlock if callback modifies collection
            foreach (var callback in snapshot)
                callback.OnSubstringDeleted(lineIndex, columnIndex, length);
        }
    }
}
