using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Text;
using Gehtsoft.Xce.Conio.Clipboard;
using Gehtsoft.Xce.Conio.Drawing;

namespace Gehtsoft.Xce.Conio.Win
{
    public class DialogItemListBoxString
    {
        private string mLabel;
        private object mUserData;

        public virtual string Label
        {
            get
            {
                return mLabel;
            }
            set
            {
                mLabel = value ?? throw new ArgumentNullException(nameof(value));
            }
        }

        public virtual object UserData
        {
            get
            {
                return mUserData;
            }
            set
            {
                mUserData = value;
            }
        }

        public DialogItemListBoxString(string label, object userData)
        {
            mLabel = label;
            mUserData = userData;
        }
    };

    public class DialogItemListBox : DialogItem, IEnumerable<DialogItemListBoxString>
    {
        private readonly List<DialogItemListBoxString> mItems = new List<DialogItemListBoxString>();
        private int mCurSel = -1;
        private int mOffset = 0;
        private bool mInFocus = false;
        private bool mEnabled;

        public int Count => mItems.Count;

        public DialogItemListBoxString this[int index]
        {
            get
            {
                return mItems[index];
            }
        }

        public int CurSel
        {
            get
            {
                return mCurSel;
            }
            set
            {
                mCurSel = value;
                if (Exists)
                    Invalidate();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }

        IEnumerator<DialogItemListBoxString> IEnumerable<DialogItemListBoxString>.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }

        public override bool Enabled => mEnabled;

        public override bool IsInputElement => true;

        public void Enable(bool enable)
        {
            if (mEnabled != enable && Exists)
                Invalidate();
            mEnabled = enable;
        }

        public int AddItem(string item)
        {
            mItems.Add(new DialogItemListBoxString(item, null));
            if (Exists)
                Invalidate();
            return mItems.Count - 1;
        }

        public int AddItem(string item, object userData)
        {
            mItems.Add(new DialogItemListBoxString(item, userData));
            if (Exists)
                Invalidate();
            return mItems.Count - 1;
        }

        public int AddItem(DialogItemListBoxString item)
        {
            mItems.Add(item);
            if (Exists)
                Invalidate();
            return mItems.Count - 1;
        }

        public void RemoveItem(int index)
        {
            if (index >= 0 && index < mItems.Count)
            {
                mItems.RemoveAt(index);
                if (mCurSel >= mItems.Count)
                    CurSel = -1;
                else if (mCurSel > index)
                    mCurSel--;
                Invalidate();
            }
        }

        public void RemoveAllItems()
        {
            mItems.Clear();
            mCurSel = -1;
            mOffset = 0;
            Invalidate();
        }

        public void EnsureVisible(int index)
        {
            if (index >= 0 && index < mItems.Count)
            {
                if (mOffset > index)
                    mOffset = index;
                if (index >= mOffset + (Height - 2))
                    mOffset = index - (Height - 3);

                if (Exists)
                    Invalidate();
            }
        }

        public DialogItemListBox(int id, int row, int column, int height, int width) : base(id, row, column, height, width)
        {
            if (height < 3)
                throw new ArgumentException(Resources.ListBoxHeight3, nameof(height));

            mEnabled = true;
        }

        public override void OnSetFocus()
        {
            mInFocus = true;
            Invalidate();
        }

        public override void OnKillFocus()
        {
            mInFocus = false;
            Invalidate();
        }

        public override void OnPaint(Canvas canvas)
        {
            CanvasColor color;
            if (Enabled)
                color = Dialog.Colors.DialogItemListBoxColor;
            else
                color = Dialog.Colors.DialogItemListBoxColorDisabled;
            canvas.Box(0, 0, Height, Width, BoxBorder.Single, color, ' ');

            if (mItems.Count > Height - 2)
            {
                canvas.Write(0, Width - 1, '\u25b2');
                canvas.Write(Height - 1, Width - 1, '\u25bc');

                canvas.Fill(1, Width - 1, Height - 2, 1, '\u2591');
                canvas.Write(1 + OffsetToThumb(), Width - 1, '\u2592');
            }

            for (int i = mOffset; i < mItems.Count; i++)
            {
                if (i < 0)
                    continue;
                int row = i - mOffset + 1;
                if (row >= Height - 1)
                    break;
                string text = mItems[i].Label;

                canvas.Write(row, 1, text, 0, Width - 2);

                if (Enabled && i == mCurSel)
                {
                    if (mInFocus)
                    {
                        Manager.SetCaretPos(this, row, 1);
                        color = Dialog.Colors.DialogItemListBoxSelectionFocused;
                    }
                    else
                        color = Dialog.Colors.DialogItemListBoxSelection;
                    canvas.Fill(row, 1, 1, Width - 2, color);
                }
            }
        }

        private int ThumbToOffset(int thumb)
        {
            if (thumb == 0)
                return 0;
            else if (thumb == Height - 3)
                return mItems.Count - 1;

            double lines = mItems.Count;
            double scrollpos = Height - 2;
            double linesPerScroll = lines / scrollpos;
            return (int)Math.Floor(thumb * linesPerScroll);
        }

        private int OffsetToThumb()
        {
            double lines = mItems.Count;
            double scrollpos = Height - 2;
            double linesPerScroll = lines / scrollpos;
            double scroll = (mCurSel >= 0 ? mCurSel : mOffset) / linesPerScroll;
            return (int)Math.Floor(scroll);
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (Dialog.PretranslateOnKeyPressed(scanCode, character, shift, ctrl, alt))
                return;

            bool noModifiers = !ctrl && !alt && !shift;

            if (noModifiers)
            {
                OnKeyPressed_ItemNavigationByKey(scanCode);
            }
            else if (ctrl && !alt && !shift && scanCode == ScanCode.C)
            {
                if (mCurSel >= 0 && mCurSel < mItems.Count && mItems[mCurSel].Label != null)
                    TextClipboardFactory.Clipboard.SetText(mItems[mCurSel].Label);
                return;
            }

            if (!ctrl && !alt && (character >= ' '))
                OnKeyPressed_SeekForItemByChar(character);
        }

        private void OnKeyPressed_ItemNavigationByKey(ScanCode scanCode)
        {
            switch (scanCode)
            {
                case ScanCode.UP:
                    if (mCurSel > 0)
                    {
                        mCurSel--;
                        Dialog.OnItemChanged(this);
                    }
                    EnsureVisible(mCurSel);
                    Invalidate();
                    break;
                case ScanCode.DOWN:
                    if (mCurSel < mItems.Count - 1)
                    {
                        mCurSel++;
                        Dialog.OnItemChanged(this);
                    }
                    EnsureVisible(mCurSel);
                    Invalidate();
                    break;
                case ScanCode.PRIOR:
                    mCurSel -= (Height - 2);
                    if (mCurSel < 0)
                        mCurSel = 0;
                    Dialog.OnItemChanged(this);
                    EnsureVisible(mCurSel);
                    Invalidate();
                    break;
                case ScanCode.NEXT:
                    mCurSel += (Height - 2);
                    if (mCurSel >= mItems.Count - 1)
                        mCurSel = mItems.Count - 1;
                    Dialog.OnItemChanged(this);
                    EnsureVisible(mCurSel);
                    Invalidate();
                    break;
                case ScanCode.HOME:
                    mCurSel = 0;
                    Dialog.OnItemChanged(this);
                    EnsureVisible(mCurSel);
                    Invalidate();
                    break;
                case ScanCode.END:
                    mCurSel = mItems.Count - 1;
                    Dialog.OnItemChanged(this);
                    EnsureVisible(mCurSel);
                    Invalidate();
                    break;
            }
        }

        private void OnKeyPressed_SeekForItemByChar(char character)
        {
            int item = mCurSel + 1;

            int stop;

            if (mCurSel < 1)
                stop = mItems.Count;
            else
                stop = mCurSel;

            if (item == mItems.Count)
                item = 0;

            while (item != stop)
            {
                if (mItems[item].Label.Length > 0 &&
                    Char.ToUpper(mItems[item].Label[0]) == Char.ToUpper(character))
                {
                    mCurSel = item;
                    EnsureVisible(item);
                    Invalidate();
                    Dialog.OnItemChanged(this);
                    break;
                }

                item++;
                if (item == stop)
                    break;
                if (item == mItems.Count)
                    item = 0;
            }
        }

        public override void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (!Enabled)
                return;

            bool wasInFocus = false;
            if (!mInFocus)
            {
                Manager.SetFocus(this);
                Invalidate();
            }
            else
                wasInFocus = true;

            if (row >= 1 && row < Height - 1 &&
                column >= 1 && column < Width - 1)
                OnMouseLButtonDown_ItemClick(row, wasInFocus);

            if (column == Width - 1)
                OnMouseLButtonDown_ScrollBarClick(row, column);
        }

        private void OnMouseLButtonDown_ItemClick(int row, bool wasInFocus)
        {
            int index = mOffset + row - 1;
            if (index >= 0 && index < mItems.Count && index != mCurSel)
            {
                mCurSel = index;
                Dialog.OnItemChanged(this);
                EnsureVisible(mCurSel);
                Invalidate();
            }
            else if (index >= 0 &&
                index == mCurSel &&
                wasInFocus &&
                Dialog.Exists)
            {
                Dialog.OnItemClick(this);
            }
        }

        private void OnMouseLButtonDown_ScrollBarClick(int row, int column)
        {
            if (row == 0)
                OnKeyPressed(ScanCode.UP, ' ', false, false, false);
            else if (row == Height - 1 && column == Width - 1)
                OnKeyPressed(ScanCode.DOWN, ' ', false, false, false);
            else
            {
                int pos = ThumbToOffset(row - 1);
                mCurSel = pos;
                if (mCurSel >= mItems.Count)
                    mCurSel = mItems.Count - 1;
                Dialog.OnItemChanged(this);
                EnsureVisible(mCurSel);
                Invalidate();
            }
        }

        public override void OnMouseWheelUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            base.OnMouseWheelUp(row, column, shift, ctrl, alt);
            OnKeyPressed(ScanCode.UP, ' ', false, false, false);
        }

        public override void OnMouseWheelDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            base.OnMouseWheelUp(row, column, shift, ctrl, alt);
            OnKeyPressed(ScanCode.DOWN, ' ', false, false, false);
        }

        public override void OnSizeChanged()
        {
            base.OnSizeChanged();
            Invalidate();
        }
    }
}
