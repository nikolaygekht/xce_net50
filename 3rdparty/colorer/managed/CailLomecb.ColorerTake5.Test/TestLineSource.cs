using System;
using System.Collections.Generic;
using System.Threading;

namespace CailLomecb.ColorerTake5.Test
{
    public class TestLineSource : IColorerLineSourceArray
    {
        public List<char[]> mContent = new();

        public int LinesCount => mContent.Count;

        public void Add(string text)
        {
            mContent.Add(text.ToCharArray());
        }

        public void RemoveAt(int line)
        {
            mContent.RemoveAt(line);
        }

        public void Change(int line, string text)
        {
            while (line >= LinesCount)
                mContent.Add(Array.Empty<char>());

            mContent[line] = text.ToCharArray();
        }

        public bool GetLine(int line, out char[] target, out int length)
        {
            length = 0;
            target = null;
            if (line < 0 || line >= mContent.Count)
                return false;
            else
            {
                if ((mContent[line]?.Length ?? 0) < 1)
                    return false;

                length = mContent[line].Length;
                target = mContent[line];
                return true;
            }
        }
    }
}
