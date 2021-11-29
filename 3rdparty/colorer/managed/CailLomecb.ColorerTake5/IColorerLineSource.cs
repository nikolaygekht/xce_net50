namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The interface to the line source provider
    /// </summary>
    public interface IColorerLineSource
    {
    }

    public interface IColorerLineSourceArray : IColorerLineSource
    {
        /// <summary>
        /// Returns the line content
        /// </summary>
        /// <param name="line">The index of the line</param>
        /// <returns></returns>
        bool GetLine(int line, out char[] target, out int length);
    }

    public interface IColorerLineSourceString : IColorerLineSource
    {
        /// <summary>
        /// Returns the line content
        /// </summary>
        /// <param name="line">The index of the line</param>
        /// <returns></returns>
        bool GetLine(int line, out string target, out int length);
    }
}
