using Gehtsoft.Xce.Conio.Drawing;
using System.Text;

namespace Gehtsoft.Xce.Conio.Output
{
    internal class VtTrueColorConsoleOutput : VtConsoleOutput
    {
        protected override StringBuilder ColorToEscapeCode(CanvasColor color)
        {
            if (color.RgbBg == -1 || color.RgbFg == -1)
                return base.ColorToEscapeCode(color);
            StringBuilder sb = new StringBuilder();

            int fg = color.RgbFg;
            int bg = color.RgbBg;

            int fgr, fgb, fgg;
            int bgr, bgb, bgg;

            fgr = fg & 0xff;
            fgb = (fg >> 8) & 0xff;
            fgg = (fg >> 16) & 0xff;
            bgr = bg & 0xff;
            bgb = (bg >> 8) & 0xff;
            bgg = (bg >> 16) & 0xff;

            sb.Append(string.Format("\x1b[38;2;{0};{1};{2}m\x1b[48;2;{3};{4};{5}m", fgr, fgb, fgg, bgr, bgb, bgg));

            return sb;
        }
    }
}
