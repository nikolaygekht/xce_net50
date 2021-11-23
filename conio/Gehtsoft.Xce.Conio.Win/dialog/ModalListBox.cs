using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public class ModalListBox : Window, IEnumerable<DialogItemListBoxString>
    {
        private List<DialogItemListBoxString> mItems = new List<DialogItemListBoxString>();
        private int mCurSel = -1;
        private int mOffset = 0;
        private int mRow, mColumn, mWidth, mHeight;
        private WindowManager mManager;
        private bool mOk;

        public int Count
        {
            get
            {
                return mItems.Count;
            }
        }

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
                {
                    if (mCurSel >= 0)
                        EnsureVisible(mCurSel);
                    Invalidate();
                }
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

        public int AddItem(string item)
        {
            mItems.Add(new DialogItemListBoxString(item, null));
            if (Exists)
                Invalidate();
            return mItems.Count - 1;
        }

        public int AddItem(string item, object userData)
        {
            mItems.Add(new DialogItemListBoxString(item, null));
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

        private IColorScheme mColors;

        public ModalListBox(int row, int column, int height, int width, IColorScheme colors) : base()
        {
            if (height < 3)
                throw new ArgumentException(Resources.ListBoxHeight3, "height");
            mRow = row;
            mColumn = column;
            mHeight = height;
            mWidth = width;
            mColors = colors;
            mOk = false;
        }

        public bool DoModal(WindowManager mgr)
        {
            mManager = mgr;
            mgr.DoModal(this, mRow, mColumn, mHeight, mWidth);
            return mOk && mCurSel >= 0 && mCurSel < mItems.Count;
        }

        public override void OnPaint(Canvas canvas)
        {
            CanvasColor color;
            color = mColors.DialogItemListBoxColor;
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

                if (i == mCurSel)
                {
                    color = mColors.DialogItemListBoxSelectionFocused;
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


        public override void OnCreate()
        {
            mManager.CaptureMouse(this);
            if (mCurSel >= 0)
                EnsureVisible(mCurSel);
        }

        public override void OnClose()
        {
            mManager.ReleaseMouse(this);
        }

        public override void OnScreenSizeChanged(int height, int width)
        {
            base.OnScreenSizeChanged(height, width);
            if (mCurSel >= 0)
                EnsureVisible(mCurSel);
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (scanCode == ScanCode.TAB || scanCode == ScanCode.RETURN)
            {
                mOk = true;
                Manager.Close(this);
            }
            else if (scanCode == ScanCode.ESCAPE)
            {
                mOk = false;
                Manager.Close(this);
            }
            else if (!ctrl && !alt && !shift && scanCode == ScanCode.UP)
            {
                if (mCurSel > 0)
                {
                    mCurSel--;
                }
                EnsureVisible(mCurSel);
                Invalidate();
            }
            else if (!ctrl && !alt && !shift && scanCode == ScanCode.DOWN)
            {
                if (mCurSel < mItems.Count - 1)
                {
                    mCurSel++;
                }
                EnsureVisible(mCurSel);
                Invalidate();
            }
            if (!ctrl && !alt && !shift && scanCode == ScanCode.PRIOR)
            {
                mCurSel -= (Height - 2);
                if (mCurSel < 0)
                    mCurSel = 0;
                EnsureVisible(mCurSel);
                Invalidate();
            }
            else if (!ctrl && !alt && !shift && scanCode == ScanCode.NEXT)
            {
                mCurSel += (Height - 2);
                if (mCurSel >= mItems.Count - 1)
                    mCurSel = mItems.Count - 1;
                EnsureVisible(mCurSel);
                Invalidate();
            }
            else if (!ctrl && !alt && !shift && scanCode == ScanCode.HOME)
            {
                mCurSel = 0;
                EnsureVisible(mCurSel);
                Invalidate();
            }
            else if (!ctrl && !alt && !shift && scanCode == ScanCode.END)
            {
                mCurSel = mItems.Count - 1;
                EnsureVisible(mCurSel);
                Invalidate();
            }
            else if (!ctrl && !alt && (character >= ' '))
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
                        break;
                    }

                    item++;
                    if (item == stop)
                        break;
                    if (item == mItems.Count)
                        item = 0;
                }
            }
        }

        public override void OnMouseLButtonDown(int _row, int _column, bool shift, bool ctrl, bool alt)
        {
            int row, column;
            bool inWindow = ScreenToWindow(_row, _column, out row, out column);
            if (!inWindow)
            {
                mOk = false;
                Manager.Close(this);
            }

            if (row >= 1 && row < Height - 1 &&
                column >= 1 && column < Width - 1)
            {
                int index = mOffset + row - 1;
                if (index >= 0 && index < mItems.Count && index != mCurSel)
                {
                    mCurSel = index;
                    EnsureVisible(mCurSel);
                    Invalidate();
                }
                else if (index >= 0 && index == mCurSel)
                {
                    mOk = true;
                    Manager.Close(this);
                }
            }

            if (column == Width - 1)
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
                    EnsureVisible(mCurSel);
                    Invalidate();
                }
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
            if (mCurSel >= 0)
                EnsureVisible(mCurSel);
            Invalidate();
        }
    }
}
