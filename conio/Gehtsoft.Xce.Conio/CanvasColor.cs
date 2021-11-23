using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio
{
    public class CanvasColor : IEquatable<CanvasColor>
    {
        private ushort mPalColor;
        private int mRGBFg;
        private int mRGBBg;
        private ConsoleStyle mStyle;

        [Flags]
        public enum ConsoleStyle : int
        {
            None = 0,
            Bold = 1,
            Italic = 2,
            Underline = 4,
            Strikeout = 8,
            Superscript = 16,
            Subscript = 32,
            Shadow = 64,
            SmallCaps = 128,
            AllCaps = 256,
        }

        public ushort PalColor => mPalColor;
        public int RgbFg => mRGBFg;
        public int RgbBg => mRGBBg;
        public ConsoleStyle Style => mStyle;

        public CanvasColor(ushort palColor) : this(palColor, -1, -1, ConsoleStyle.None)
        {
        }
        public CanvasColor(ushort palColor, int rgbFg, int rgbBg) : this(palColor, rgbFg, rgbBg, ConsoleStyle.None)
        {
        }
        public CanvasColor(ushort palColor, int rgbFg, int rgbBg, ConsoleStyle style)
        {
            mPalColor = palColor;
            mRGBFg = rgbFg;
            mRGBBg = rgbBg;
            mStyle = style;
        }

        public static int RGB(int r, int g, int b)
        {
            if (r < 0 || r > 255)
                throw new ArgumentOutOfRangeException(nameof(r));
            if (g < 0 || g > 255)
                throw new ArgumentOutOfRangeException(nameof(g));
            if (b < 0 || b > 255)
                throw new ArgumentOutOfRangeException(nameof(b));
            return r | (g << 8) | (b << 16);
        }

        public static bool IsValid(int rgbcolor)
        {
            return rgbcolor >= 0 && rgbcolor <= 0xffffff;
        }

        public bool Equals(CanvasColor c)
        {
            return c.PalColor == PalColor && c.RgbFg == RgbFg && c.RgbBg == RgbBg && c.Style == Style;
        }

        public override bool Equals(object obj)
        {
            if (obj is CanvasColor cc)
                return Equals(cc);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PalColor, RgbFg, RgbBg, Style);
        }

        public override string ToString()
        {
            if (IsValid(RgbFg) && IsValid(RgbBg))
                return $"[pal:{PalColor:x2}, fg:#{RgbFg:x6}, bg:#{RgbBg:x6}]";
            else
                return $"[pal:{PalColor:x2}]";
        }
    }
}
