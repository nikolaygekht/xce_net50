namespace Gehtsoft.Xce.Conio.Win
{
    internal static class ControlSpaceModeProcessor
    {
        public static bool ProcessCtrlSpaceMode(Window window, ScanCode scanCode, bool shift, bool ctrl, bool alt)
        {
            if (!window.WindowManager.WindowKeyboardShortcutsEnabled)
                return false;

            if (scanCode == ScanCode.SPACE && ctrl && !alt && !shift)
            {
                window.CtrlSpaceMode = !window.CtrlSpaceMode;
                return true;
            }

            if (window.CtrlSpaceMode)
                return ProcessCtrlSpaceMode_InCtrlSpaceMode(window, scanCode, shift, ctrl, alt);

            return false;
        }

        private static bool ProcessCtrlSpaceMode_InCtrlSpaceMode(Window window, ScanCode scanCode, bool shift, bool ctrl, bool alt)
        {
            if (scanCode == ScanCode.N && !ctrl && !alt && !shift)
            {
                window.WindowManager.FocusNextWindow();
                window.CtrlSpaceMode = false;
                return true;
            }

            if (scanCode == ScanCode.C && !ctrl && !alt && !shift)
            {
                var window1 = window.WindowBorderContainer;
                (window1 ?? window).Close();
                return true;
            }

            if (scanCode == ScanCode.ESCAPE && !ctrl && !alt && !shift)
            {
                window.CtrlSpaceMode = false;
                return true;
            }

            if (IsArrow(scanCode) && !shift && !alt)
            {
                ProcessCtrlSpaceMode_InCtrlSpaceMode_MoveOrSize(window, scanCode, ctrl);
                return true;
            }

            return false;
        }

        private static bool IsArrow(ScanCode scanCode) => scanCode == ScanCode.UP || scanCode == ScanCode.DOWN || scanCode == ScanCode.LEFT || scanCode == ScanCode.RIGHT;

        private static void ProcessCtrlSpaceMode_InCtrlSpaceMode_MoveOrSize(Window window, ScanCode scanCode, bool ctrl)
        {
            var window1 = window.WindowBorderContainer;

            if (ctrl)
                ProcessAltSpaceMode_InAltSpaceMode_Size(window1, scanCode);
            else
                ProcessAltSpaceMode_InAltSpaceMode_Move(window1, scanCode);
        }

        private static void ProcessAltSpaceMode_InAltSpaceMode_Move(Window window, ScanCode scanCode)
        {
            if (scanCode == ScanCode.UP && window.Row > 0)
                window.Move(window.Row - 1, window.Column, window.Height, window.Width);
            else if (scanCode == ScanCode.DOWN && window.Row + window.Height < (window.Parent == null ? window.WindowManager.ScreenHeight : window.Parent.Height))
                window.Move(window.Row + 1, window.Column, window.Height, window.Width);
            else if (scanCode == ScanCode.LEFT && window.Column > 0)
                window.Move(window.Row, window.Column - 1, window.Height, window.Width);
            else if (scanCode == ScanCode.RIGHT && window.Column + window.Width < (window.Parent == null ? window.WindowManager.ScreenWidth : window.Parent.Width))
                window.Move(window.Row, window.Column + 1, window.Height, window.Width);
        }

        private static void ProcessAltSpaceMode_InAltSpaceMode_Size(Window window, ScanCode scanCode)
        {
            if (scanCode == ScanCode.UP && window.Height > 3)
                window.Move(window.Row, window.Column, window.Height - 1, window.Width);
            else if (scanCode == ScanCode.DOWN && window.Row + window.Height < window.WindowManager.ScreenHeight)
                window.Move(window.Row, window.Column, window.Height + 1, window.Width);
            else if (scanCode == ScanCode.LEFT && window.Width > 10)
                window.Move(window.Row, window.Column, window.Height, window.Width - 1);
            else if (scanCode == ScanCode.RIGHT && window.Column + window.Width < window.WindowManager.ScreenWidth)
                window.Move(window.Row, window.Column, window.Height, window.Width + 1);
        }
    }
}
