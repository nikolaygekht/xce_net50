namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The interface to the line source provider
    /// </summary>
    public interface IColorerLineSource
    {
        /// <summary>
        /// Returns the line content
        /// </summary>
        /// <param name="line">The index of the line</param>
        /// <returns></returns>
        string GetLine(int line);
    }
}
