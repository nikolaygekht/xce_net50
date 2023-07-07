using System.Runtime.InteropServices;
using System.Text;

namespace Gehtsoft.Xce.Conio.Output
{
    internal class Win32ConsoleCursor : IConsoleCursor
    {
        private readonly Win32ConsoleOutput mOutput;

        private readonly static uint gCSBISize = (uint)Marshal.SizeOf<Win32.CONSOLE_SCREEN_BUFFER_INFO_EX>();

        public bool CursorVisible
        {
            get
            {
                Win32.CONSOLE_CURSOR_INFO cci = new Win32.CONSOLE_CURSOR_INFO();
                Win32.GetConsoleCursorInfo(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), ref cci);
                return cci.Visible;
            }
            set
            {
                Win32.CONSOLE_CURSOR_INFO cci = new Win32.CONSOLE_CURSOR_INFO();
                Win32.GetConsoleCursorInfo(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), ref cci);
                cci.Visible = value;
                Win32.SetConsoleCursorInfo(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), ref cci);
            }
        }

        public int CursorSize
        {
            get
            {
                Win32.CONSOLE_CURSOR_INFO cci = new Win32.CONSOLE_CURSOR_INFO();
                Win32.GetConsoleCursorInfo(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), ref cci);
                return (int)cci.Size;
            }
            set
            {
                Win32.CONSOLE_CURSOR_INFO cci = new Win32.CONSOLE_CURSOR_INFO();
                Win32.GetConsoleCursorInfo(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), ref cci);
                cci.Size = (uint)value;
                Win32.SetConsoleCursorInfo(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), ref cci);
            }
        }
        public int CursorRow
        {
            get
            {
                GetCursorPosition(out int row, out _);
                return row;
            }
            set
            {
                GetCursorPosition(out int _, out int column);
                SetCursorPosition(value, column);
            }
        }
        public int CursorColumn
        {
            get
            {
                GetCursorPosition(out int _, out int column);
                return column;
            }
            set
            {
                GetCursorPosition(out int row, out _);
                SetCursorPosition(row, value);
            }
        }

        public Win32ConsoleCursor(Win32ConsoleOutput output)
        {
            mOutput = output;
        }

        public void GetCursorPosition(out int row, out int column)
        {
            Win32.CONSOLE_SCREEN_BUFFER_INFO_EX sbi = new Win32.CONSOLE_SCREEN_BUFFER_INFO_EX
            {
                cbSize = gCSBISize
            };
            Win32.GetConsoleScreenBufferInfoEx(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), ref sbi);
            row = sbi.dwCursorPosition.Y - mOutput.mWindowTop;
            column = sbi.dwCursorPosition.X - mOutput.mWindowLeft;
        }

        public void SetCursorPosition(int row, int column)
        {
            Win32.COORD c = new Win32.COORD()
            {
                Y = (short)(row + mOutput.mWindowTop),
                X = (short)(column + mOutput.mWindowLeft)
            };
            Win32.SetConsoleCursorPosition(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), c);
        }
    }
}
