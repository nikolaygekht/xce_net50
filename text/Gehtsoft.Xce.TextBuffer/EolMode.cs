namespace Gehtsoft.Xce.TextBuffer
{
    /// <summary>
    /// The end-of-line mode
    /// </summary>
    public enum EolMode
    {
        Cr,
        Lf,
        CrLf,
        Windows = CrLf,
        Linux = Lf,
    }
}
