using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test
{
    public static class Program
    {
        public static bool Quit { get; set; } = false;
        public static IColorScheme CurrentSheme { get; set; }

        private static bool ProcessCommandLineArgs(string[] args, out ConioMode input, out ConioMode output)
        {
            input = output = ConioFactory.DefaultMode;
            ConioFactory.EnableTrueColor = false;

            if (args.Length == 0)
                return false;

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "win32":
                        input = output = ConioMode.Win32;
                        break;
                    case "console":
                        input = output = ConioMode.CompatibleConsole;
                        break;
                    case "conemu":
                        input = output = ConioMode.Win32;
                        ConioFactory.EnableTrueColor = true;
                        break;
                    case "win32-input":
                        input = ConioMode.Win32;
                        break;
                    case "win32-output":
                        output = ConioMode.Win32;
                        break;
                    case "console-input":
                        input = ConioMode.CompatibleConsole;
                        break;
                    case "console-output":
                        output = ConioMode.CompatibleConsole;
                        break;
                    case "conemu-output":
                        output = ConioMode.Win32;
                        ConioFactory.EnableTrueColor = true;
                        break;
                    case "default":
                        input = output = ConioFactory.DefaultMode;
                        ConioFactory.EnableTrueColor = false;
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
                Console.WriteLine("    win32 win32-input win32-output conemu conemu-output console-input console-output default");
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
