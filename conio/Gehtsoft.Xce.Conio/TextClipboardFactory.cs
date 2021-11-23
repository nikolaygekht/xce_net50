using System;

namespace Gehtsoft.Xce.Conio
{
    public static class TextClipboardFactory
    {
        public static ITextClipboard Clipboard { get; private set; }

        static TextClipboardFactory()
        {
            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.Unix || os.Platform == PlatformID.MacOSX || os.Platform == PlatformID.Xbox)
                Clipboard = new NetTextClipboard();
            else if (os.Platform == PlatformID.Win32NT)
                Clipboard = new Win32TextClipboard();
            throw new InvalidOperationException($"Unknown platform mode {os.Platform}");

        }
    }
}
