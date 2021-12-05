using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public class DialogItemRadioBox : DialogItem
    {
        private bool mEnabled;
        private bool mInFocus;
        private readonly char mHotKey;
        private readonly int mHotKeyPosition;
        private bool mChecked;

        public string Title { get; }

        public override bool Enabled => mEnabled;

        public override bool IsInputElement => true;

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

        public bool Checked
        {
            get => mChecked;
            set
            {
                if (value)
                    UncheckAllInGroup();

                mChecked = value;
                if (Exists)
                    Invalidate();
            }
        }

        private void FindGroup(out int from)
        {
            int thisIndex = -1;

            //find this
            for (int i = 0; i < Dialog.Items.Count && thisIndex == -1; i++)
                if (ReferenceEquals(Dialog.Items[i], this))
                    thisIndex = i;

            from = thisIndex;
            if (from == -1)
                return;

            while (from >= 0)
            {
                if (Dialog.Items[from] is not DialogItemRadioBox drbi)
                {
                    from++;
                    break;
                }

                if (drbi.GroupStart)
                    break;

                from--;
            }
        }

        private void UncheckAllInGroup()
        {
            FindGroup(out var from);

            if (from < 0)
                return;

            for (int i = from; i < Dialog.Items.Count; i++)
            {
                DialogItem item = Dialog.Items[i];
                if (item is not DialogItemRadioBox dirb)
                    break;

                if (i > from && dirb.GroupStart)
                    break;

                dirb.Checked = false;
            }
        }

        public char CheckMark { get; set; } = '\u2219';

        public bool GroupStart { get; set; }

        public DialogItemRadioBox(string title, int id, bool isChecked, int row, int column, int width, bool groupStart)
            : base(id, row, column)
        {
            mHotKeyPosition = StringUtil.ProcessHotKey(ref title);
            if (mHotKeyPosition >= 0)
                mHotKey = title[mHotKeyPosition];
            mChecked = isChecked;
            Title = title;
            mEnabled = true;
            GroupStart = groupStart;
            SetDimesions(1, width);
        }

        public DialogItemRadioBox(string title, int id, bool isChecked, int row, int column, bool groupStart)
            : base(id, row, column)
        {
            mHotKeyPosition = StringUtil.ProcessHotKey(ref title);
            if (mHotKeyPosition >= 0)
                mHotKey = title[mHotKeyPosition];
            Title = title;
            mChecked = isChecked;
            mEnabled = true;
            SetDimesions(1, title.Length + 4);
            GroupStart = groupStart;
        }

        public void Enable(bool enable)
        {
            if (mEnabled != enable)
                Invalidate();
            mEnabled = enable;
        }

        public override void Click()
        {
            if (Enabled && !Checked)
            {
                Checked = true;
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
            canvas.Write(0, 0, '(');
            if (mChecked)
                canvas.Write(0, 1, CheckMark);
            else
                canvas.Write(0, 1, ' ');
            canvas.Write(0, 2, ')');
            canvas.Write(0, 3, ' ');
            canvas.Write(0, 4, Title);
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
