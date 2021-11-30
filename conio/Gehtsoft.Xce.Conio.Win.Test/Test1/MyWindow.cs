using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test.Test1
{
    internal class MyWindow : WindowBorderContainer
    {
        private readonly Window mCreator;

        public MyWindow(string title, Window creator) : base(title, BoxBorder.Double, Program.CurrentSheme.WindowBackground, true, true)
        {
            mCreator = creator;
            AttachClientArea(new MyClientArea(Program.CurrentSheme.WindowBackground));
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Show(true);
        }

        public override void OnClose()
        {
            WindowManager.SetFocus(mCreator);
        }
    }
}
