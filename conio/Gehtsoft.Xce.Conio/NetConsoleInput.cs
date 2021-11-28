using System;
using System.Threading;

namespace Gehtsoft.Xce.Conio
{
    internal class NetConsoleInput : IConsoleInput
    {
        public bool MouseSupported => false;

        public int CurrentLayout => 0;

        public ConioMode Mode => ConioMode.CompatibleConsole;

        public bool Read(IConsoleInputListener listener, int timeout)
        {
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    listener.OnKeyPressed((ScanCode)(int)k.Key, k.KeyChar, (k.Modifiers & ConsoleModifiers.Shift) != 0, (k.Modifiers & ConsoleModifiers.Control) != 0, (k.Modifiers & ConsoleModifiers.Alt) != 0);
                    return true;
                }
                if (timeout == 0)
                    return false;
                Thread.Sleep(10);
                if (timeout > 0 && timeout < 10)
                    timeout = 0;
                else if (timeout > 0)
                    timeout -= 10;
            }
        }
    }
}
