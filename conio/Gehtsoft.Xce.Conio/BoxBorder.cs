using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio
{
    public class BoxBorder
    {
        public char TopLeft { get; }
        public char Top { get; }
        public char TopRight { get; }
        public char Left { get; }
        public char Right { get; }
        public char BottomLeft { get; }
        public char Bottom { get; }
        public char BottomRight { get; }

        public BoxBorder(char topLeft, char top, char topRight, char left, char right, char bottomLeft, char bottom, char bottomRight)  //NOSONAR
        {
            TopLeft = topLeft;
            Top = top;
            TopRight = topRight;
            Left = left;
            Right = right;
            BottomLeft = bottomLeft;
            Bottom = bottom;
            BottomRight = bottomRight;
        }

        public static BoxBorder Single { get; } = new BoxBorder('\u250c', '\u2500', '\u2510', '\u2502', '\u2502', '\u2514', '\u2500', '\u2518');
        public static BoxBorder Double { get; } = new BoxBorder('\u2554', '\u2550', '\u2557', '\u2551', '\u2551', '\u255a', '\u2550', '\u255d');
    }
}
