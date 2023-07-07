using System;

namespace Gehtsoft.Xce.Conio.Drawing
{
    public sealed class CanvasCell : IEquatable<CanvasCell>
    {
        public char Character { get; set; }
        public CanvasColor Color { get; set; }

        public CanvasCell(char charater, CanvasColor color)
        {
            Character = charater;
            Color = color;
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
            return false;
        }

        public override string ToString()
        {
            if (Character < ' ')
                return $"[char:'\\x{Character:x}',color:{Color}]";
            else
                return $"[char:'{Character}',color:{Color}]";
        }

        public CanvasCell Clone() => new CanvasCell(Character, Color);
    }
}
