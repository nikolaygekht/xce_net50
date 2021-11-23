using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test.Test1
{
    class MyDialog1 : Dialog
    {
        private const int idLabel = 0x100;
        private const int idList = 0x101;
        private const int idAdd = 0x102;
        private const int idExit = 0x103;

        private DialogItemListBox mListBox;
        private DialogItemComboBox mCb;
        private int mLastID = 0;


        public MyDialog1() : base("test", Program.CurrentSheme, false, 12, 42)
        {
            AddItem(new DialogItemLabel("&List", idLabel, 0, 0));
            mListBox = new DialogItemListBox(idList, 1, 0, 6, 40);
            AddItem(mListBox);
            AddItem(new DialogItemButton("<&Add String>", idAdd, 8, 6));
            AddItem(new DialogItemButton("<&Exit>", idExit, 8, 21));
            AddItem(mCb = new DialogItemComboBox("", 0x1000, 9, 5, 30));
            mCb.AddItem("Default Item 1");
            mCb.AddItem("Default Item 2");
            mCb.AddItem("Default Item 3");
            mCb.AddItem("Default Item 4");
        }

        public override void OnItemClick(DialogItem item)
        {
            if (item.ID == idAdd)
            {
                mListBox.AddItem(string.Format("{0} - {1}", mLastID++, mCb.Text));
                if (mListBox.CurSel < 0)
                    mListBox.CurSel = 0;
            }
            else if (item.ID == idExit)
            {
                EndDialog(Dialog.DialogResultCancel);
            }
            else
                base.OnItemClick(item);
        }
    }
}
