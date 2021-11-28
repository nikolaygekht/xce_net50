using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
                              // NG fields are kept to prevent GC of the value: it is used in native code

namespace CailLomecb.ColorerTake5
{
    internal sealed class ColorerLineAdapter : IDisposable
    {
        private IntPtr mNativeSource;
        private readonly NativeExports.LineSourceLineContent mLineSourceLineContent;

        internal IntPtr NativeSource => mNativeSource;

        internal ColorerLineAdapter(IColorerLineSource colorerLineSource)
        {
            LineSource = colorerLineSource;
            mLineSourceLineContent = new NativeExports.LineSourceLineContent(this.SendLine);
            NativeExports.CreateColorerLineSourceAdapter(mLineSourceLineContent, out mNativeSource);
        }

        private void SendLine(int line)
        {
            string s = LineSource.GetLine(line);
            if (!string.IsNullOrEmpty(s))
                NativeExports.ColorerLineSourceAdapterAcceptLine(mNativeSource, s, s.Length);
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
            mNativeSource = IntPtr.Zero;
        }

        public IColorerLineSource LineSource { get; }
    }
}
