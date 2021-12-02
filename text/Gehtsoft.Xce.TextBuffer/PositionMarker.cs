using System;

namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// The market for a position in the text
    /// </summary>
    public sealed class PositionMarker : IComparable<PositionMarker>, IEquatable<PositionMarker>
    {
        /// <summary>
        /// The name of the market
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The number of the line
        /// </summary>
        public int Line { get; set; }
        /// <summary>
        /// The number of the column
        /// </summary>
        public int Column { get; set; }

        public PositionMarker(string name, int line, int column)
        {
            Name = name;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Creates a copy of the position marker
        /// </summary>
        /// <returns></returns>
        public PositionMarker Clone()
        {
            return new PositionMarker(Name, Line, Column);
        }

        /// <summary>
        /// Compares a position market to another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(int otherLine, int otherColumn)
        {
            if (Line < otherLine)
                return -1;
            else if (Line > otherLine)
                return 1;
            else if (Column < otherColumn)
                return -1;
            else if (Column > otherColumn)
                return 1;
            return 0;
        }

        /// <summary>
        /// Compares a position market to another
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(PositionMarker other)
        {
            if (other == null)
                return 1;
            return CompareTo(other.Line, other.Column);
        }

        public bool Equals(PositionMarker pm) => CompareTo(pm) == 0 && Name == pm.Name;

        public bool EqualsTo(int line, int column) => CompareTo(line, column) == 0;

        /// <summary>
        /// Checks whether position markets are equal
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is PositionMarker pm)
                return Equals(pm);
            return false;
        }

        /// <summary>
        /// Returns position market hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => HashCode.Combine(Name, Line, Column);

        public static bool operator ==(PositionMarker a, PositionMarker b) => a.CompareTo(b) == 0;
        public static bool operator !=(PositionMarker a, PositionMarker b) => a.CompareTo(b) != 0;
        public static bool operator >(PositionMarker a, PositionMarker b) => a.CompareTo(b) > 0;
        public static bool operator >=(PositionMarker a, PositionMarker b) => a.CompareTo(b) >= 0;
        public static bool operator <(PositionMarker a, PositionMarker b) => a.CompareTo(b) < 0;
        public static bool operator <=(PositionMarker a, PositionMarker b) => a.CompareTo(b) <= 0;
    }
}

