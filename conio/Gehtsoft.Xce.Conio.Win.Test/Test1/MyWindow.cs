using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test.Test1
{
    class MyWindow : WindowBorderContainer
    {
        Window mCreator;
        MyClientArea mClient;

        public MyWindow(string title, Window creator) : base(title, BoxBorder.Double, Program.CurrentSheme.WindowBackground, true, true)
        {
            mCreator = creator;
            mClient = new MyClientArea(Program.CurrentSheme.WindowBackground);
            AttachClientArea(mClient);
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
