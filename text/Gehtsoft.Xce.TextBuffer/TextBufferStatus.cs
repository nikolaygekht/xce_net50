using System;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// The status of the text buffer
    /// </summary>
    public sealed class TextBufferStatus : IEquatable<TextBufferStatus>
    {
        /// <summary>
        /// The position of the cursor
        /// </summary>
        public PositionMarker CursorPosition { get; }

        /// <summary>
        /// The position of the block start
        /// </summary>
        public PositionMarker BlockStart { get; }

        /// <summary>
        /// The position of the block end
        /// </summary>
        public PositionMarker BlockEnd { get; }

        /// <summary>
        /// The block selection mode
        /// </summary>
        public BlockMode BlockMode { get; internal set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TextBufferStatus()
        {
            CursorPosition = new PositionMarker("cursor", 0, 0, false);
            BlockStart = new PositionMarker("blockStart", 0, 0);
            BlockEnd = new PositionMarker("blockEnd", 0, 0);
            BlockMode = BlockMode.None;
        }

        /// <summary>
        /// Copying constructor
        /// </summary>
        public TextBufferStatus(PositionMarker cursor, PositionMarker blockStart, PositionMarker blockEnd, BlockMode blockMode)
        {
            CursorPosition = cursor;
            BlockStart = blockStart;
            BlockEnd = blockEnd;
            BlockMode = blockMode;
        }

        /// <summary>
        /// Creates a copy of the status
        /// </summary>
        /// <returns></returns>
        public TextBufferStatus Clone()
        {
            return new TextBufferStatus(CursorPosition.Clone(), BlockStart.Clone(), BlockEnd.Clone(), BlockMode);
        }

        /// <summary>
        /// Checks whether two buffer status equals
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(TextBufferStatus other)
        {
            if (other is null)
                return false;
            return CursorPosition.Equals(other.CursorPosition) &&
                BlockStart.Equals(other.BlockStart) &&
                BlockEnd.Equals(other.BlockEnd) &&
                BlockMode == other.BlockMode;
        }

        /// <summary>
        /// Checks whether the object equals to another object.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is TextBufferStatus other)
                return Equals(other);
            return false;
        }

        /// <summary>
        /// Returns hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
            => HashCode.Combine(CursorPosition, BlockStart, BlockEnd, BlockMode);
    }
}

