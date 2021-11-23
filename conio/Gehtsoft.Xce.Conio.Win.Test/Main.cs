using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test
{
    public class Program
    {
        public static bool Quit { get; set; } = false;
        public static IColorScheme CurrentSheme { get; set; }

        public static void Main(string[] args)
        {
            ConioMode mode = ConioFactory.DefaultMode;
            if (args.Length > 0 && args[0] == "win32")
                mode = ConioMode.Win32;
            else if (args.Length > 0 && args[0] == "console")
                mode = ConioMode.CompatibleConsole;

            IConsoleInput input = ConioFactory.CreateInput(mode);
            IConsoleOutput output = ConioFactory.CreateOutput(mode);

            CurrentSheme = ColorScheme.White;

            output.CaptureOnStart();

            WindowManager manager = new WindowManager(false, output, input);
            manager.WindowKeyboardShortcutsEnabled = true;
            manager.Create(new MainWindow(), null, 0, 0, output.VisibleRows, output.VisibleColumns);
            
            while (!Quit)
                manager.PumpMessage(-1);

            output.ReleaseOnFinish();
        }
    }
}
