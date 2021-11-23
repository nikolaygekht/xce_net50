using System;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The class describes on syntax region of a line. 
    /// </summary>
    public sealed class LineRegion
    {
        private readonly int mLine;
        private readonly int mStart;
        private readonly int mEnd;
        private SyntaxRegion mSyntaxRegion;
        private StyledRegion mStyledRegion;
        private LineRegion mNext;


        internal LineRegion(int line, IntPtr region, bool wholeChain)
        {
            mLine = line;
            mStart = NativeExports.LineRegionStart(region);
            mEnd = NativeExports.LineRegionEnd(region) - 1;
            IntPtr r;

            r = NativeExports.LineRegionSyntaxRegion(region);
            if (r == IntPtr.Zero)
                mSyntaxRegion = null;
            else
                mSyntaxRegion = new SyntaxRegion(r);

            r = NativeExports.LineRegionStyledRegion(region);
            
            if (r == IntPtr.Zero)
                mStyledRegion = null;
            else
                mStyledRegion = new StyledRegion(r);

            if (wholeChain)
            {
                r = NativeExports.LineRegionNext(region);
                if (r != IntPtr.Zero)
                    mNext = new LineRegion(line, r, true);
            }


        }

        public int Line => mLine;

        /// <summary>
        /// Returns a next region or null if this region is the last in line
        /// </summary>
        public LineRegion Next => mNext;

        /// <summary>
        /// Returns the index of the first character of the line in the region.
        /// </summary>
        public int Start => mStart;
        /// <summary>
        /// Returns the index of the last character of the line in the region.
        /// </summary>
        public int End => mEnd;

        public int Length => mEnd - mStart + 1;

        /// <summary>
        /// Returns a syntax region defintion or null if the region isn't a syntax region.
        /// </summary>
        public SyntaxRegion SyntaxRegion => mSyntaxRegion;

        /// <summary>
        /// Returns a style of the region or null if the region cannot be styled. 
        /// </summary>
        public StyledRegion StyledRegion => mStyledRegion;
    }
}
