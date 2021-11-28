using System;
using System.Collections.Generic;
using System.Text;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The information about matching pairs.
    /// </summary>
    public class PairMatch
    {
        internal PairMatch(IntPtr editor, IntPtr pair)
        {
            StartLine = NativeExports.PairMatchStartLine(pair);
            EndLine = NativeExports.PairMatchEndLine(pair);

            IntPtr r = NativeExports.PairMatchStartLineRegion(pair);
            if (r != IntPtr.Zero)
                StartRegion = new LineRegion(StartLine, r, false);

            r = NativeExports.PairMatchEndLineRegion(pair);
            if (r != IntPtr.Zero)
                StartRegion = new LineRegion(EndLine, r, false);

            NativeExports.BaseEditorReleaseMatch(editor, pair);
        }

        /// <summary>
        /// Returns the line index at which the pair starts.
        /// </summary>
        public int StartLine { get; }
        /// <summary>
        /// Returns the line index at which the pair ends.
        /// </summary>
        public int EndLine { get; }
        /// <summary>
        /// Returns the line region of the pair start.
        /// </summary>
        public LineRegion  StartRegion { get;  }
        /// <summary>
        /// Returns the line region where pair ends.
        /// </summary>
        public LineRegion EndRegion { get; }
    }
}
