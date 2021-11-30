namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// The market for a position in the text
    /// </summary>
    public class PositionMarker
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
    }
}
