using System;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The class describes on syntax region of a line.
    /// </summary>
    public sealed class LineRegion
    {
        internal LineRegion(int line, IntPtr region, bool wholeChain)
        {
            Line = line;
            Start = NativeExports.LineRegionStart(region);
            End = NativeExports.LineRegionEnd(region) - 1;

            IntPtr r = NativeExports.LineRegionSyntaxRegion(region);
            if (r == IntPtr.Zero)
                SyntaxRegion = null;
            else
                SyntaxRegion = new SyntaxRegion(r);

            r = NativeExports.LineRegionStyledRegion(region);
            if (r == IntPtr.Zero)
                StyledRegion = null;
            else
                StyledRegion = new StyledRegion(r);

            if (wholeChain)
            {
                r = NativeExports.LineRegionNext(region);
                if (r != IntPtr.Zero)
                    Next = new LineRegion(line, r, true);
            }
        }

        public int Line { get; }

        /// <summary>
        /// Returns a next region or null if this region is the last in line
        /// </summary>
        public LineRegion Next { get; }

        /// <summary>
        /// Returns the index of the first character of the line in the region.
        /// </summary>
        public int Start { get; }
        /// <summary>
        /// Returns the index of the last character of the line in the region.
        /// </summary>
        public int End { get; }

        public int Length => End - Start + 1;

        /// <summary>
        /// Returns a syntax region defintion or null if the region isn't a syntax region.
        /// </summary>
        public SyntaxRegion SyntaxRegion { get; }

        /// <summary>
        /// Returns a style of the region or null if the region cannot be styled.
        /// </summary>
        public StyledRegion StyledRegion { get; }
    }
}
