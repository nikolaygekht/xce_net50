namespace Gehtsoft.Xce.Conio
{
    public interface IConsoleCursor
    {
        bool CursorVisible { get; set; }
        int CursorSize { get; set; }

        int CursorRow { get; set; }
        int CursorColumn { get; set; }

        void SetCursorPosition(int row, int column);
        void GetCursorPosition(out int row, out int column);
    }
}
