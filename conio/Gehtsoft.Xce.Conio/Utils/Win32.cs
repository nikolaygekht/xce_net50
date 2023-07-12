using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

#pragma warning disable S101 // Types should be named in PascalCase

namespace Gehtsoft.Xce.Conio
{
    internal static class Win32
    {
        public const int STD_OUTPUT_HANDLE = -11;
        public const int STD_INPUT_HANDLE = -10;
        public const int STD_ERROR_HANDLE = -12;

        [StructLayout(LayoutKind.Sequential)]
        public struct AnnotationHeader
        {
            public int StructSize;
            public int BufferSize;
            public int Locked;
            public uint FlushCounter;
        }

        [BitStruct(SizeOf = 32)]
        public class AnnotationInfo
        {
            [BitField(32)]              //byte 0-3
            public int bk_color;
            [BitField(24)]              //byte 4-6
            public int fg_color;
            [BitField(1)]               //byte 7 (bit 0)
            public int bk_valid;
            [BitField(1)]               //byte 7 (bit 1)
            public int fg_valid;
            [BitField(32)]              //6 bit of byte 7 and then bytes 8, 9, 10 and 11
            public int unused1;
            [BitField(6)]               //6 bit of byte 7 and then bytes 8, 9, 10 and 11
            public int unused2;
            [BitField(16)]
            public int style;           //byte 12!!!
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct COORD
        {
            internal short X;
            internal short Y;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct SMALL_RECT
        {
            internal short Left;
            internal short Top;
            internal short Right;
            internal short Bottom;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct COLORREF
        {
            internal uint ColorDWORD;

            internal COLORREF(uint r, uint g, uint b)
            {
                ColorDWORD = r + (g << 8) + (b << 16);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_SCREEN_BUFFER_INFO_EX
        {
            public uint cbSize;
            public COORD dwSize;
            public COORD dwCursorPosition;
            public short wAttributes;
            public SMALL_RECT srWindow;
            public COORD dwMaximumWindowSize;

            public ushort wPopupAttributes;
            public bool bFullscreenSupported;

            internal COLORREF black;
            internal COLORREF darkBlue;
            internal COLORREF darkGreen;
            internal COLORREF darkCyan;
            internal COLORREF darkRed;
            internal COLORREF darkMagenta;
            internal COLORREF darkYellow;
            internal COLORREF gray;
            internal COLORREF darkGray;
            internal COLORREF blue;
            internal COLORREF green;
            internal COLORREF cyan;
            internal COLORREF red;
            internal COLORREF magenta;
            internal COLORREF yellow;
            internal COLORREF white;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CHAR_INFO
        {
            public ushort UnicodeChar;
            public ushort Attributes;
        }

        public const int KEY_EVENT = 0x1;
        public const int MOUSE_EVENT = 0x2;
        public const int MENU_EVENT = 0x8;
        public const int FOCUS_EVENT = 0x10;
        public const int BUFFER_SIZE_EVENT = 0x4;

        public const int LEFT_ALT_PRESSED = 0x0002;
        public const int LEFT_CTRL_PRESSED = 0x0008;
        public const int RIGHT_ALT_PRESSED = 0x0001;
        public const int RIGHT_CTRL_PRESSED = 0x0004;
        public const int SHIFT_PRESSED = 0x0010;
        public const int ENHANCED_KEY = 0x0100;
        public const int CAPSLOCK_ON = 0x0080;
        public const int SCROLLLOCK_ON = 0x0040;

        [DllImport("USER32.dll")]
        internal static extern ushort GetKeyState(int nVirtKey);

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUT_RECORD
        {
            [FieldOffset(0)]
            public ushort EventType;
            [FieldOffset(4)]
            public KEY_EVENT_RECORD KeyEvent;
            [FieldOffset(4)]
            public MOUSE_EVENT_RECORD MouseEvent;
            [FieldOffset(4)]
            public WINDOW_BUFFER_SIZE_RECORD WindowBufferSizeEvent;
            [FieldOffset(4)]
            public MENU_EVENT_RECORD MenuEvent;
            [FieldOffset(4)]
            public FOCUS_EVENT_RECORD FocusEvent;
        };

        [StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
        public struct KEY_EVENT_RECORD
        {
            [FieldOffset(0), MarshalAs(UnmanagedType.Bool)]
            public bool bKeyDown;
            [FieldOffset(4), MarshalAs(UnmanagedType.U2)]
            public ushort wRepeatCount;
            [FieldOffset(6), MarshalAs(UnmanagedType.U2)]
            public ushort wVirtualKeyCode;
            [FieldOffset(8), MarshalAs(UnmanagedType.U2)]
            public ushort wVirtualScanCode;
            [FieldOffset(10)]
            public char UnicodeChar;
            [FieldOffset(12), MarshalAs(UnmanagedType.U4)]
            public uint dwControlKeyState;
        }

        public const int DOUBLE_CLICK = 0x0002;
        public const int MOUSE_HWHEELED = 0x0008;
        public const int MOUSE_MOVED = 0x0001;
        public const int MOUSE_WHEELED = 0x0004;
        public const int FROM_LEFT_1ST_BUTTON_PRESSED = 0x0001;
        public const int FROM_LEFT_2ND_BUTTON_PRESSED = 0x0004;
        public const int FROM_LEFT_3RD_BUTTON_PRESSED = 0x0008;
        public const int FROM_LEFT_4TH_BUTTON_PRESSED = 0x0010;
        public const int RIGHTMOST_BUTTON_PRESSED = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSE_EVENT_RECORD
        {
            public COORD dwMousePosition;
            public uint dwButtonState;
            public uint dwControlKeyState;
            public uint dwEventFlags;
        }

        public struct WINDOW_BUFFER_SIZE_RECORD
        {
            public COORD dwSize;

            public WINDOW_BUFFER_SIZE_RECORD(short x, short y)
            {
                dwSize = new COORD
                {
                    X = x,
                    Y = y
                };
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MENU_EVENT_RECORD
        {
            public uint dwCommandId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct FOCUS_EVENT_RECORD
        {
            public uint bSetFocus;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CONSOLE_CURSOR_INFO
        {
            public uint Size;
            public bool Visible;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetNumberOfConsoleInputEvents(IntPtr nStdHandle, ref uint ulCount);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleScreenBufferInfoEx(IntPtr hConsoleOutput, ref CONSOLE_SCREEN_BUFFER_INFO_EX csbe);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteConsoleSingleOutputW")]
        public static extern bool WriteConsoleSingleOutput(IntPtr hConsoleOutput, ref CHAR_INFO lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool WriteConsoleOutput(IntPtr hConsoleOutput, IntPtr lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpWriteRegion);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleOutputAttribute(IntPtr hConsoleOutput, ushort[] lpAttribute, uint nLength, COORD dwWriteCoord, out uint lpNumberOfAttrsWritten);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteConsoleOutputCharacterW")]
        public static extern bool WriteConsoleOutputCharacter(IntPtr hConsoleOutput, char[] lpChar, uint nLength, COORD dwWriteCoord, out uint lpNumberOfCharsWritten);

        [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "WriteConsoleOutputCharacterW")]
        public static extern bool WriteConsoleOutputSingleCharacter(IntPtr hConsoleOutput, ref char lpChar, uint nLength, COORD dwWriteCoord, out uint lpNumberOfCharsWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteConsoleSingleAttribute(IntPtr hConsoleOutput, ref ushort lpAttribute, uint nLength, COORD dwWriteCoord, out uint lpNumberOfAttrsWritten);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool ReadConsoleOutput(IntPtr hConsoleOutput, IntPtr lpBuffer, COORD dwBufferSize, COORD dwBufferCoord, ref SMALL_RECT lpReadRegion);

        [DllImport("kernel32.dll", EntryPoint = "ReadConsoleInputW", CharSet = CharSet.Unicode)]
        public static extern bool ReadConsoleSingleInput(IntPtr hConsoleInput, ref INPUT_RECORD lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

        [DllImport("kernel32.dll", EntryPoint = "PeekConsoleInputW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool PeekConsoleSingleInput(IntPtr hConsoleInput, ref INPUT_RECORD lpBuffer, uint nLength, out uint lpNumberOfEventsRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCursorPosition(IntPtr hConsoleOutput, COORD dwCursorPosition);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetConsoleCursorInfo(IntPtr hConsoleOutput, ref CONSOLE_CURSOR_INFO lpConsoleCursorInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetConsoleCursorInfo(IntPtr hConsoleOutput, ref CONSOLE_CURSOR_INFO lpConsoleCursorInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetConsoleWindow();

        [DllImport("user32.dll")]
        internal static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool OpenClipboard(uint handle);

        [DllImport("user32.dll")]
        internal static extern bool EmptyClipboard();

        internal const uint CF_TEXT = 1;
        internal const uint CF_UNICODETEXT = 13;

        [DllImport("user32.dll")]
        internal static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalUnlock(IntPtr hMem);

        internal const uint ENABLE_PROCESSED_INPUT = 0x0001;
        internal const uint ENABLE_LINE_INPUT = 0x0002;
        internal const uint ENABLE_ECHO_INPUT = 0x0004;
        internal const uint ENABLE_WINDOW_INPUT = 0x0008;
        internal const uint ENABLE_MOUSE_INPUT = 0x0010;
        internal const uint ENABLE_INSERT_MODE = 0x0020;
        internal const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
        internal const uint ENABLE_EXTENDED_FLAGS = 0x0080;
        internal const uint ENABLE_AUTO_POSITION = 0x0100;

        internal const uint ENABLE_PROCESSED_OUTPUT = 0x0001;
        internal const uint ENABLE_WRAP_AT_EOL_OUTPUT = 0x0002;
        internal const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;
        internal const uint DISABLE_NEWLINE_AUTO_RETURN = 0x0008;
        internal const uint ENABLE_LVB_GRID_WORLDWIDE = 0x0010;

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GlobalAlloc(uint uFlags, uint dwBytes);

        internal const uint GMEM_FIXED = 0x0000;
        internal const uint GMEM_MOVEABLE = 0x0002;
        internal const uint GMEM_ZEROINIT = 0x0040;
        internal const uint GMEM_MODIFY = 0x0080;
        internal const uint GMEM_DDESHARE = 0x2000;

        [DllImport("kernel32.dll", EntryPoint = "lstrlenA")]
        internal static extern int strlen(IntPtr lpstring);

        [DllImport("kernel32.dll", EntryPoint = "lstrlenW")]
        internal static extern int wcslen(IntPtr lpstring);

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]

        internal static extern void memcpy(IntPtr dest, IntPtr src, uint count);

        [DllImport("kernel32.dll", EntryPoint = "GetConsoleKeyboardLayoutNameA")]
        internal static extern IntPtr GetConsoleKeyboardLayoutName();

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern Int32 WaitForSingleObject(IntPtr Handle, Int32 Wait);

        [Flags]
        internal enum FileMapAccess : uint
        {
            None = 0,
            FileMapCopy = 0x0001,
            FileMapWrite = 0x0002,
            FileMapRead = 0x0004,
            FileMapReserved1 = 0x0008,
            FileMapReserved2 = 0x0010,

            FileMapAllAccess = 0x001f,
            fileMapExecute = 0x0020,
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenFileMapping(FileMapAccess DesiredAccess, bool bInheritHandle, string lpName);

        [DllImport("kernel32.dll")]
        internal static extern bool FlushViewOfFile(IntPtr lpBaseAddress, Int32 dwNumberOfBytesToFlush);

        [DllImport("kernel32")]
        internal static extern bool UnmapViewOfFile(IntPtr lpBaseAddress);

        [DllImport("Kernel32", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, FileMapAccess dwDesiredAccess, Int32 dwFileOffsetHigh, Int32 dwFileOffsetLow, Int32 dwNumberOfBytesToMap);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern IntPtr SetCursor(IntPtr handle);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        public const int IDC_ARROW = 32512;

        [DllImport("user32.dll")]
        public static extern uint MsgWaitForMultipleObjectsEx(uint nCount, IntPtr[] pHandles,
                    int dwMilliseconds, uint dwWakeMask, uint dwFlags);

        public const uint QS_ALLEVENTS = 1215;

        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point p;
        }

        [SuppressUnmanagedCodeSecurity]
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool PeekMessage(out NativeMessage message, uint handle, uint filterMin, uint filterMax, uint flags);
    }
}
