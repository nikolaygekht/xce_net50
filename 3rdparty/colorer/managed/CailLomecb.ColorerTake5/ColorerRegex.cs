using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CailLomecb.ColorerTake5
{
    public sealed class ColorerRegex : IDisposable
    {
        private IntPtr mColorerRegex;

        public static ColorerRegex Parse(string regex)
        {
            int rc = NativeExports.CreateRegex(regex, out IntPtr r);
            if (rc == 0)
                return new ColorerRegex(r);

            string error;
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

        public ColorerRegexMatches Parse(string text, int start, int length)
        {
            if (length < 0)
                length = text.Length;
            NativeExports.RegexParse(mColorerRegex, text, start, length, out IntPtr pmathes);
            return new ColorerRegexMatches(pmathes);
        }
        public ColorerRegexMatches Find(string text, int start = 0, int length = -1)
        {
            if (length < 0)
                length = text.Length;
            NativeExports.RegexFind(mColorerRegex, text, start, length, out IntPtr pmathes);
            return new ColorerRegexMatches(pmathes);
        }

        public void FindAll(Func<int, int, bool> callback, string text, int start = 0, int length = -1)
        {
            if (length < 0)
                length = text.Length;

            NativeExports.RegexFindAll(mColorerRegex, text, start, length, (s, e) => callback(s, e));
        }
    }
}
