namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// Type of text selection block
    /// </summary>
    public enum TextBufferBlockType
    {
        /// <summary>
        /// No block selected
        /// </summary>
        None,

        /// <summary>
        /// Line block - sequence of full lines from first to last selected line
        /// </summary>
        Line,

        /// <summary>
        /// Box block - set of substrings between first and last column for all lines from first to last
        /// </summary>
        Box,

        /// <summary>
        /// Stream block - substring from first position to end of first line, all lines in between,
        /// and substring from beginning of last line to last position
        /// </summary>
        Stream
    }
}
