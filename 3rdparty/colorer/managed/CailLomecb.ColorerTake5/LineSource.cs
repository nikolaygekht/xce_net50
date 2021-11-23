using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CailLomecb.ColorerTake5
{
    /// <summary>
    /// The interface to the line source provider
    /// </summary>
    public interface IColorerLineSource
    {
        /// <summary>
        /// Returns the line content
        /// </summary>
        /// <param name="line">The index of the line</param>
        /// <returns></returns>
        string GetLine(int line);
    }

    internal sealed class ColorerLineAdapter : IDisposable
    {
        private string mLastLine = null;
        private IntPtr mNativeSource;
        private readonly IColorerLineSource mSource;
        private readonly NativeExports.LineSourceLineContent mLineSourceLineContent;

        internal IntPtr NativeSource => mNativeSource;

        internal ColorerLineAdapter(IColorerLineSource colorerLineSource)
        {
            mSource = colorerLineSource;
            mLineSourceLineContent = new NativeExports.LineSourceLineContent(this.SendLine);
            NativeExports.CreateColorerLineSourceAdapter(mLineSourceLineContent, out mNativeSource);
        }

        private void SendLine(int line)
        {
            string s = mSource.GetLine(line);
            if (!string.IsNullOrEmpty(s))
                NativeExports.ColorerLineSourceAdapterAcceptLine(mNativeSource, s, s.Length);
        }

        ~ColorerLineAdapter()
        {
            if (mNativeSource != IntPtr.Zero)
                NativeExports.DeleteColorerLineSourceAdapter(mNativeSource);
            mNativeSource = IntPtr.Zero;
        }

        public void Dispose()
        {
            if (mNativeSource != IntPtr.Zero)
                NativeExports.DeleteColorerLineSourceAdapter(mNativeSource);
            mNativeSource = IntPtr.Zero;
            GC.SuppressFinalize(this);
        }

        public IColorerLineSource LineSource => mSource;
    }
}
