using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Scintilla.CellBuffer;

namespace Gehtsoft.Xce.TextBuffer
{

    /// <summary>
    /// The text buffer
    /// </summary>
    public class TextBuffer
    {
        /// <summary>
        /// The event invoked when the text buffer is changed
        /// </summary>
        public event TextBufferChangedDelegate TextBufferChanged;

        /// <summary>
        /// The object for synchronization of operations with buffer
        /// </summary>
        public object SyncRoot { get; } = new object();

        /// <summary>
        /// The file name of associated with the text buffer
        /// </summary>
        public string FileName { get; set; }

        private readonly SplitList<SplitList<char>> mContent = new SplitList<SplitList<char>>();

        /// <summary>
        /// The number of the lines if the buffer
        /// </summary>
        public int LinesCount => mContent.Count;

        /// <summary>
        /// The buffer encoding code page
        /// </summary>
        public int Codepage { get; set; }

        /// <summary>
        /// The end of line mode
        /// </summary>
        public EolMode EolMode { get; set; }

        /// <summary>
        /// The list of the markers
        /// </summary>
        private readonly List<PositionMarker> mMarkers = new List<PositionMarker>();

        /// <summary>
        /// Constructor
        /// </summary>
        public TextBuffer()
        {
        }

        /// <summary>
        /// Returns a line of the text as a new array
        /// </summary>
        /// <param name="line"></param>
        /// <param name="target"></param>
        public void GetLine(int line, out char[] target)
        {
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
                    GetLine(line, target, out _);
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
            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));
            if (line >= mContent.Count)
            {
                length = 0;
                return;
            }
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
            if (line < 0)
                throw new ArgumentOutOfRangeException(nameof(line));
            if (column < 0)
                throw new ArgumentOutOfRangeException(nameof(column));
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

                if (length != 0)
                {
                    target = new char[length];
                    lineContent.ToArray(column, length, target, 0);
                }

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

        internal void AppendLine(string line) => AppendLine(line.ToCharArray());

        internal void InsertLine(int index, string line) => InsertLine(index, line.ToCharArray());

        internal void InsertSubstring(int index, int position, string substring) => InsertSubstring(index, position, substring.ToCharArray());

        internal void InsertCharacter(int index, int position, char character)
        {
        }

        internal void AppendLine(char[] line)
        {
        }

        internal void InsertLine(int index, char[] line)
        {
        }

        internal void InsertSubstring(int index, int position, char[] substring)
        {
        }

        internal void RemoveSubstring(int index, int position, int length)
        {
        }

        internal void RemoveLine(int index)
        {
        }

        internal void RemoveLines(int index, int count)
        {
        }
    }
}
