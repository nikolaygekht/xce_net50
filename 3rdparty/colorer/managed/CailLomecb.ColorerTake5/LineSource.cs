using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
                              // NG fields are kept to prevent GC of the value: it is used in native code

namespace CailLomecb.ColorerTake5
{
    internal sealed class ColorerLineAdapter : IDisposable
    {
        private IntPtr mBuffer;
        private IntPtr mNativeSource;
        private readonly NativeExports.LineSourceLineContent mLineSourceLineContent;

        internal IntPtr NativeSource => mNativeSource;

        public IColorerLineSource LineSource { get; }
        private readonly IColorerLineSourceArray mArraySource;
        private readonly IColorerLineSourceString mStringSource;

        internal ColorerLineAdapter(IColorerLineSource colorerLineSource)
        {
            mBuffer = Marshal.AllocCoTaskMem(8192);
            LineSource = colorerLineSource;
            mLineSourceLineContent = new NativeExports.LineSourceLineContent(this.SendLine);
            NativeExports.CreateColorerLineSourceAdapter(mLineSourceLineContent, out mNativeSource);
            mArraySource = colorerLineSource as IColorerLineSourceArray;
            mStringSource = colorerLineSource as IColorerLineSourceString;
        }

        private void SendLine(int line)
        {
            if (mArraySource != null)
            {
                if (!mArraySource.GetLine(line, out char[] content, out int length))
                    length = 0;
                if (length > 4096)
                    length = 4096;
                if (length != 0)
                    Marshal.Copy(content, 0, mBuffer, length);
                NativeExports.ColorerLineSourceAdapterAcceptLine(mNativeSource, mBuffer, length);
            }
            else if (mStringSource != null)
            {
                if (!mStringSource.GetLine(line, out string content, out int length))
                    length = 0;
                if (content == null)
                    content = "";
                IntPtr nativeString = Marshal.StringToCoTaskMemUni(content);
                NativeExports.ColorerLineSourceAdapterAcceptLine(mNativeSource, nativeString, length);
                Marshal.FreeCoTaskMem(nativeString);
            }
            else
                NativeExports.ColorerLineSourceAdapterAcceptLine(mNativeSource, mBuffer, 0);
        }

        ~ColorerLineAdapter()
        {
            PerformDispose();
        }

        public void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        private void PerformDispose()
        {
            if (mNativeSource != IntPtr.Zero)
                NativeExports.DeleteColorerLineSourceAdapter(mNativeSource);
            if (mBuffer != IntPtr.Zero)
                Marshal.FreeCoTaskMem(mBuffer);
            mNativeSource = IntPtr.Zero;
            mBuffer = IntPtr.Zero;
        }
    }
}
