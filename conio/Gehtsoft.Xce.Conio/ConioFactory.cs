using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Gehtsoft.Xce.Conio
{
    public enum ConioMode
    {
        Win32,
        CompatibleConsole
    }

    public static class ConioFactory
    {
        public static bool EnableTrueColor { get; set; } = true;
        public static ConioMode DefaultMode
        {
            get
            {
                var os = Environment.OSVersion;
                if (os.Platform == PlatformID.Unix || os.Platform == PlatformID.MacOSX || os.Platform == PlatformID.Xbox)
                    return ConioMode.CompatibleConsole;
                else if (os.Platform == PlatformID.Win32NT)
                    return ConioMode.Win32;
                throw new InvalidOperationException($"Unknown platform mode {os.Platform}");
            }
        }

        public static IConsoleInput CreateInput(ConioMode mode)
        {
            if (mode == ConioMode.Win32)
                return new Win32ConsoleInput();
            else if (mode == ConioMode.CompatibleConsole)
                return new NetConsoleInput();
            else
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
        public static IConsoleOutput CreateOutput(ConioMode mode)
        {
            if (mode == ConioMode.Win32)
                return new Win32ConsoleOutput(EnableTrueColor);
            else if (mode == ConioMode.CompatibleConsole)
                return new NetConsoleOutput();
            else
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
}
