using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public class DialogItemLabel : DialogItem
    {
        private string mTitle;
        private char mHotKey;
        private int mHotKeyPosition;

        public DialogItemLabel(string title, int id, int row, int column) : base(id, row, column)
        {
            mHotKeyPosition = StringUtil.ProcessHotKey(ref title);
            if (mHotKeyPosition >= 0)
                mHotKey = title[mHotKeyPosition];
            mTitle = title;
            SetDimesions(1, mTitle.Length);
        }

        public string Title
        {
            get
            {
                return mTitle;
            }
            set
            {
                string title = value ?? "";
                mHotKeyPosition = StringUtil.ProcessHotKey(ref title);
                if (mHotKeyPosition >= 0)
                    mHotKey = title[mHotKeyPosition];
                mTitle = title;
                SetDimesions(1, mTitle.Length);
                Invalidate();
            }
        }

        public override bool HasHotKey => mHotKeyPosition >= 0;

        public override char HotKey
        {
            get
            {
                if (!HasHotKey)
                    throw new InvalidOperationException();
                return mHotKey;
            }
        }

        public override bool IsInputElement => false;

        public override bool Enabled => true;

        public override void OnPaint(Canvas canvas)
        {
            canvas.Fill(0, 0, 1, Width, Dialog.Colors.DialogItemLabelColor);
            canvas.Write(0, 0, mTitle);
            if (mHotKeyPosition >= 0)
                canvas.Write(0, mHotKeyPosition, Dialog.Colors.DialogItemLabelHotKey);
        }
    }
}
