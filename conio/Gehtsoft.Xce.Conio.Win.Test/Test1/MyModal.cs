using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test.Test1
{
    class MyModal1 : WindowBorderContainer
    {
        Window mCreator;
        MyClientArea mChild;

        public MyModal1(Window creator, CanvasColor windowColor) : base("Dialog", BoxBorder.Single, Program.CurrentSheme.WindowBackground, true, true)
        {
            mCreator = creator;
            mChild = new MyClientArea(windowColor);
            this.AttachClientArea(mChild);
        }

        public override void OnClose()
        {
            WindowManager.SetFocus(mCreator);
        }
    }
}
