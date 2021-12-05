using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    /// <summary>
    /// Popup menu
    /// </summary>
    public class PopupMenu : Window
    {
        #region properties
        /// <summary>
        /// Layout of one menu item in vertical menu layout
        /// </summary>
        private sealed class VerticalPopupMenuLayoutItem
        {
            /// <summary>
            /// Offset of the menu item from left corner of menu bar
            /// </summary>
            public int offset;
            /// <summary>
            /// Length of the menu item in columns
            /// </summary>
            public int length;

            internal VerticalPopupMenuLayoutItem(int _offset, int _length)
            {
                offset = _offset;
                length = _length;
            }

            public bool IsInsideItem(int position) => position >= offset && position < offset + length;
        };

        public const int PopupCommandNone = 0;
        public const int PopupCommandEscape = -1;
        internal const int PopupCommandLeft = -2;
        internal const int PopupCommandRight = -3;
        internal const int PretranslatedButtonEscape = -4;

        /// <summary>
        /// Menu content
        /// </summary>
        private readonly PopupMenuItem mMenu;
        /// <summary>
        /// Menu colors
        /// </summary>
        private readonly IColorScheme mColors;
        /// <summary>
        /// Parent menu
        /// </summary>
        private readonly PopupMenu mParent;
        /// <summary>
        /// Menu Bar layout type
        /// </summary>
        private readonly bool mVertical;
        /// <summary>
        /// Layout for vertical bar
        /// </summary>
        private List<VerticalPopupMenuLayoutItem> mLayout;
        /// <summary>
        /// Chosen command
        /// </summary>
        private int mCommand = PopupCommandNone;
        /// <summary>
        /// Currently selected item
        /// </summary>
        private int mCurSel = 0;

        /// <summary>
        /// Chosen command
        /// </summary>
        public int CommandChosen => mCommand;
        #endregion

        #region constructor
        /// <summary>
        /// Constructor for subsequent menu
        /// </summary>
        /// <param name="parent">parent menu bar</param>
        /// <param name="menu">menu content</param>
        /// <param name="colors">menu colors</param>
        /// <param name="vertical">flag indicating whether the bar must be vertical or horizonal</param>
        internal PopupMenu(PopupMenu parent, PopupMenuItem menu, IColorScheme colors, bool vertical)
        {
            mMenu = menu ?? throw new ArgumentNullException(nameof(menu));
            mColors = colors;
            mVertical = vertical;
            mParent = parent;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="menu">menu content</param>
        /// <param name="defaultColor">default menu item color</param>
        /// <param name="defaultSelectedColor">default menu item selected color</param>
        /// <param name="vertical">flag indicating whether the bar must be vertical or horizonal</param>
        public PopupMenu(PopupMenuItem menu, IColorScheme colors, bool vertical)
        {
            mMenu = menu ?? throw new ArgumentNullException(nameof(menu));
            mColors = colors;
            mVertical = vertical;
            mParent = null;
        }

        /// <summary>
        /// Calculate the size of the menu bar and layout of vertical menu bars
        /// </summary>
        /// <param name="height">[output] menu bar height in rows</param>
        /// <param name="width">[output] menu bar width in rows</param>
        internal void DoLayout(out int height, out int width)
        {
            if (mVertical)
                DoVerticalLayout(out height, out width);
            else
                DoHorizontalLayout(out height, out width);
        }

        private void DoVerticalLayout(out int height, out int width)
        {
            mLayout = new List<VerticalPopupMenuLayoutItem>();
            width = 1;
            foreach (MenuItem item in mMenu)
            {
                if (item is CommandMenuItem cmd)
                {
                    mLayout.Add(new VerticalPopupMenuLayoutItem(width, cmd.Title.Length));
                    width += cmd.Title.Length + 1;
                }
                else if (item is PopupMenuItem popup)
                {
                    mLayout.Add(new VerticalPopupMenuLayoutItem(width, popup.Title.Length));
                    width += popup.Title.Length + 1;
                }
                else if (item is SeparatorMenuItem)
                {
                    mLayout.Add(new VerticalPopupMenuLayoutItem(width, 2));
                    width += 2;
                }
            }
            height = 3;
        }

        private void DoHorizontalLayout(out int height, out int width)
        {
            int leftWidth = mMenu.Max(item =>
            {
                if (item is CommandMenuItem cmd)
                    return cmd.Title.Length;
                else if (item is PopupMenuItem popup)
                    return popup.Title.Length;
                return 0;
            });

            int rightWidth = mMenu.Max(item =>
            {
                if (item is CommandMenuItem cmd)
                    return cmd.RightSide.Length;
                else if (item is PopupMenuItem popup)
                    return 1;
                return 0;
            });

            width = 3 + leftWidth;
            
            if (rightWidth > 0)
                width = width + rightWidth + 1;
            height = mMenu.Count + 2;
        }
        #endregion

        #region activities
        public override void OnCreate()
        {
            Manager.ShowCaret(false);
            Manager.CaptureMouse(this);
        }

        public override void OnClose()
        {
            Manager.ReleaseMouse(this);
        }

        /// <summary>
        /// go to the previous item
        /// </summary>
        private void Left()
        {
            mCurSel--;
            if (mCurSel < 0)
                mCurSel = mMenu.Count - 1;
            while (mCurSel >= 0 && mMenu[mCurSel] is SeparatorMenuItem)
                mCurSel--;
            Invalidate();
        }

        /// <summary>
        /// go to the next item
        /// </summary>
        private void Right()
        {
            mCurSel++;
            while (mCurSel < mMenu.Count && mMenu[mCurSel] is SeparatorMenuItem)
                mCurSel++;
            if (mCurSel >= mMenu.Count)
            {
                mCurSel = 0;
                while (mCurSel < mMenu.Count && mMenu[mCurSel] is SeparatorMenuItem)
                    mCurSel++;
            }
            Invalidate();
        }

        /// <summary>
        /// choose item
        /// </summary>
        /// <param name="onlypopup">if the flag is true, only submenus are processed</param>
        /// <returns></returns>
        private bool Select(bool onlypopup)
        {
            if (mCurSel < 0 && mCurSel >= mMenu.Count)
                return false;

            MenuItem item = mMenu[mCurSel];

            if (item is CommandMenuItem menuItem && !onlypopup)
            {
                if (menuItem.Enabled)
                {
                    mCommand = menuItem.Command;
                    Manager.Close(this);
                }
                return true;
            }
            else if (item is PopupMenuItem subMenu)
            {
                PopupMenu menu = new PopupMenu(this, subMenu, mColors, false);
                int row, column;
                if (mVertical)
                {
                    column = mLayout[mCurSel].offset;
                    row = Row + Height;
                }
                else
                {
                    column = Column + Width;
                    row = Row + mCurSel + 1;
                }
                Manager.ReleaseMouse(this);
                Manager.ShowPopupMenu(menu, row, column);
                Manager.CaptureMouse(this);

                switch (menu.CommandChosen)
                {
                    case PretranslatedButtonEscape:
                        OnMouseLButtonDown(mPretranslatedRow, mPretranslatedColumn, false, false, false);
                        break;
                    case PopupCommandLeft when mVertical:
                        Left();
                        Select(true);
                        break;
                    case PopupCommandRight when mVertical:
                        Right();
                        Select(true);
                        break;
                    case PopupCommandEscape:
                    case PopupCommandLeft when !mVertical:
                    case PopupCommandRight when !mVertical:
                    case PopupCommandNone:
                        //do nothing
                        break;
                    default:
                        mCommand = menu.CommandChosen;
                        Manager.Close(this);
                        break;
                }
                return true;
            }
            return false;
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (!shift && !ctrl && !alt && ProcessControlKey(scanCode))
                return;

            if (!ctrl && character > 0)
            {
                for (int i = 0; i < mMenu.Count; i++)
                {
                    MenuItem item = mMenu[i];
                    if (item is TitledMenuItem)
                    {
                        TitledMenuItem t_item = item as TitledMenuItem;
                        if (t_item.HasHotkey && t_item.HotKey == character)
                        {
                            mCurSel = i;
                            Select(false);
                            break;
                        }
                    }
                }
            }
        }

        private bool ProcessControlKey(ScanCode scanCode)
        {
            switch (scanCode)
            {
                case ScanCode.RIGHT when mVertical:
                    Right();
                    return true;
                case ScanCode.RIGHT when !mVertical:
                    if (!Select(true) && mParent != null)
                    {
                        mCommand = PopupCommandRight;
                        Manager.Close(this);
                    }
                    return true;
                case ScanCode.LEFT when mVertical:
                    Left();
                    return true;
                case ScanCode.LEFT when !mVertical:
                    if (mParent != null)
                    {
                        mCommand = PopupCommandLeft;
                        Manager.Close(this);
                    }
                    return true;
                case ScanCode.DOWN when mVertical:
                    Select(true);
                    return true;
                case ScanCode.DOWN when !mVertical:
                    Right();
                    return true;
                case ScanCode.UP when !mVertical:
                    Left();
                    return true;
                case ScanCode.HOME:
                    mCurSel = 0;
                    while (mCurSel < mMenu.Count && mMenu[mCurSel] is SeparatorMenuItem)
                        mCurSel++;
                    Invalidate();
                    return true;
                case ScanCode.END:
                    mCurSel = mMenu.Count - 1;
                    while (mCurSel >= 0 && mMenu[mCurSel] is SeparatorMenuItem)
                        mCurSel--;
                    Invalidate();
                    return true;
                case ScanCode.ESCAPE:
                    mCommand = PopupCommandEscape;
                    Manager.Close(this);
                    return true;
                case ScanCode.RETURN:
                    Select(false);
                    return true;
            }
            return false;
        }

        public override void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (ScreenToWindow(row, column, out int winRow, out int winColumn))
                OnMouseLButton_FindItemByClick(winRow, winColumn);

            if (mParent?.PreTranslateLButtonDown(row, column) == true)
            {
                mCommand = PretranslatedButtonEscape;
                Manager.Close(this);
            }
        }

        private void OnMouseLButton_FindItemByClick(int winRow, int winColumn)
        {
            int newSel = -1;
            
            if (mVertical)
                newSel = mLayout.FindIndex(item => item.IsInsideItem(winColumn));
            else
            {
                winRow--;
                if (winRow >= 0 && winRow < mMenu.Count)
                {
                    MenuItem item = mMenu[winRow];
                    if (!(item is SeparatorMenuItem))
                        newSel = winRow;
                }
            }

            if (newSel >= 0)
            {
                if (newSel == mCurSel)
                    Select(false);
                else
                {
                    mCurSel = newSel;
                    Invalidate();
                }
            }
        }

        internal int mPretranslatedRow, mPretranslatedColumn;

        internal bool PreTranslateLButtonDown(int row, int column)
        {
            bool rc;
            if (ScreenToWindow(row, column, out int _, out int _))
            {
                rc = true;
            }
            else if (mParent != null)
            {
                rc = mParent.PreTranslateLButtonDown(row, column);
            }
            else
                rc = false;
            if (rc)
            {
                mPretranslatedRow = row;
                mPretranslatedColumn = column;
            }
            return rc;
        }

        public override void OnMouseRButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            mCommand = PopupCommandEscape;
            Manager.Close(this);
        }

        public override void OnPaint(Canvas canvas)
        {
            canvas.Box(0, 0, Height, Width, BoxBorder.Single, mColors.MenuItem, ' ');
            if (mVertical)
                PaintVertical(canvas);
            else
                PaintHorizontal(canvas);
        }

        private void PaintVertical(Canvas canvas)
        {
            for (int i = 0; i < mMenu.Count; i++)
                PaintVertical_Item(canvas, i);
        }

        private void PaintVertical_Item(Canvas canvas, int index)
        {
            VerticalPopupMenuLayoutItem layout = mLayout[index];
            MenuItem item = mMenu[index];
            CanvasColor color;
            string title;
            int highlight = -1;

            if (item is CommandMenuItem cmd)
            {
                title = cmd.Title;
                color = GetItemColor(index, cmd.Enabled);

                if (cmd.HasHotkey)
                    highlight = cmd.HotKeyPosition;
            }
            else if (item is PopupMenuItem popup)
            {
                title = popup.Title;
                color = GetItemColor(index, true);
                if (popup.HasHotkey)
                    highlight = popup.HotKeyPosition;
            }
            else if (item is SeparatorMenuItem)
            {
                color = mColors.MenuItem;
                title = "\u2502";
            }
            else
            {
                color = mColors.MenuItem;
                title = "";
            }
            canvas.Write(1, layout.offset, title, color);


            if (highlight < 0) //if nothing to highlight
                return;

            var hotKeyColor = mCurSel == index ? mColors.MenuHotKeySelected : mColors.MenuHotKey;
            canvas.Write(1, layout.offset + highlight, hotKeyColor);
        }

        private void PaintHorizontal(Canvas canvas)
        {
            for (int i = 0; i < mMenu.Count; i++)
                PaintHorizontal_Item(canvas, i);
        }

        private void PaintHorizontal_Item(Canvas canvas, int index)
        {
            MenuItem item = mMenu[index];

            CanvasColor color;

            if (item is CommandMenuItem)
            {
                CommandMenuItem cmd = item as CommandMenuItem;
                color = GetItemColor(index, cmd.Enabled);
                canvas.Fill(index + 1, 1, 1, Width - 2, color);
                canvas.Write(index + 1, 2, cmd.Title);

                if (cmd.HasHotkey)
                    PaintHorizontal_HighlightHotKey(canvas, index, cmd.HotKeyPosition);

                if (cmd.RightSide != null)
                    canvas.Write(index + 1, Width - (cmd.RightSide.Length + 1), cmd.RightSide);
                if (cmd.Checked)
                    canvas.Write(index + 1, 1, '\u221a');

                if (mCurSel == index)
                    WindowManager.SetCaretPos(this, index + 1, 2);
            }
            else if (item is PopupMenuItem popup)
            {
                color = GetItemColor(index, true);
                canvas.Fill(index + 1, 1, 1, Width - 2, color);
                canvas.Write(index + 1, 2, popup.Title);
                canvas.Write(index + 1, Width - 2, '\u25ba');

                if (popup.HasHotkey)
                    PaintHorizontal_HighlightHotKey(canvas, index, popup.HotKeyPosition);

                if (mCurSel == index)
                    WindowManager.SetCaretPos(this, index + 1, 2);
            }
            else if (item is SeparatorMenuItem)
            {
                canvas.Fill(index + 1, 1, 1, Width - 2, '\u2500');
                canvas.Write(index + 1, 0, '\u251c');
                canvas.Write(index + 1, Width - 1, '\u2524');
            }
        }

        private void PaintHorizontal_HighlightHotKey(Canvas canvas, int commandIndex, int hotkeyPosition)
        {
            if (mCurSel == commandIndex)
                canvas.Write(commandIndex + 1, 2 + hotkeyPosition, mColors.MenuHotKeySelected);
            else
                canvas.Write(commandIndex + 1, 2 + hotkeyPosition, mColors.MenuHotKey);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private CanvasColor GetItemColor(int index, bool enabled)
        {
            if (mCurSel == index)
            {
                if (enabled)
                    return mColors.MenuItemSelected;
                else
                    return mColors.MenuDisabledItemSelected;
            }
            if (enabled)
                return mColors.MenuItem;
            else
                return mColors.MenuDisabledItem;
        }


        #endregion
    }
}
