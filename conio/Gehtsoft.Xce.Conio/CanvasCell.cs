using System;

namespace Gehtsoft.Xce.Conio
{
    public class CanvasCell : IEquatable<CanvasCell>
    {
        public CanvasColor mColor;

        public char Character { get; set; }
        public CanvasColor Color { get => mColor; set => mColor = value; }

        public CanvasCell(char charater, CanvasColor color)
        {
            Character = charater;
            mColor = color;
        }

        public bool Equals(CanvasCell cell)
        {
            return cell.Color.Equals(Color) && cell.Character == Character;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Color, Character);
        }

        public override bool Equals(object obj)
        {
            if (obj is CanvasCell cell)
                return Equals(cell);
            return base.Equals(obj);
        }

        public override string ToString()
        {
            if (Character < ' ')
                return $"[char:'\\x{Character:x}',color:{Color}]";
            else
                return $"[char:'{Character}',color:{Color}]";
        }
    }

    
}
