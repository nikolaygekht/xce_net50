using System;

namespace CailLomecb.ColorerTake5
{
    public sealed class ColorerRegexMatches : IDisposable
    {
        private IntPtr mColorerMatches;

        public bool Success => mColorerMatches != IntPtr.Zero;

        internal ColorerRegexMatches(IntPtr colorerMatches)
        {
            mColorerMatches = colorerMatches;
        }

        ~ColorerRegexMatches()
        {
            if (mColorerMatches != IntPtr.Zero)
            {
                NativeExports.DeleteMatches(mColorerMatches);
                mColorerMatches = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (mColorerMatches != IntPtr.Zero)
            {
                NativeExports.DeleteMatches(mColorerMatches);
                mColorerMatches = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        public int Count => NativeExports.MatchesCount(mColorerMatches);

        public ColorerRegexMatch this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                NativeExports.MatchGet(mColorerMatches, index, out int s, out int e);
                return new ColorerRegexMatch()
                {
                    Start = s,
                    End = e
                };
            }
        }
    }
}
