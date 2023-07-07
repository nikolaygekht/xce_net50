using System;

namespace Gehtsoft.Xce.Conio.Clipboard
{
    public static class TextClipboardFactory
    {
        private readonly static ITextClipboard mClipboard = ClipboardFactory();

        public static ITextClipboard Clipboard => mClipboard ?? throw new InvalidOperationException($"Unknown platform mode {Environment.OSVersion.Platform}");

        private static ITextClipboard ClipboardFactory()
        {
            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.Unix || os.Platform == PlatformID.MacOSX || os.Platform == PlatformID.Xbox)
                return new NetTextClipboard();
            else if (os.Platform == PlatformID.Win32NT)
                return new Win32TextClipboard();
            else
                return null;
        }
    }
}
