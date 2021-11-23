using System.Collections.Generic;

namespace CailLomecb.ColorerTake5.Test
{
    public class TestLineSource : IColorerLineSource
    {
        public List<string> mContent = new List<string>();

        public int LinesCount => mContent.Count;

        public void Add(string text)
        {
            mContent.Add(text);
        }

        public void RemoveAt(int line)
        {
            mContent.RemoveAt(line);
        }

        public void Change(int line, string text)
        {
            while (line >= LinesCount)
                mContent.Add("");
            mContent[line] = text;
        }

        public string GetLine(int line)
        {
            if (line < 0 || line >= mContent.Count)
                return "";
            else
                return mContent[line] ?? "";
        }

        public int GetLineLength(int line)
        {
            return GetLine(line).Length;
        }
    }
}
