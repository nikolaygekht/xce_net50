using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test.Test1
{
    internal class MyModal1 : WindowBorderContainer
    {
        private readonly Window mCreator;

        public MyModal1(Window creator, CanvasColor windowColor) : base("Dialog", BoxBorder.Single, Program.CurrentSheme.WindowBackground, true, true)
        {
            mCreator = creator;
            this.AttachClientArea(new MyClientArea(windowColor));
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (scanCode == ScanCode.ESCAPE && !shift && !ctrl && !alt)
                this.Close();
        }

        public override void OnClose()
        {
            WindowManager.SetFocus(mCreator);
        }
    }
}
