using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test
{
    public static class Program
    {
        public static bool Quit { get; set; } = false;
        public static IColorScheme CurrentSheme { get; set; }

        private static bool ProcessCommandLineArgs(string[] args, out ConioInputMode input, out ConioOutputMode output)
        {
            ConioFactory.GetDefaultMode(out input, out output);

            if (args.Length == 0)
                return false;

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "win32":
                        input = ConioInputMode.Win32;
                        output = ConioOutputMode.Win32;
                        break;
                    case "console":
                        input = ConioInputMode.NetConsole;
                        output = ConioOutputMode.NetConsole;
                        break;
                    case "conemu":
                        input = ConioInputMode.Win32;
                        output = ConioOutputMode.ConEmu;
                        break;
                    case "vt":
                        input = ConioInputMode.Terminal;
                        output = ConioOutputMode.Terminal;
                        break;
                    case "vt-color":
                        input = ConioInputMode.Terminal;
                        output = ConioOutputMode.TrueColorTerminal;
                        break;
                    case "win32-input":
                        input = ConioInputMode.Win32;
                        break;
                    case "console-input":
                        input = ConioInputMode.NetConsole;
                        break;
                    case "vt-input":
                        input = ConioInputMode.Terminal;
                        break;
                    case "win32-output":
                        output = ConioOutputMode.Win32;
                        break;
                    case "console-output":
                        output = ConioOutputMode.NetConsole;
                        break;
                    case "conemu-output":
                        output = ConioOutputMode.ConEmu;
                        break;
                    case "vt-output":
                        output = ConioOutputMode.Terminal;
                        break;
                    case "vt-color-output":
                        output = ConioOutputMode.TrueColorTerminal;
                        break;
                    case "default":
                        ConioFactory.GetDefaultMode(out input, out output);
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        public static void Main(string[] args)
        {
            if (!ProcessCommandLineArgs(args, out var inputMode, out var outputMode))
            {
                Console.WriteLine("Usage: app.exe mode(s)");
                Console.WriteLine(" Where mode could be:");
                Console.WriteLine("    * default");
                Console.WriteLine("    * win32 win32-input win32-output ");
                Console.WriteLine("    * console console-input console-output");
                Console.WriteLine("    * conemu conemu-output");
                Console.WriteLine("    * vt vt-input vt-output");
                Console.WriteLine("    * vt-color vt-color-output");
                return;
            }

            IConsoleInput input = ConioFactory.CreateInput(inputMode);
            using IConsoleOutput output = ConioFactory.CreateOutput(outputMode);
            CurrentSheme = ColorScheme.White;

            output.CaptureOnStart();

            WindowManager manager = new WindowManager(output, input)
            {
                WindowKeyboardShortcutsEnabled = true
            };
            manager.Create(new MainWindow($"{output.Mode}.{output.SupportsTrueColor}"), null, 0, 0, output.VisibleRows, output.VisibleColumns);

            while (!Quit)
                manager.PumpMessage(50);

            output.ReleaseOnFinish();
        }
    }
}
