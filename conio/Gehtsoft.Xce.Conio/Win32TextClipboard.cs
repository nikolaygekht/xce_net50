using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Gehtsoft.Xce.Conio
{
    class Win32TextClipboard : ITextClipboard
    {
        public bool IsTextAvailable(TextClipboardFormat format)
        {
            if (!Win32.OpenClipboard())
                return false;
            bool rc =  Win32.IsClipboardFormatAvailable((uint)(int)format);
            Win32.CloseClipboard();
            return rc;
        }

        public string GetText(TextClipboardFormat format)
        {
            if (!Win32.OpenClipboard())
                return null;

            IntPtr hdata = Win32.GetClipboardData((uint)(int)format);
            if (hdata == IntPtr.Zero)
            {
                Win32.CloseClipboard();
                return null;
            }

            IntPtr ptr = Win32.GlobalLock(hdata);
            string rc;
            if (format == TextClipboardFormat.UnicodeText)
                rc = Marshal.PtrToStringUni(ptr);
            else
                rc = Marshal.PtrToStringAnsi(ptr);
            Win32.GlobalUnlock(hdata);
            Win32.CloseClipboard();
            return rc;
        }

        public string GetText()
        {
            if (IsTextAvailable(TextClipboardFormat.Text))
                return GetText(TextClipboardFormat.Text);
            else if (IsTextAvailable(TextClipboardFormat.UnicodeText))
                return GetText(TextClipboardFormat.UnicodeText);
            return null;
        }

        public void SetText(string text, TextClipboardFormat format = TextClipboardFormat.UnicodeText)
        {
            if (!Win32.OpenClipboard())
                return ;

            Win32.EmptyClipboard();

            IntPtr ptr;
            int l;
            if (format == TextClipboardFormat.UnicodeText)
            {
                ptr = Marshal.StringToCoTaskMemUni(text);
                l = (Win32.wcslen(ptr) + 1) * 2;
            }
            else
            {
                ptr = Marshal.StringToCoTaskMemAnsi(text);
                l = Win32.strlen(ptr) + 1;
            }

            IntPtr hdata = Win32.GlobalAlloc(Win32.GMEM_DDESHARE | Win32.GMEM_MOVEABLE, (uint)l);
            IntPtr dst = Win32.GlobalLock(hdata);
            Win32.memcpy(dst, ptr, (uint)l);
            Win32.GlobalUnlock(hdata);
            Marshal.FreeCoTaskMem(ptr);
            Win32.SetClipboardData((uint)format, hdata);
            Win32.CloseClipboard();
            return;
        }
    }
}
