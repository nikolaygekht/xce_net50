using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    internal static class StringUtil
    {
        internal static int ProcessHotKey(ref string label)
        {
            int hkposition = -1;
            StringBuilder newLabel = new StringBuilder();
            bool prevAmp = false;

            foreach (char c in label)
            {
                if (c == '&')
                {
                    if (prevAmp)
                    {
                        newLabel.Append(c);
                        prevAmp = false;
                    }
                    else
                    {
                        prevAmp = true;
                    }
                }
                else
                {
                    if (prevAmp)
                    {
                        if (hkposition < 0)
                            hkposition = newLabel.Length;
                        prevAmp = false;
                    }
                    newLabel.Append(c);
                }
            }
            label = newLabel.ToString();
            return hkposition;
        }
    }
}
