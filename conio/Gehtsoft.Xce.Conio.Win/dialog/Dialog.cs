using System;
using System.Collections.Generic;
using System.Text;
using CanvasColor = Gehtsoft.Xce.Conio.CanvasColor;

namespace Gehtsoft.Xce.Conio.Win
{
    public class Dialog : WindowBorderContainer
    {
        internal class DialogClientArea : Window
        {
            private readonly CanvasColor mBg;
            private readonly Dialog  mDlg;
            private Window mPreviousFocus;

            internal DialogClientArea(Dialog dlg, CanvasColor background)
            {
                mBg = background;
                mDlg = dlg;
            }

            public override void OnCreate()
            {
                DialogItem firstInput = null;
                foreach (DialogItem item in mDlg.Items)
                {
                    item.SetDialog(mDlg);
                    Manager.Create(item, this, item.Row, item.Column, item.Height, item.Width);
                    item.Show(true);
                    if (item.IsInputElement && firstInput == null)
                        firstInput = item;
                }
                mPreviousFocus = Manager.GetFocus();
                if (firstInput != null)
                    Manager.SetFocus(firstInput);
            }

            public override void OnClose()
            {
                Manager.SetFocus(mPreviousFocus);
            }

            public override void OnPaint(Canvas canvas)
            {
                canvas.Fill(0, 0, Height, Width, ' ', mBg);
            }
        }

        private readonly DialogClientArea mClientArea;
        private readonly List<DialogItem> mItems = new List<DialogItem>();
        private int mDialogResultCode = -1;
        public const int DialogResultOK = 0;
        public const int DialogResultCancel = -1;
        private readonly int mHeight, mWidth;

        public int ResultCode => mDialogResultCode;

        public IColorScheme Colors { get; }

        public Dialog(string title, IColorScheme colors, bool sizeable, int height, int width) : base(title, BoxBorder.Single, colors.DialogBorder, true, sizeable)
        {
            Colors = colors;
            mClientArea = new DialogClientArea(this, colors.DialogBorder);
            AttachClientArea(mClientArea);
            mHeight = height;
            mWidth = width;
        }

        public void AddItem(DialogItem item)
        {
            mItems.Add(item);
            if (Exists)
            {
                item.SetDialog(this);
                Manager.Create(item, mClientArea, item.Row, item.Column, item.Height, item.Width);
                item.Show(true);
            }
        }

        public void AddItemBefore(DialogItem item, DialogItem next)
        {
            int position;

            if (next == null)
                position = 0;
            else
            {
                for (position = 0; position < mItems.Count; position++)
                    if (mItems[position] == next)
                        break;
                if (position == mItems.Count)
                    throw new ArgumentOutOfRangeException(nameof(next));
            }
            mItems.Insert(position, item);
            if (Exists)
            {
                item.SetDialog(this);
                Manager.Create(item, mClientArea, item.Row, item.Column, item.Height, item.Width);
                item.Show(true);
            }
        }

        public int DoModal(WindowManager manager)
        {
            int height = manager.ScreenHeight;
            int width = manager.ScreenWidth;

            int row = (height - mHeight) / 2;
            int column = (width - mWidth) / 2;

            manager.CreateModal(this, row, column, mHeight, mWidth);
            Show(true);
            while (Exists)
                manager.PumpMessage(-1);

            return mDialogResultCode;
        }

        public void EndDialog(int resultCode)
        {
            mDialogResultCode = resultCode;
            Manager.Close(this);
        }

        public IEnumerable<DialogItem> Items => mItems;

        virtual public bool PretranslateOnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (scanCode == ScanCode.TAB)
            {
                Window focus = Manager.GetFocus();
                if (focus is DialogItem dlg && dlg.Dialog == this)
                {
                    int i, curr = -1;
                    for (i = 0; i < mItems.Count; i++)
                    {
                        if (mItems[i] == focus)
                        {
                            curr = i;
                            break;
                        }
                    }

                    if (curr != -1)
                    {
                        if (!shift && !ctrl && !alt)
                        {
                            for (i = curr + 1; i != curr; i++)
                            {
                                if (i == mItems.Count)
                                {
                                    i = -1;
                                    continue;
                                }
                                if (mItems[i].IsInputElement && mItems[i].Enabled)
                                {
                                    Manager.SetFocus(mItems[i]);
                                    break;
                                }
                            }
                        }
                        else if (shift && !ctrl && !alt)
                        {
                            for (i = curr - 1; i != curr; i--)
                            {
                                if (i == -1)
                                {
                                    i = mItems.Count;
                                    continue;
                                }

                                if (mItems[i].IsInputElement && mItems[i].Enabled)
                                {
                                    Manager.SetFocus(mItems[i]);
                                    break;
                                }
                            }
                        }
                        return true;
                    }
                }
            }
            else if (scanCode == ScanCode.RETURN)
            {
                if (!shift && !ctrl && !alt)
                {
                    //find OK button
                    foreach (DialogItem item in mItems)
                        if (item.ID == Dialog.DialogResultOK)
                        {
                            item.Click();
                            return true;
                        }
                }
                return false;
            }
            else if (scanCode == ScanCode.ESCAPE)
            {
                if (!shift && !ctrl && !alt)
                {
                    //find OK button
                    foreach (DialogItem item in mItems)
                        if (item.ID == Dialog.DialogResultCancel)
                        {
                            item.Click();
                            return true;
                        }
                }
                return false;
            }

            if (!ctrl && alt && character > ' ')
            {
                for (int i = 0; i < mItems.Count; i++)
                {
                    DialogItem item = mItems[i];
                    if (item.HasHotKey && item.Enabled && char.ToUpper(item.HotKey) == char.ToUpper(character))
                    {
                        if (item.IsInputElement)
                        {
                            Manager.SetFocus(item);
                            item.OnHotkeyActivated();
                        }
                        else
                        {
                            if (i != mItems.Count - 1)
                            {
                                item = mItems[i + 1];
                                if (item.Enabled && item.IsInputElement)
                                {
                                    Manager.SetFocus(item);
                                    item.OnHotkeyActivated();
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public virtual void OnItemClick(DialogItem item)
        {
            if (item is DialogItemButton && item.ID == DialogResultOK && OnOK())
                EndDialog(DialogResultOK);
            else if (item is DialogItemButton && item.ID == DialogResultCancel && OnCancel())
                EndDialog(DialogResultCancel);
        }

        public virtual void OnItemActivated(DialogItem item)
        {
        }

        public virtual void OnItemChanged(DialogItem item)
        {
        }

        public virtual bool OnOK()
        {
            return true;
        }

        public virtual bool OnCancel()
        {
            return true;
        }

        public void CenterButtons(DialogItemButton[] buttons)
        {
            int l0 = 0;
            foreach (DialogItemButton button in buttons)
                l0 += button.Width;
            l0 += buttons.Length - 1;

            int width;
            if (Exists)
                width = Width;
            else
                width = mWidth;

            int baseColumn = (width - 2 - l0) / 2;
            if (baseColumn < 0)
                baseColumn = 0;
            foreach (DialogItemButton button in buttons)
            {
                button.Reposition(button.Row, baseColumn, 1, button.Width);
                baseColumn += button.Width + 1;
            }
        }

        public void CenterButtons(DialogItemButton b1)
        {
            CenterButtons(new DialogItemButton[] { b1 });
        }

        public void CenterButtons(DialogItemButton b1, DialogItemButton b2)
        {
            CenterButtons(new DialogItemButton[] { b1, b2 });
        }

        public void CenterButtons(DialogItemButton b1, DialogItemButton b2, DialogItemButton b3)
        {
            CenterButtons(new DialogItemButton[] { b1, b2, b3 });
        }

        public void CenterButtons(DialogItemButton b1, DialogItemButton b2, DialogItemButton b3, DialogItemButton b4)
        {
            CenterButtons(new DialogItemButton[] { b1, b2, b3, b4 });
        }

        public int ItemsCount => mItems.Count;

        public DialogItem GetItem(int index)
        {
            return mItems[index];
        }
    }
}
