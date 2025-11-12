namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// End-of-line mode
    /// </summary>
    public enum EolMode
    {
        /// <summary>
        /// Carriage Return + Line Feed (Windows style, \r\n)
        /// </summary>
        CrLf,

        /// <summary>
        /// Carriage Return only (Mac classic style, \r)
        /// </summary>
        Cr,

        /// <summary>
        /// Line Feed only (Unix/Linux style, \n)
        /// </summary>
        Lf
    }
}
