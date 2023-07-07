using Gehtsoft.Xce.Conio.Input;
using Gehtsoft.Xce.Conio.Output;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace Gehtsoft.Xce.Conio
{

    public static class ConioFactory
    {       
        public static void GetDefaultMode(out ConioInputMode intputMode, out ConioOutputMode outputMode)
        {
                var os = Environment.OSVersion;
                if (os.Platform == PlatformID.Unix || os.Platform == PlatformID.MacOSX || os.Platform == PlatformID.Xbox)
                {
                    intputMode = ConioInputMode.NetConsole;
                    outputMode = ConioOutputMode.NetConsole;
                    return;
                }
                else if (os.Platform == PlatformID.Win32NT)
                {
                    intputMode = ConioInputMode.Win32;
                    outputMode = ConioOutputMode.Win32;
                    return;
                }
                throw new InvalidOperationException($"Unknown platform mode {os.Platform}");
        }

        public static IConsoleInput CreateInput(ConioInputMode mode)
        {
            if (mode == ConioInputMode.Win32)
                return new Win32ConsoleInput();
            else if (mode == ConioInputMode.NetConsole)
                return new NetConsoleInput();
            else
                throw new ArgumentOutOfRangeException(nameof(mode));
        }

        public static IConsoleOutput CreateOutput(ConioOutputMode mode)
        {
            if (mode == ConioOutputMode.Win32)
                return new Win32ConsoleOutput();
            else if (mode == ConioOutputMode.ConEmu)
                return new ConEmuConsoleOutput();
            else if (mode == ConioOutputMode.NetConsole)
                return new NetConsoleOutput();
            else
                throw new ArgumentOutOfRangeException(nameof(mode));
        }
    }
}
