using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Test
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args[0] == "input1")
                    KbTest1();
                if (args[0] == "input2")
                    KbTest2();
                else if (args[0] == "debug1")
                    Debug1();
                else if (args[0] == "debug2w")
                    Debug2(true);
                else if (args[0] == "debug2c")
                    Debug2(false);
            }
            else
                Console.WriteLine("Params: input1 | input2 | output");
        }

        private static bool mRequestStop = false;

        private static void KbTest1()
        {
            ConsoleInputEventListener listener = new ConsoleInputEventListener();
            listener.KeyPressed += InputTest_Listener_KeyPressed;
            listener.MouseMoved += Listener_MouseMoved;
            IConsoleInput input = ConioFactory.CreateInput(ConioInputMode.Win32);
            while (!mRequestStop)
                input.Read(listener, -1);
        }
        private static void KbTest2()
        {
            ConsoleInputEventListener listener = new ConsoleInputEventListener();
            listener.KeyPressed += InputTest_Listener_KeyPressed;
            listener.MouseMoved += Listener_MouseMoved;
            IConsoleInput input = ConioFactory.CreateInput(ConioInputMode.NetConsole);
            while (!mRequestStop)
                input.Read(listener, -1);
        }

        private static void Listener_MouseMoved(int row, int column, bool shift, bool ctrl, bool alt, bool lb, bool rb)
        {
            Console.WriteLine($"Mouse: {row} {column}");
        }

        private static bool mPrevQ = false;

        private static void InputTest_Listener_KeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            Console.WriteLine("{0} {1}", ScanCodeParser.KeyCodeToName(scanCode, shift, ctrl, alt), character < ' ' ? $"'\\x{(uint)character:x2}'" : $"'{character}'");
            if (scanCode == ScanCode.ESCAPE && !shift && !alt && !ctrl)
            {
                if (mPrevQ)
                    mRequestStop = true;
                else
                    mPrevQ = true;
            }
            else
                mPrevQ = false;
        }

        public static void Debug1()
        {
            IConsoleInput input = ConioFactory.CreateInput(ConioInputMode.Win32);
            IConsoleOutput output = ConioFactory.CreateOutput(ConioOutputMode.Win32);
            Canvas save = output.BufferToCanvas();
            Console.WriteLine("Buffer Size {0}x{1}", output.BufferRows, output.BufferColumns);
            Console.WriteLine("Visible Size {0}x{1}", output.VisibleRows, output.VisibleColumns);
            Console.WriteLine("True color: {0}", output.SupportsTrueColor);

            ConsoleInputEventListener listener = new ConsoleInputEventListener();
            listener.KeyPressed += Debug1_Listener_KeyPressed;

            while (!mRequestStop)
                input.Read(listener, -1);

            output.PaintCanvasToBuffer(save, 0, 0);
        }

        private static void Debug1_Listener_KeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (scanCode == ScanCode.ESCAPE && !shift && !alt && !ctrl)
                mRequestStop = true;
        }

        public static void Debug2(bool win)
        {
            IConsoleInput input = ConioFactory.CreateInput(win ? ConioInputMode.Win32 : ConioInputMode.NetConsole);
            IConsoleOutput output = ConioFactory.CreateOutput(win ? ConioOutputMode.Win32 : ConioOutputMode.NetConsole);

            Canvas c = new Canvas(output.VisibleRows, output.VisibleColumns);
            c.Fill(0, 0, c.Rows, c.Columns, ' ', new CanvasColor(0x03));
            for (int i = 1; i < 16; i++)
                c.Fill(i + 1, 0, 1, c.Columns, (char)('A' + i), new CanvasColor((ushort)i));
            for (int i = 1; i < 16; i++)
                c.Fill(i + 16, 0, 1, c.Columns, (char)('A' + i), new CanvasColor((ushort)(i << 4)));

            output.PaintCanvasToScreen(c, 0, 0);

            ConsoleInputEventListener listener = new ConsoleInputEventListener();
            listener.KeyPressed += Debug1_Listener_KeyPressed;

            while (!mRequestStop)
                input.Read(listener, -1);
        }
    }
}
