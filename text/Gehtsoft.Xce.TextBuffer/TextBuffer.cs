using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Scintilla.CellBuffer;
using Gehtsoft.Xce.TextBuffer.Undo;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Text buffer that manages lines of text efficiently using gap buffers
    /// </summary>
    public class TextBuffer
    {
        private readonly object mLock = new object();
        private readonly SplitList<SplitList<char>> mLines;
        private readonly TextBufferCallbackCollection mCallbacks;
        private readonly Stack<IUndoAction> mUndoActions = new Stack<IUndoAction>();
        private readonly Stack<IUndoAction> mRedoActions = new Stack<IUndoAction>();

        // Each entry pairs an open transaction with the BEFORE-state snapshot taken
        // when it began. The outermost transaction's BEFORE snapshot becomes the
        // "before" half of the StateSnapshotUndoAction wrapped around the transaction
        // when it commits.
        private readonly Stack<TransactionFrame> mTransactionStack = new Stack<TransactionFrame>();

        // Buffer-owned editor state. Adjusted in-line during edits and round-tripped
        // through undo/redo via BufferStateSnapshot.
        private readonly TextCursor mCursor = new TextCursor();
        private readonly TextBufferBlock mBlock = new TextBufferBlock();
        private readonly TextMarkerCollection mMarkers = new TextMarkerCollection();

        /// <summary>Caret + selection anchor. Owned by the buffer; round-tripped through undo/redo.</summary>
        public TextCursor Cursor => mCursor;

        /// <summary>Primary selection block. Owned by the buffer; round-tripped through undo/redo.</summary>
        public TextBufferBlock Block => mBlock;

        /// <summary>Marker collection. Owned by the buffer; round-tripped through undo/redo.</summary>
        public TextMarkerCollection Markers => mMarkers;

        private readonly struct TransactionFrame
        {
            public readonly UndoTransaction Transaction;
            public readonly BufferStateSnapshot Before;
            public TransactionFrame(UndoTransaction transaction, BufferStateSnapshot before)
            {
                Transaction = transaction;
                Before = before;
            }
        }

        /// <summary>
        /// Gets the number of lines in the buffer
        /// </summary>
        public int LinesCount
        {
            get
            {
                lock (mLock)
                {
                    return mLines.Count;
                }
            }
        }

        /// <summary>
        /// Gets the callback collection for registering change notifications
        /// </summary>
        public TextBufferCallbackCollection Callbacks => mCallbacks;

        /// <summary>
        /// Constructor - creates an empty text buffer
        /// </summary>
        public TextBuffer()
        {
            mLines = new SplitList<SplitList<char>>();
            mCallbacks = new TextBufferCallbackCollection(mLock);
        }

        /// <summary>
        /// Constructor - creates a text buffer from an array of strings
        /// </summary>
        /// <param name="lines">Initial lines</param>
        public TextBuffer(string[] lines)
        {
            if (lines == null || lines.Length == 0)
            {
                mLines = new SplitList<SplitList<char>>();
            }
            else
            {
                var lineBuffers = new SplitList<char>[lines.Length];
                for (int i = 0; i < lines.Length; i++)
                {
                    lineBuffers[i] = new SplitList<char>(lines[i].ToCharArray());
                }
                mLines = new SplitList<SplitList<char>>(lineBuffers);
            }
            mCallbacks = new TextBufferCallbackCollection(mLock);
        }

        /// <summary>
        /// Gets the length of a line. Returns 0 if line index is out of range.
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <returns>The length of the line, or 0 if line doesn't exist</returns>
        public int GetLineLength(int lineIndex)
        {
            lock (mLock)
            {
                if (lineIndex < 0 || lineIndex >= mLines.Count)
                    return 0;
                return mLines[lineIndex].Count;
            }
        }

        /// <summary>
        /// Copies a line to a span. Returns the number of characters copied.
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="target">The target span</param>
        /// <returns>The number of characters copied, or 0 if line doesn't exist</returns>
        public int GetLine(int lineIndex, Span<char> target)
        {
            lock (mLock)
            {
                if (lineIndex < 0 || lineIndex >= mLines.Count)
                    return 0;

                var line = mLines[lineIndex];
                if (line.Count == 0)
                    return 0;

                int length = Math.Min(line.Count, target.Length);
                line.ToArray(0, length, target);
                return length;
            }
        }

        /// <summary>
        /// Gets a line as a string. Returns empty string if line index is out of range.
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <returns>The line content as a string, or empty string if line doesn't exist</returns>
        public string GetLine(int lineIndex)
        {
            lock (mLock)
            {
                if (lineIndex < 0 || lineIndex >= mLines.Count)
                    return string.Empty;

                var line = mLines[lineIndex];
                int lineLength = line.Count;
                if (lineLength == 0)
                    return string.Empty;

                char[] buffer = new char[lineLength];
                line.ToArray(0, lineLength, buffer.AsSpan());
                return new string(buffer, 0, lineLength);
            }
        }

        /// <summary>
        /// Copies a substring from a line to a span. Returns the number of characters copied.
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="columnIndex">The starting column index</param>
        /// <param name="length">The length of the substring</param>
        /// <param name="target">The target span</param>
        /// <returns>The number of characters copied, or 0 if indices are out of range</returns>
        public int GetSubstring(int lineIndex, int columnIndex, int length, Span<char> target)
        {
            lock (mLock)
            {
                if (lineIndex < 0 || lineIndex >= mLines.Count)
                    return 0;

                var line = mLines[lineIndex];

                if (columnIndex < 0 || columnIndex >= line.Count)
                    return 0;

                if (length < 0)
                    return 0;

                // Adjust length if it exceeds line bounds
                if (columnIndex + length > line.Count)
                    length = line.Count - columnIndex;

                // Adjust length if it exceeds target span size
                if (length > target.Length)
                    length = target.Length;

                if (length == 0)
                    return 0;

                line.ToArray(columnIndex, length, target);
                return length;
            }
        }

        /// <summary>
        /// Gets a substring from a line. Returns empty string if indices are out of range.
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="columnIndex">The starting column index</param>
        /// <param name="length">The length of the substring</param>
        /// <returns>The substring, or empty string if indices are out of range</returns>
        public string GetSubstring(int lineIndex, int columnIndex, int length)
        {
            lock (mLock)
            {
                // First check if we can get anything at all
                if (lineIndex < 0 || lineIndex >= mLines.Count)
                    return string.Empty;

                var line = mLines[lineIndex];
                if (columnIndex < 0 || columnIndex >= line.Count || length <= 0)
                    return string.Empty;

                // Adjust length if it exceeds line bounds
                if (columnIndex + length > line.Count)
                    length = line.Count - columnIndex;

                if (length == 0)
                    return string.Empty;

                char[] buffer = new char[length];
                line.ToArray(columnIndex, length, buffer.AsSpan());
                return new string(buffer, 0, length);
            }
        }

        /// <summary>
        /// Ensures that the buffer has at least the specified number of lines
        /// </summary>
        /// <param name="lineIndex">The line index that must exist</param>
        private void EnsureLineExists(int lineIndex)
        {
            int startCount = mLines.Count;
            while (mLines.Count <= lineIndex)
            {
                mLines.Add(new SplitList<char>());
            }

            int addedLines = mLines.Count - startCount;
            if (addedLines > 0)
            {
                NotifyLinesInserted(startCount, addedLines);
            }
        }

        /// <summary>
        /// Ensures that a line has at least the specified length (extended with spaces if needed)
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="columnIndex">The column index that must exist</param>
        private void EnsureColumnExists(int lineIndex, int columnIndex)
        {
            var line = mLines[lineIndex];
            int currentLength = line.Count;

            if (columnIndex > currentLength)
            {
                int spacesToAdd = columnIndex - currentLength;
                line.Add(' ', spacesToAdd);
                NotifySubstringInserted(lineIndex, currentLength, spacesToAdd);
            }
        }

        // --- internal notification helpers: update buffer-owned state in-line, then external callbacks ---

        private void NotifyLinesInserted(int lineIndex, int count)
        {
            mCursor.OnLinesInserted(lineIndex, count);
            mBlock.OnLinesInserted(lineIndex, count);
            mMarkers.OnLinesInserted(lineIndex, count);
            mCallbacks.InvokeOnLinesInserted(lineIndex, count);
        }

        private void NotifyLinesDeleted(int lineIndex, int count)
        {
            mCursor.OnLinesDeleted(lineIndex, count);
            mBlock.OnLinesDeleted(lineIndex, count);
            mMarkers.OnLinesDeleted(lineIndex, count);
            mCallbacks.InvokeOnLinesDeleted(lineIndex, count);
        }

        private void NotifySubstringInserted(int lineIndex, int columnIndex, int length)
        {
            mCursor.OnSubstringInserted(lineIndex, columnIndex, length);
            mBlock.OnSubstringInserted(lineIndex, columnIndex, length);
            // markers ignore substring ops by design
            mCallbacks.InvokeOnSubstringInserted(lineIndex, columnIndex, length);
        }

        private void NotifySubstringDeleted(int lineIndex, int columnIndex, int length)
        {
            mCursor.OnSubstringDeleted(lineIndex, columnIndex, length);
            mBlock.OnSubstringDeleted(lineIndex, columnIndex, length);
            // markers ignore substring ops by design
            mCallbacks.InvokeOnSubstringDeleted(lineIndex, columnIndex, length);
        }

        /// <summary>
        /// Restore cursor / block / marker state from a snapshot. Called by
        /// StateSnapshotUndoAction during undo/redo.
        /// </summary>
        internal void RestoreStateFromSnapshot(BufferStateSnapshot snapshot)
        {
            snapshot.Restore(mCursor, mBlock, mMarkers);
        }

        /// <summary>
        /// Internal method to insert a new line with undo support
        /// </summary>
        internal void InsertLineInternal(int lineIndex, ReadOnlySpan<char> text, bool suppressUndo)
        {
            if (lineIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index cannot be negative");

            // Track auto-extended lines
            int linesBefore = mLines.Count;

            // Auto-extend if inserting beyond current line count
            if (lineIndex > mLines.Count)
            {
                EnsureLineExists(lineIndex - 1);
            }

            int autoAddedLines = mLines.Count - linesBefore;

            SplitList<char> newLine;
            if (text.Length == 0)
                newLine = new SplitList<char>();
            else
                newLine = new SplitList<char>(text);

            mLines.InsertAt(lineIndex, newLine);
            NotifyLinesInserted(lineIndex, 1);

            if (!suppressUndo)
            {
                RegisterUndoAction(new InsertLineUndoAction(this, lineIndex, text, autoAddedLines));
            }
        }

        /// <summary>
        /// Inserts a new line at the specified position using a span.
        /// <para>
        /// <b>Callback events:</b> if <paramref name="lineIndex"/> is past the current buffer
        /// end, the buffer first auto-creates the missing intermediate lines (firing one
        /// <c>OnLinesInserted</c> for the batch) and then inserts the requested line
        /// (firing a second <c>OnLinesInserted</c>). Listeners must be ready to receive 1
        /// or 2 events for a single InsertLine call. All events together correspond to a
        /// single undoable unit.
        /// </para>
        /// </summary>
        /// <param name="lineIndex">The line index where to insert</param>
        /// <param name="text">The text content of the new line</param>
        public void InsertLine(int lineIndex, ReadOnlySpan<char> text)
        {
            lock (mLock)
            {
                bool isOutermost = mTransactionStack.Count == 0;
                BufferStateSnapshot before = isOutermost
                    ? BufferStateSnapshot.Capture(mCursor, mBlock, mMarkers) : null;
                int countBefore = mUndoActions.Count;
                InsertLineInternal(lineIndex, text, false);
                WrapLastUndoActionWithSnapshot(isOutermost, before, countBefore);
            }
        }

        /// <summary>
        /// Inserts a new line at the specified position
        /// </summary>
        /// <param name="lineIndex">The line index where to insert</param>
        /// <param name="text">The text content of the new line</param>
        public void InsertLine(int lineIndex, string text = "")
        {
            InsertLine(lineIndex, text.AsSpan());
        }

        /// <summary>
        /// Internal method to delete a line with undo support
        /// </summary>
        internal void DeleteLineInternal(int lineIndex, bool suppressUndo)
        {
            if (lineIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index cannot be negative");

            // If line doesn't exist, do nothing
            if (lineIndex >= mLines.Count)
                return;

            // Register undo action BEFORE deletion
            if (!suppressUndo)
            {
                var line = mLines[lineIndex];
                const int stackAllocThreshold = 1024; // Use stack for small buffers (< 1KB)

                if (line.Count <= stackAllocThreshold)
                {
                    // Small buffer - use stackalloc
                    Span<char> buffer = stackalloc char[line.Count];
                    line.ToArray(0, line.Count, buffer);
                    RegisterUndoAction(new DeleteLineUndoAction(this, lineIndex, buffer));
                }
                else
                {
                    // Large buffer - use ArrayPool to avoid stack overflow
                    char[] rentedArray = ArrayPool<char>.Shared.Rent(line.Count);
                    try
                    {
                        Span<char> buffer = rentedArray.AsSpan(0, line.Count);
                        line.ToArray(0, line.Count, buffer);
                        RegisterUndoAction(new DeleteLineUndoAction(this, lineIndex, buffer));
                    }
                    finally
                    {
                        ArrayPool<char>.Shared.Return(rentedArray);
                    }
                }
            }

            mLines.RemoveAt(lineIndex);
            NotifyLinesDeleted(lineIndex, 1);
        }

        /// <summary>
        /// Deletes a line at the specified position. Does nothing if line doesn't exist.
        /// </summary>
        /// <param name="lineIndex">The line index to delete</param>
        public void DeleteLine(int lineIndex)
        {
            lock (mLock)
            {
                bool isOutermost = mTransactionStack.Count == 0;
                BufferStateSnapshot before = isOutermost
                    ? BufferStateSnapshot.Capture(mCursor, mBlock, mMarkers) : null;
                int countBefore = mUndoActions.Count;
                DeleteLineInternal(lineIndex, false);
                WrapLastUndoActionWithSnapshot(isOutermost, before, countBefore);
            }
        }

        /// <summary>
        /// Internal method to insert a substring with undo support
        /// </summary>
        internal void InsertSubstringInternal(int lineIndex, int columnIndex, ReadOnlySpan<char> text, bool suppressUndo)
        {
            if (lineIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index cannot be negative");

            if (columnIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(columnIndex), "Column index cannot be negative");

            if (text.Length == 0)
                return;

            // Track auto-extended lines and spaces
            int linesBefore = mLines.Count;
            int columnsBefore = 0;

            // Auto-extend lines if needed
            if (lineIndex >= mLines.Count)
            {
                EnsureLineExists(lineIndex);
            }

            int autoAddedLines = mLines.Count - linesBefore;

            var line = mLines[lineIndex];
            columnsBefore = line.Count;

            // Auto-extend line with spaces if needed
            if (columnIndex > line.Count)
            {
                EnsureColumnExists(lineIndex, columnIndex);
            }

            int autoAddedSpaces = line.Count - columnsBefore;

            line.InsertAt(columnIndex, text);
            NotifySubstringInserted(lineIndex, columnIndex, text.Length);

            if (!suppressUndo)
            {
                RegisterUndoAction(new InsertSubstringUndoAction(this, lineIndex, columnIndex, text, autoAddedLines, autoAddedSpaces));
            }
        }

        /// <summary>
        /// Inserts a substring into a line using a span. Auto-extends the buffer and line if needed.
        /// <para>
        /// <b>Callback events:</b> a single InsertSubstring call may fire MULTIPLE callback events
        /// when auto-extension is triggered:
        /// </para>
        /// <list type="number">
        /// <item><c>OnLinesInserted(...)</c> — once, if <paramref name="lineIndex"/> is past the buffer end and lines must be auto-added.</item>
        /// <item><c>OnSubstringInserted(...)</c> — once, if <paramref name="columnIndex"/> is past the line end and padding spaces are inserted.</item>
        /// <item><c>OnSubstringInserted(...)</c> — once for the actual <paramref name="text"/>.</item>
        /// </list>
        /// <para>
        /// Listeners must be ready to receive 1, 2, or 3 events for a single InsertSubstring call.
        /// All events together correspond to a single undoable unit: one Undo unwinds them all.
        /// </para>
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="columnIndex">The column index where to insert</param>
        /// <param name="text">The text to insert</param>
        public void InsertSubstring(int lineIndex, int columnIndex, ReadOnlySpan<char> text)
        {
            lock (mLock)
            {
                bool isOutermost = mTransactionStack.Count == 0;
                BufferStateSnapshot before = isOutermost
                    ? BufferStateSnapshot.Capture(mCursor, mBlock, mMarkers) : null;
                int countBefore = mUndoActions.Count;
                InsertSubstringInternal(lineIndex, columnIndex, text, false);
                WrapLastUndoActionWithSnapshot(isOutermost, before, countBefore);
            }
        }

        /// <summary>
        /// Inserts a substring into a line. Auto-extends the buffer and line if needed.
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="columnIndex">The column index where to insert</param>
        /// <param name="text">The text to insert</param>
        public void InsertSubstring(int lineIndex, int columnIndex, string text)
        {
            InsertSubstring(lineIndex, columnIndex, text.AsSpan());
        }

        /// <summary>
        /// Internal method to delete a substring with undo support
        /// </summary>
        internal void DeleteSubstringInternal(int lineIndex, int columnIndex, int length, bool suppressUndo)
        {
            if (lineIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(lineIndex), "Line index cannot be negative");

            if (columnIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(columnIndex), "Column index cannot be negative");

            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative");

            // If line doesn't exist or length is 0, do nothing
            if (lineIndex >= mLines.Count || length == 0)
                return;

            var line = mLines[lineIndex];

            // If column is beyond line end, do nothing
            if (columnIndex >= line.Count)
                return;

            // Adjust length if it goes beyond the end of line
            if (columnIndex + length > line.Count)
                length = line.Count - columnIndex;

            if (length > 0)
            {
                // Register undo action BEFORE deletion
                if (!suppressUndo)
                {
                    const int stackAllocThreshold = 1024; // Use stack for small buffers (< 1KB)

                    if (length <= stackAllocThreshold)
                    {
                        // Small buffer - use stackalloc
                        Span<char> buffer = stackalloc char[length];
                        line.ToArray(columnIndex, length, buffer);
                        RegisterUndoAction(new DeleteSubstringUndoAction(this, lineIndex, columnIndex, buffer));
                    }
                    else
                    {
                        // Large buffer - use ArrayPool to avoid stack overflow
                        char[] rentedArray = ArrayPool<char>.Shared.Rent(length);
                        try
                        {
                            Span<char> buffer = rentedArray.AsSpan(0, length);
                            line.ToArray(columnIndex, length, buffer);
                            RegisterUndoAction(new DeleteSubstringUndoAction(this, lineIndex, columnIndex, buffer));
                        }
                        finally
                        {
                            ArrayPool<char>.Shared.Return(rentedArray);
                        }
                    }
                }

                line.RemoveAt(columnIndex, length);
                NotifySubstringDeleted(lineIndex, columnIndex, length);
            }
        }

        /// <summary>
        /// Deletes a substring from a line. Adjusts length if it goes beyond the end of line. Does nothing if line or column doesn't exist.
        /// </summary>
        /// <param name="lineIndex">The line index</param>
        /// <param name="columnIndex">The column index where to start deletion</param>
        /// <param name="length">The length of the substring to delete</param>
        public void DeleteSubstring(int lineIndex, int columnIndex, int length)
        {
            lock (mLock)
            {
                bool isOutermost = mTransactionStack.Count == 0;
                BufferStateSnapshot before = isOutermost
                    ? BufferStateSnapshot.Capture(mCursor, mBlock, mMarkers) : null;
                int countBefore = mUndoActions.Count;
                DeleteSubstringInternal(lineIndex, columnIndex, length, false);
                WrapLastUndoActionWithSnapshot(isOutermost, before, countBefore);
            }
        }

        /// <summary>
        /// If we're at the outermost level (not inside a transaction) and the just-run
        /// internal action pushed something onto the undo stack, wrap that action with
        /// before/after state snapshots so undo/redo round-trips cursor/block/markers.
        /// </summary>
        private void WrapLastUndoActionWithSnapshot(bool isOutermost, BufferStateSnapshot before, int countBefore)
        {
            if (!isOutermost) return;
            if (mUndoActions.Count <= countBefore) return; // op was a no-op (nothing pushed)

            var inner = mUndoActions.Pop();
            var after = BufferStateSnapshot.Capture(mCursor, mBlock, mMarkers);
            mUndoActions.Push(new StateSnapshotUndoAction(this, inner, before, after));
        }

        /// <summary>
        /// Registers an undo action. If a transaction is active, adds to the transaction instead.
        /// </summary>
        private void RegisterUndoAction(IUndoAction action)
        {
            if (mTransactionStack.Count > 0)
            {
                // Add to the current transaction
                mTransactionStack.Peek().Transaction.AddAction(action);
            }
            else
            {
                // No transaction active, add directly to undo stack
                mUndoActions.Push(action);
                mRedoActions.Clear(); // Clear redo stack when new action is performed
            }
        }

        /// <summary>
        /// Checks if undo is available
        /// </summary>
        public bool CanUndo
        {
            get
            {
                lock (mLock)
                {
                    return mUndoActions.Count > 0;
                }
            }
        }

        /// <summary>
        /// Checks if redo is available
        /// </summary>
        public bool CanRedo
        {
            get
            {
                lock (mLock)
                {
                    return mRedoActions.Count > 0;
                }
            }
        }

        /// <summary>
        /// Undoes the last action
        /// </summary>
        public void Undo()
        {
            lock (mLock)
            {
                if (mTransactionStack.Count > 0)
                    throw new InvalidOperationException("Cannot Undo while a transaction is open");

                if (mUndoActions.Count == 0)
                    throw new InvalidOperationException("No actions to undo");

                var action = mUndoActions.Pop();
                try
                {
                    action.Undo();
                }
                catch (Exception ex)
                {
                    // The action may have applied part of its work before throwing.
                    // The buffer can no longer trust its history; drop both stacks
                    // so subsequent code cannot replay corrupt state.
                    mUndoActions.Clear();
                    mRedoActions.Clear();
                    throw new UndoCorruptedException(
                        "Undo action failed; undo/redo history has been cleared.", ex);
                }
                mRedoActions.Push(action);
            }
        }

        /// <summary>
        /// Redoes the last undone action
        /// </summary>
        public void Redo()
        {
            lock (mLock)
            {
                if (mTransactionStack.Count > 0)
                    throw new InvalidOperationException("Cannot Redo while a transaction is open");

                if (mRedoActions.Count == 0)
                    throw new InvalidOperationException("No actions to redo");

                var action = mRedoActions.Pop();
                try
                {
                    action.Redo();
                }
                catch (Exception ex)
                {
                    mUndoActions.Clear();
                    mRedoActions.Clear();
                    throw new UndoCorruptedException(
                        "Redo action failed; undo/redo history has been cleared.", ex);
                }
                mUndoActions.Push(action);
            }
        }

        /// <summary>
        /// Test-only hook to inject a custom <see cref="IUndoAction"/> directly onto the
        /// undo stack. Used by lifecycle tests that need to simulate a faulty extension
        /// action; not part of the public API.
        /// </summary>
        internal void RegisterUndoActionForTesting(IUndoAction action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (mLock)
            {
                mUndoActions.Push(action);
                mRedoActions.Clear();
            }
        }

        /// <summary>
        /// Begins an undo transaction. All operations performed until disposal will be grouped as a single undoable action.
        /// </summary>
        /// <returns>A disposable transaction object. Dispose to commit the transaction.</returns>
        public IDisposable BeginUndoTransaction()
        {
            lock (mLock)
            {
                var transaction = new UndoTransaction();
                // Capture the BEFORE snapshot only for the outermost transaction;
                // nested ones share the outer's snapshot semantically (we only restore
                // at the outermost boundary).
                BufferStateSnapshot before = mTransactionStack.Count == 0
                    ? BufferStateSnapshot.Capture(mCursor, mBlock, mMarkers) : null;
                mTransactionStack.Push(new TransactionFrame(transaction, before));
                return new UndoTransactionScope(this, transaction);
            }
        }

        /// <summary>
        /// Internal method to commit a transaction
        /// </summary>
        private void CommitTransaction(UndoTransaction transaction)
        {
            lock (mLock)
            {
                if (mTransactionStack.Count == 0)
                    throw new InvalidOperationException("No active transaction");

                // Validate before mutating the stack: if a wrong-order Dispose throws here,
                // the stack must remain intact so the actually-active transaction can still commit.
                if (mTransactionStack.Peek().Transaction != transaction)
                    throw new InvalidOperationException("Transaction mismatch");

                var frame = mTransactionStack.Pop();

                // Only commit to undo stack if this is the outermost transaction
                if (mTransactionStack.Count == 0)
                {
                    // Only add non-empty transactions
                    if (transaction.Count > 0)
                    {
                        // Wrap with before/after snapshots so cursor/block/markers round-trip.
                        var after = BufferStateSnapshot.Capture(mCursor, mBlock, mMarkers);
                        mUndoActions.Push(new StateSnapshotUndoAction(this, transaction, frame.Before, after));
                        mRedoActions.Clear(); // Clear redo stack when new action is performed
                    }
                }
                else
                {
                    // This is a nested transaction, add it to the parent transaction
                    if (transaction.Count > 0)
                    {
                        mTransactionStack.Peek().Transaction.AddAction(transaction);
                    }
                }
            }
        }

        /// <summary>
        /// Helper class for transaction scope management
        /// </summary>
        private class UndoTransactionScope : IDisposable
        {
            private readonly TextBuffer mBuffer;
            private readonly UndoTransaction mTransaction;
            private bool mDisposed;

            public UndoTransactionScope(TextBuffer buffer, UndoTransaction transaction)
            {
                mBuffer = buffer;
                mTransaction = transaction;
            }

            public void Dispose()
            {
                if (!mDisposed)
                {
                    mBuffer.CommitTransaction(mTransaction);
                    mDisposed = true;
                }
            }
        }
    }
}
