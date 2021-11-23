using System;
using System.Text;

namespace Gehtsoft.Xce.Conio
{
    public static class ScanCodeParser
    {
        public static ScanCode NameToKeyCode(string name) => (ScanCode)Enum.Parse(typeof(ScanCode), name);

        public static ScanCode NameToKeyCode(string name, out bool shift, out bool control, out bool alt)
        {
            shift = control = alt = false;
            while (true)
            {
                int idx = name.IndexOf('-');
                if (idx < 0)
                    return (ScanCode)Enum.Parse(typeof(ScanCode), name);
                else
                {
                    string modifier = name.Substring(0, idx);
                    name = name.Substring(idx + 1);
                    if (modifier == "shift")
                        shift = true;
                    else if (modifier == "ctrl")
                        control = true;
                    else if (modifier == "alt")
                        alt = true;
                }
            }


            
        }

        public static string KeyCodeToName(ScanCode scanCode) => scanCode.ToString();

        public static string KeyCodeToName(ScanCode scanCode, bool shift, bool control, bool alt)
        {
            StringBuilder sb = new StringBuilder();
            if (shift)
                sb.Append("shift-");
            if (control)
                sb.Append("ctrl-");
            if (alt)
                sb.Append("alt-");
            sb.Append(scanCode.ToString());
            return sb.ToString();
        }
    }
}

