using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public class DialogItemCheckBox : DialogItem
    {
        private string mTitle;
        private bool mEnabled;
        private bool mInFocus;
        private char mHotKey;
        private int mHotKeyPosition;
        private bool mChecked;
        private char mCheckMark = '\u221a';

        public string Title
        {
            get
            {
                return mTitle;
            }
        }

        public override bool Enabled
        {
            get
            {
                return mEnabled;
            }
        }

        public override bool IsInputElement
        {
            get
            {
                return true;
            }
        }

        public override bool HasHotKey
        {
            get
            {
                return mHotKeyPosition >= 0;
            }
        }

        public override char HotKey
        {
            get
            {
                if (!HasHotKey)
                    throw new InvalidOperationException();
                return mHotKey;
            }
        }

        public bool Checked
        {
            get
            {
                return mChecked;
            }
            set
            {
                mChecked = value;
                if (Exists)
                    Invalidate();
            }

        }

        public char CheckMark
        {
            get
            {
                return mCheckMark;
            }
            set
            {
                mCheckMark = value;
            }
        }

        public DialogItemCheckBox(string title, int id, bool isChecked, int row, int column, int width)
            : base(id, row, column)
        {
            mHotKeyPosition = StringUtil.ProcessHotKey(ref title);
            if (mHotKeyPosition >= 0)
                mHotKey = title[mHotKeyPosition];
            mChecked = isChecked;
            mTitle = title;
            mEnabled = true;
        }

        public DialogItemCheckBox(string title, int id, bool isChecked, int row, int column)
            : base(id, row, column)
        {
            mHotKeyPosition = StringUtil.ProcessHotKey(ref title);
            if (mHotKeyPosition >= 0)
                mHotKey = title[mHotKeyPosition];
            mTitle = title;
            mChecked = isChecked;
            mEnabled = true;
            SetDimesions(1, title.Length + 4);
        }

        public void Enable(bool enable)
        {
            if (mEnabled != enable)
                Invalidate();
            mEnabled = enable;
        }

        public override void Click()
        {
            if (Enabled)
            {
                Checked = !Checked;
                Dialog.OnItemChanged(this);
            }
        }

        public override void OnCreate()
        {
            mInFocus = false;
        }

        public override void OnSetFocus()
        {
            mInFocus = true;
            Manager.ShowCaret(true);
            Manager.SetCaretPos(this, 0, 1);
            Dialog.OnItemActivated(this);
            Invalidate();
        }

        public override void OnKillFocus()
        {
            mInFocus = false;
            Manager.ShowCaret(false);
            Invalidate();
        }

        public override void OnPaint(Canvas canvas)
        {
            CanvasColor color;

            if (Enabled)
            {
                if (mInFocus)
                    color = Dialog.Colors.DialogItemCheckBoxColorFocused;
                else
                    color = Dialog.Colors.DialogItemCheckBoxColor;
            }
            else
                color = Dialog.Colors.DialogItemCheckBoxColorDisabled;

            canvas.Fill(0, 0, 1, Width, color);
            canvas.Write(0, 0, '[');
            if (mChecked)
                canvas.Write(0, 1, mCheckMark);
            else
                canvas.Write(0, 1, ' ');
            canvas.Write(0, 2, ']');
            canvas.Write(0, 3, ' ');
            canvas.Write(0, 4, mTitle);
            if (Enabled && HasHotKey)
                canvas.Write(0, 4 + mHotKeyPosition, mInFocus ? Dialog.Colors.DialogItemCheckBoxHotKeyFocused : Dialog.Colors.DialogItemCheckBoxHotKey);
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (!shift && !alt && !ctrl && (scanCode == ScanCode.SPACE))
                Click();
            else
                Dialog.PretranslateOnKeyPressed(scanCode, character, shift, ctrl, alt);
        }

        public override void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (Enabled)
            {
                Manager.SetFocus(this);
                Invalidate();
            }
        }

        public override void OnMouseLButtonUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (mInFocus && Enabled)
                Click();
        }

        public override void OnHotkeyActivated()
        {
            Click();
        }

    }
}
