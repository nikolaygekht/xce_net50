using Gehtsoft.Xce.Conio.Drawing;
using Gehtsoft.Xce.Conio.Win.Test.Test1;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test
{
    internal class MainWindow : Window
    {
        private readonly string mOutputMode;

        public MainWindow(string outputMode)
        {
            mOutputMode = outputMode;
        }

        public override void OnCreate()
        {
            Show(true);
            WindowManager.SetFocus(this);
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (scanCode == ScanCode.F9 && !shift && !ctrl && !alt)
            {
                this.WindowManager.ShowPopupMenu(MainMenuFactory.Menu, 0, 0);
                if (MainMenuFactory.Menu.CommandChosen == MainMenuFactory.ExitId)
                    Program.Quit = true;
                else if (MainMenuFactory.Menu.CommandChosen == MainMenuFactory.MessageBoxId)
                {
                    MessageBoxButton r = MessageBox.Show(WindowManager, Program.CurrentSheme, "This is the message to be displayed\r\nIn two lines", "MessageBox", MessageBoxButtonSet.YesNoCancel);
                    MessageBox.Show(WindowManager, Program.CurrentSheme, $"Selected Button is {r}", "Result", MessageBoxButtonSet.Ok);
                }
                else if (MainMenuFactory.Menu.CommandChosen == MainMenuFactory.TestFocusId)
                {
                    var w1 = new MyWindow("Window 1", this);
                    WindowManager.Create(w1, null, 1, 1, 5, 40);
                    var w2 = new MyWindow("Window 2", this);
                    WindowManager.Create(w2, null, 5, 5, 5, 10);
                    var w3 = new MyWindow("Window 3", this);
                    WindowManager.Create(w3, null, 10, 10, 5, 10);
                    WindowManager.SetFocus(w1);
                }
                else if (MainMenuFactory.Menu.CommandChosen == MainMenuFactory.TestFileDialogId)
                {
                    FileDialog dlg = new FileDialog("test", ".", "test.txt", Program.CurrentSheme, 10, 30);
                    if (dlg.DoModal(WindowManager) == Dialog.DialogResultOK)
                        MessageBox.Show(WindowManager, Program.CurrentSheme, dlg.File, "Chosen", MessageBoxButtonSet.Ok);
                }
                else if (MainMenuFactory.Menu.CommandChosen == MainMenuFactory.TestDialog1Id)
                {
                    MyDialog1 dlg = new MyDialog1();
                    dlg.DoModal(WindowManager);
                }
                else if (MainMenuFactory.Menu.CommandChosen == MainMenuFactory.TestDialog2Id)
                {
                    Dialog dlg = new Dialog("test", Program.CurrentSheme, false, 10, 28);
                    dlg.AddItem(new DialogItemCheckBox("check &1", 100, true, 0, 0));
                    dlg.AddItem(new DialogItemCheckBox("check &2", 101, false, 1, 0));
                    DialogItemCheckBox cb = new DialogItemCheckBox("check &3", 104, false, 2, 0);
                    cb.Enable(false);
                    dlg.AddItem(cb);
                    dlg.AddItem(new DialogItemLabel("&label 1", 102, 3, 0));
                    dlg.AddItem(new DialogItemSingleLineTextBox("123456", 105, 4, 0, 26));
                    dlg.AddItem(new DialogItemRadioBox("&A", 200, true, 5, 0, true));
                    dlg.AddItem(new DialogItemRadioBox("&B", 201, false, 5, 6, false));
                    dlg.AddItem(new DialogItemRadioBox("&C", 202, false, 5, 12, false));
                    dlg.AddItem(new DialogItemRadioBox("&D", 202, false, 6, 0, true));
                    dlg.AddItem(new DialogItemRadioBox("&E", 203, true, 6, 6, false));
                    dlg.AddItem(new DialogItemRadioBox("&F", 204, false, 6, 12, false));
                    dlg.AddItem(new DialogItemButton("b&utton 1", Dialog.DialogResultOK, 7, 0));
                    dlg.AddItem(new DialogItemButton("button 2", Dialog.DialogResultCancel, 7, 14));
                    dlg.DoModal(WindowManager);
                }
            }
            if (scanCode == ScanCode.ESCAPE && !shift && !ctrl && !alt)
                Program.Quit = true;
            base.OnKeyPressed(scanCode, character, shift, ctrl, alt);
        }

        public override void OnPaint(Canvas canvas)
        {
            canvas.Fill(0, 0, canvas.Rows, canvas.Columns, ' ', Program.CurrentSheme.WindowBackground);
            canvas.Write(0, 0, "Winlib example");
            canvas.Write(1, 0, "Press F9 for menu");
            canvas.Write(2, 0, "Press Esc for exit По русски");
            if (WindowManager.GetFocus() == this)
                canvas.Write(3, 0, "Focus!");
            canvas.Write(4, 0, $"True color {mOutputMode}");

            for (int i = 0; i < 16; i++)
            {
                int c = i * 16;
                canvas.Write(5, i, 'x', new CanvasColor((ushort)i, c, c << 8));
            }
        }

        public override void OnSetFocus()
        {
            Invalidate();
        }
        public override void OnKillFocus()
        {
            Invalidate();
        }
    }
}
