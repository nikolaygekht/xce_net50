using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Scintilla.CellBuffer;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// The text buffer
    /// </summary>
    public class TextBuffer : IDisposable
    {
        private readonly MutexSlim mSyncRoot = new MutexSlim();

        private readonly SplitList<SplitList<char>> mContent = new SplitList<SplitList<char>>();

        /// <summary>
        /// The number of the lines if the buffer
        /// </summary>
        public int LinesCount => mContent.Count;

        private readonly List<PositionMarker> mMarkers = new List<PositionMarker>();

        private TextBufferStatus mStatus;

        public TextBufferStatus Status
        {
            get => mStatus;
            set
            {
                mStatus = value;

                mMarkers[0] = mStatus.CursorPosition;
                mMarkers[1] = mStatus.BlockStart;
                mMarkers[2] = mStatus.BlockEnd;
            }
        }

        public IReadOnlyList<PositionMarker> SavedPositions { get; } = new PositionMarkerCollection();

        /// <summary>
        /// Constructor
        /// </summary>
        public TextBuffer()
        {
            mMarkers.Add(null);
            mMarkers.Add(null);
            mMarkers.Add(null);
            Status = new TextBufferStatus();
            for (int i = 0; i < SavedPositions.Count; i++)
                mMarkers.Add(SavedPositions[i]);
        }

        /// <summary>
        /// Returns a line of the text as a new array
        /// </summary>
        /// <param name="line"></param>
        /// <param name="target"></param>
        public void GetLine(int line, out char[] target)
        {
            using var @lock = mSyncRoot.Lock();

            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));
            if (line >= mContent.Count)
                target = Array.Empty<char>();
            else
            {
                var lineContent = mContent[line];
                if (lineContent.Count == 0)
                    target = Array.Empty<char>();
                else
                {
                    target = new char[lineContent.Count];
                    lineContent.ToArray(0, target.Length, target, 0);
                }
            }
        }

        /// <summary>
        /// Returns a line of the text into the existing array
        /// </summary>
        /// <param name="line"></param>
        /// <param name="target"></param>
        public void GetLine(int line, char[] target, out int length)
        {
            using var @lock = mSyncRoot.Lock();

            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));
            if (line >= mContent.Count)
            {
                length = 0;
                return;
            }

            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var lineContent = mContent[line];
            length = lineContent.Count;
            if (length > target.Length)
                length = target.Length;
            lineContent.ToArray(0, length, target, 0);
        }

        /// <summary>
        /// Returns a line as a string
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string GetLine(int line)
        {
            GetLine(line, out var target);
            return new string(target);
        }

        /// <summary>
        /// Returns a part of the line of the text as a new array
        /// </summary>
        /// <param name="line"></param>
        /// <param name="target"></param>
        public void GetSubstring(int line, int column, int length, out char[] target)
        {
            using var @lock = mSyncRoot.Lock();

            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (line >= mContent.Count)
                target = Array.Empty<char>();
            else
            {
                var lineContent = mContent[line];
                if (lineContent.Count - column < length)
                    length = lineContent.Count - column;
                if (length < 0)
                    length = 0;

                if (length == 0)
                    target = Array.Empty<char>();
                else
                {
                    target = new char[length];
                    lineContent.ToArray(column, length, target, 0);
                }
            }
        }

        /// <summary>
        /// Returns a part of the line of the text into an existing new array
        /// </summary>
        /// <param name="line"></param>
        /// <param name="target"></param>
        public void GetSubstring(int line, int column, int length, char[] target, out int actualLength)
        {
            using var @lock = mSyncRoot.Lock();

            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (line >= mContent.Count)
                actualLength = 0;
            else
            {
                var lineContent = mContent[line];
                if (lineContent.Count - column < length)
                    length = lineContent.Count - column;

                if (length < 0)
                    length = 0;

                if (length > target.Length)
                    length = target.Length;

                if (length > 0)
                    lineContent.ToArray(column, length, target, 0);

                actualLength = length;
            }
        }

        /// <summary>
        /// Returns a part of a line of the text as a string
        /// </summary>
        /// <param name="line"></param>
        /// <param name="column"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public string GetSubstring(int line, int column, int length)
        {
            GetSubstring(line, column, length, out var characters);
            return new string(characters);
        }

        internal void AppendLine(string text, bool suppressUndo = false) => AppendLine(text?.ToCharArray(), suppressUndo);

        internal void InsertLine(int line, string text, bool suppressUndo = false) => InsertLine(line, text?.ToCharArray(), suppressUndo);

        internal void InsertSubstring(int line, int position, string text, bool suppressUndo = false) => InsertSubstring(line, position, text?.ToCharArray(), suppressUndo);

        internal void AppendLine(char[] text, bool suppressUndo = false)
        {
            using var @lock = mSyncRoot.Lock();

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            mContent.Add(new SplitList<char>(text));
            UpdateMarkersLineInserted(mContent.Count, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int AdjustLines(int line)
        {
            var count = 0;
            while (mContent.Count < line)
            {
                mContent.Add(new SplitList<char>());
                count++;
            }
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int AdjustLineContent(SplitList<char> line, int position)
        {
            if (line.Count >= position)
                return 0;
            int count = position - line.Count;
            line.Add(' ', count);
            return count;
        }

        internal void InsertLine(int line, char[] text, bool suppressUndo = false)
        {
            using var @lock = mSyncRoot.Lock();

            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));

            AdjustLines(line);

            if (text == null)
                throw new ArgumentNullException(nameof(text));

            if (line == mContent.Count)
                mContent.Add(new SplitList<char>(text));
            else
                mContent.InsertAt(line, new SplitList<char>(text));

            UpdateMarkersLineInserted(line, 1);
        }

        internal void InsertCharacter(int line, int position, char character, bool suppressUndo = false)
        {
            Span<char> v = stackalloc char[1];
            v[0] = character;
            InsertSubstring(line, position, v, suppressUndo);
        }

        internal void InsertSubstring(int line, int position, char[] text, bool suppressUndo = false)
        {
            if (text == null)
                throw new ArgumentNullException(nameof(text));

            InsertSubstring(line, position, new Span<char>(text), suppressUndo);
        }

        private void InsertSubstring(int line, int position, Span<char> text, bool suppressUndo)
        {
            using var @lock = mSyncRoot.Lock();

            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));

            AdjustLines(line + 1);

            var lineContent = mContent[line];

            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            AdjustLineContent(lineContent, position);

            if (text.Length > 0)
            {
                lineContent.InsertAt(position, text);
                UpdateMarkersCharactersInserted(line, position, text.Length);
            }
        }

        internal void RemoveSubstring(int line, int position, int length, bool suppressUndo = false)
        {
            using var @lock = mSyncRoot.Lock();

            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));

            if (line >= mContent.Count)
                return;

            var lineContent = mContent[line];

            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));

            if (position + length > lineContent.Count)
            {
                length = lineContent.Count - position;
                if (length <= 0)
                    return;
            }

            lineContent.RemoveAt(position, length);
            UpdateMarkersCharactersRemoved(line, position, length);
        }

        internal void RemoveLine(int line, bool suppressUndo = false) => RemoveLines(line, 1, suppressUndo);

        internal void RemoveLines(int line, int count, bool suppressUndo = false)
        {
            using var @lock = mSyncRoot.Lock();

            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));

            if (line + count > mContent.Count)
            {
                count = mContent.Count - line;
                if (count <= 0)
                    return;
            }

            mContent.RemoveAt(line, count);
            UpdateMarkersLineRemoved(line, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMarkersLineInserted(int line, int count)
        {
            for (int i = 0; i < mMarkers.Count; i++)
            {
                var m = mMarkers[i];
                if (m.Line >= line)
                    m.Line += count;
            }
            ValidateBlockMode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMarkersLineRemoved(int line, int count)
        {
            for (int i = 0; i < mMarkers.Count; i++)
            {
                var m = mMarkers[i];
                var ml = m.Line;
                if (ml >= line && ml < line + count)
                {
                    if (m.RemoveWhenLineDeleted)
                        m.Line = -1;
                    else
                        m.Line = line;
                }
                else if (m.Line >= line + count)
                    m.Line -= count;
            }
            ValidateBlockMode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMarkersCharactersInserted(int line, int position, int count)
        {
            for (int i = 0; i < mMarkers.Count; i++)
            {
                var m = mMarkers[i];
                if (m.Line == line && m.Column >= position)
                    m.Column += count;
            }
            ValidateBlockMode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateMarkersCharactersRemoved(int line, int position, int count)
        {
            for (int i = 0; i < mMarkers.Count; i++)
            {
                var m = mMarkers[i];
                var mc = m.Column;
                if (m.Line == line)
                {
                    if (mc >= position && mc < position + count)
                    {
                        if (m.RemoveWhenLineDeleted)
                            m.Line = m.Column = -1;
                        else
                            m.Column = position;
                    }
                    else if (mc >= position + count)
                        m.Column -= count;
                }
            }
            ValidateBlockMode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateBlockMode()
        {
            if (Status.BlockMode != BlockMode.None && (Status.BlockStart > Status.BlockEnd || Status.BlockStart.Line < 0 || Status.BlockEnd.Line < 0))
                Status.BlockMode = BlockMode.None;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            mSyncRoot.Dispose();
        }
    }
}


