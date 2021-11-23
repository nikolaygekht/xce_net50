using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;

namespace Gehtsoft.Xce.Conio
{

    class Win32ConsoleInput : IConsoleInput
    {
        private bool mLeftButtonPressed, mRightButtonPressed;
        private int mMouseRow, mMouseColumn;

        public bool MouseSupported => true;

        public int CurrentLayout
        {
            get
            {
                IntPtr r = Win32.GetConsoleKeyboardLayoutName();
                if (r == IntPtr.Zero)
                    return 0;
                string s = Marshal.PtrToStringAnsi(r);
                if (!int.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rc))
                    return 0;
                return rc;
            }
        }

        public ConioMode Mode => ConioMode.Win32;

        public Win32ConsoleInput()
        {
            Win32.SetConsoleMode(Win32.GetStdHandle(Win32.STD_INPUT_HANDLE), Win32.ENABLE_MOUSE_INPUT);
            mLeftButtonPressed = mRightButtonPressed = false;
            mMouseColumn = mMouseRow = -1;
        }

        public bool Read(IConsoleInputListener listener, int timeout)
        {
            IntPtr input = Win32.GetStdHandle(Win32.STD_INPUT_HANDLE);
            
            if (Win32.WaitForSingleObject(input, timeout) != 0)
                return false;

            Win32.INPUT_RECORD ri = new Win32.INPUT_RECORD();
            uint ulEvents;

            Win32.PeekConsoleSingleInput(input, ref ri, 1, out ulEvents);

            switch (ri.EventType)
            {
                case Win32.KEY_EVENT:
                    {
                        bool shift = (ri.KeyEvent.dwControlKeyState & Win32.SHIFT_PRESSED) != 0;
                        bool alt = (ri.KeyEvent.dwControlKeyState & Win32.LEFT_ALT_PRESSED) != 0 || (ri.KeyEvent.dwControlKeyState & Win32.RIGHT_ALT_PRESSED) != 0;
                        bool ctrl = (ri.KeyEvent.dwControlKeyState & Win32.LEFT_CTRL_PRESSED) != 0 || (ri.KeyEvent.dwControlKeyState & Win32.RIGHT_CTRL_PRESSED) != 0;
                        int scanCode = 0;
                        char chr = '\0';
                        bool press = false;

                        if (ri.KeyEvent.bKeyDown && ri.KeyEvent.wRepeatCount == 1)
                        {
                            Win32.ReadConsoleSingleInput(input, ref ri, 1, out ulEvents);
                            scanCode = ri.KeyEvent.wVirtualKeyCode;
                            chr = ri.KeyEvent.UnicodeChar;
                            press = true;
                        }
                        else if (!ri.KeyEvent.bKeyDown)
                        {
                            Win32.ReadConsoleSingleInput(input, ref ri, 1, out ulEvents);
                            scanCode = ri.KeyEvent.wVirtualKeyCode;
                            chr = ri.KeyEvent.UnicodeChar;
                            press = false;
                        }
                        else
                        {
                            ulEvents = 0;
                            Win32.GetNumberOfConsoleInputEvents(input, ref ulEvents);
                            if (ulEvents > 0)
                                Win32.ReadConsoleSingleInput(input, ref ri, 1, out ulEvents);
                        }

                        if (scanCode != 0)
                        {
                            if (press)
                                listener.OnKeyPressed((ScanCode)scanCode, chr, shift, ctrl, alt);
                            else
                                listener.OnKeyReleased((ScanCode)scanCode, chr, shift, ctrl, alt);
                        }
                    }
                    break;
                case Win32.MOUSE_EVENT:
                    {
                        ulEvents = 0;
                        Win32.GetNumberOfConsoleInputEvents(input, ref ulEvents);
                        if (ulEvents > 0)
                            Win32.ReadConsoleSingleInput(input, ref ri, 1, out ulEvents);

                        bool shift = (ri.MouseEvent.dwControlKeyState & Win32.SHIFT_PRESSED) != 0;
                        bool alt = (ri.MouseEvent.dwControlKeyState & Win32.LEFT_ALT_PRESSED) != 0 || (ri.MouseEvent.dwControlKeyState & Win32.RIGHT_ALT_PRESSED) != 0;
                        bool ctrl = (ri.MouseEvent.dwControlKeyState & Win32.LEFT_CTRL_PRESSED) != 0 || (ri.MouseEvent.dwControlKeyState & Win32.RIGHT_CTRL_PRESSED) != 0;

                        switch (ri.MouseEvent.dwEventFlags)
                        {
                            case 0:
                            case Win32.DOUBLE_CLICK:
                            case Win32.MOUSE_MOVED:
                                {
                                    int row, column;
                                    bool left, right, rc;
                                    column = ri.MouseEvent.dwMousePosition.X;
                                    row = ri.MouseEvent.dwMousePosition.Y;
                                    left = (ri.MouseEvent.dwButtonState & Win32.FROM_LEFT_1ST_BUTTON_PRESSED) != 0;
                                    right = (ri.MouseEvent.dwButtonState & Win32.RIGHTMOST_BUTTON_PRESSED) != 0;
                                    rc = false;
                                    bool oldLeftButtonPressed = mLeftButtonPressed;
                                    bool oldRightButtonPressed = mRightButtonPressed;
                                    mLeftButtonPressed = left;
                                    mRightButtonPressed = right;

                                    if (left != oldLeftButtonPressed)
                                    {
                                        if (left)
                                            listener.OnMouseLButtonDown(row, column, shift, ctrl, alt);
                                        else
                                            listener.OnMouseLButtonUp(row, column, shift, ctrl, alt);
                                        rc = true;
                                    }

                                    if (right != oldRightButtonPressed)
                                    {
                                        if (right)
                                            listener.OnMouseRButtonDown(row, column, shift, ctrl, alt);
                                        else
                                            listener.OnMouseRButtonUp(row, column, shift, ctrl, alt);
                                        rc = true;
                                    }

                                    if (!rc && (row != mMouseRow || column != mMouseColumn))
                                    {
                                        listener.OnMouseMove(row, column, shift, ctrl, alt, left, right);
                                    }

                                    mMouseRow = row;
                                    mMouseColumn = column;
                                }
                                break;
                            case Win32.MOUSE_WHEELED:
                                {
                                    int row, column;
                                    column = ri.MouseEvent.dwMousePosition.X;
                                    row = ri.MouseEvent.dwMousePosition.Y;

                                    if ((ri.MouseEvent.dwButtonState & 0xFF000000) != 0)
                                        listener.OnMouseWheelDown(row, column, shift, ctrl, alt);
                                    else
                                        listener.OnMouseWheelUp(row, column, shift, ctrl, alt);
                                }
                                break;
                        }
                    }
                    return true;
                case Win32.FOCUS_EVENT:
                    {
                        ulEvents = 0;
                        Win32.GetNumberOfConsoleInputEvents(input, ref ulEvents);
                        if (ulEvents > 0)
                            Win32.ReadConsoleSingleInput(input, ref ri, 1, out ulEvents);
                        bool ctrl = (Win32.GetKeyState((int)ScanCode.CONTROL) & 0x8000) != 0;
                        bool shift = (Win32.GetKeyState((int)ScanCode.SHIFT) & 0x8000) != 0;
                        bool alt = (Win32.GetKeyState((int)ScanCode.MENU) & 0x8000) != 0;
                        listener.OnGetFocus(shift, ctrl, alt);
                    }
                    return true;
                case Win32.BUFFER_SIZE_EVENT:
                    listener.OnScreenBufferChanged(ri.WindowBufferSizeEvent.dwSize.Y, ri.WindowBufferSizeEvent.dwSize.X);
                    ulEvents = 0;
                    Win32.GetNumberOfConsoleInputEvents(input, ref ulEvents);
                    if (ulEvents > 0)
                        Win32.ReadConsoleSingleInput(input, ref ri, 1, out ulEvents);
                    return true;
                default:
                    ulEvents = 0;
                    Win32.GetNumberOfConsoleInputEvents(input, ref ulEvents);
                    if (ulEvents > 0)
                        Win32.ReadConsoleSingleInput(input, ref ri, 1, out ulEvents);
                    return true;
            }
            return true;
        }
    }
}
