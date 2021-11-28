using System;

namespace Gehtsoft.Xce.Conio
{
    internal class NetConsoleCursor : IConsoleCursor
    {
        private readonly bool mWindows;
        private readonly NetConsoleOutput mOutput;

        public bool CursorVisible
        {
            get => !mWindows || Console.CursorVisible;
            set
            {
                if (mWindows)
                    Console.CursorVisible = value;
            }
        }
        public int CursorSize
        {
            get => mWindows ? Console.CursorSize : -1;
            set
            {
                if (mWindows)
                    Console.CursorSize = value;
            }
        }
        public int CursorRow { get => Console.CursorTop - mOutput.WindowTop; set => Console.CursorTop = value + mOutput.WindowTop; }
        public int CursorColumn { get => Console.CursorLeft - mOutput.WindowLeft; set => Console.CursorLeft = value + mOutput.WindowLeft; }

        public NetConsoleCursor(NetConsoleOutput output)
        {
            mOutput = output;
            var os = Environment.OSVersion;
            mWindows = os.Platform == PlatformID.Win32NT;
        }

        public void GetCursorPosition(out int row, out int column)
        {
            row = CursorRow;
            column = CursorColumn;
        }

        public void SetCursorPosition(int row, int column)
        {
            Console.SetCursorPosition(column + mOutput.WindowLeft, row + mOutput.WindowTop);
        }
    }
}
