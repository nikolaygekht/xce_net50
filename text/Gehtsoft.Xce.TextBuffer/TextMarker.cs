namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Represents a marker at a specific position in the text buffer
    /// </summary>
    public class TextMarker
    {
        /// <summary>
        /// Gets or sets the marker identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the line position (0-based)
        /// </summary>
        public int Line { get; set; }

        /// <summary>
        /// Gets or sets the column position (0-based)
        /// </summary>
        public int Column { get; set; }

        /// <summary>
        /// Creates a new text marker
        /// </summary>
        public TextMarker()
        {
            Id = string.Empty;
            Line = 0;
            Column = 0;
        }

        /// <summary>
        /// Creates a new text marker with specified parameters
        /// </summary>
        /// <param name="id">The marker identifier</param>
        /// <param name="line">The line position</param>
        /// <param name="column">The column position</param>
        public TextMarker(string id, int line, int column)
        {
            Id = id ?? string.Empty;
            Line = line;
            Column = column;
        }
    }
}
