using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CailLomecb.ColorerTake5
{
    public struct ColorerMatch
    {
        public int Start { get; set; }
        public int End { get; set; }
    }

    public sealed class ColorerMatches : IDisposable
    {
        private IntPtr mColorerMatches;

        public bool Success => mColorerMatches != IntPtr.Zero;

        internal ColorerMatches(IntPtr colorerMatches)
        {
            mColorerMatches = colorerMatches;
        }

        ~ColorerMatches()
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

        public ColorerMatch this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                    throw new IndexOutOfRangeException();
                int s, e;
                NativeExports.MatchGet(mColorerMatches, index, out s, out e);
                return new ColorerMatch()
                {
                    Start = s,
                    End = e
                };
            }
        }

        
    }

    public class ColorerRegex : IDisposable
    {
        private IntPtr mColorerRegex;

        public static ColorerRegex Parse(string regex)
        {
            IntPtr r;
            int rc = NativeExports.CreateRegex(regex, out r);
            if (rc == 0)
                return new ColorerRegex(r);

            string error = null;
            switch (rc)
            {
                case 1:
                    error = "The regular expression is malformed";
                    break;
                case 2:
                    error = "The syntax error in the regular expression";
                    break;
                case 3:
                    error = "The brackets aren't balanced";
                    break;
                case 4:
                    error = "The invalid character class is used";
                    break;
                case 5:
                    error = "The invalid operation is used";
                    break;
                default:
                    error = "Unexpected error while parsing the expression";
                    break;
            }
            throw new ArgumentException(error, nameof(regex));
        }

        internal ColorerRegex(IntPtr colorerRegex)
        {
            mColorerRegex = colorerRegex;
        }

        ~ColorerRegex()
        {
            if (mColorerRegex != IntPtr.Zero)
            {
                NativeExports.DeleteRegex(mColorerRegex);
                mColorerRegex = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            if (mColorerRegex != IntPtr.Zero)
            {
                NativeExports.DeleteRegex(mColorerRegex);
                mColorerRegex = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }

        public bool IsDiposed() => mColorerRegex == IntPtr.Zero;

        public ColorerMatches Parse(string text, int start = 0, int length = -1)
        {
            IntPtr pmathes;
            if (length < 0)
                length = text.Length;
            NativeExports.RegexParse(mColorerRegex, text, start, length, out pmathes);
            return new ColorerMatches(pmathes);
        }
        public ColorerMatches Find(string text, int start = 0, int length = -1)
        {
            IntPtr pmathes;
            if (length < 0)
                length = text.Length;
            NativeExports.RegexFind(mColorerRegex, text, start, length, out pmathes);
            return new ColorerMatches(pmathes);
        }

        public void FindAll(Func<int, int, bool> callback, string text, int start = 0, int length = -1)
        {
            if (length < 0)
                length = text.Length;

            NativeExports.RegexFindAll(mColorerRegex, text, start, length, (s, e) => callback(s, e));
        }
    }
}
