using System;
using System.Collections.Generic;
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
            {
                mLayout = new List<VerticalPopupMenuLayoutItem>();
                width = 1;
                foreach (MenuItem item in mMenu)
                {
                    if (item is CommandMenuItem)
                    {
                        CommandMenuItem cmd = item as CommandMenuItem;
                        mLayout.Add(new VerticalPopupMenuLayoutItem(width, cmd.Title.Length));
                        width += cmd.Title.Length + 1;
                    }
                    else if (item is PopupMenuItem)
                    {
                        PopupMenuItem cmd = item as PopupMenuItem;
                        mLayout.Add(new VerticalPopupMenuLayoutItem(width, cmd.Title.Length));
                        width += cmd.Title.Length + 1;
                    }
                    else if (item is SeparatorMenuItem)
                    {
                        mLayout.Add(new VerticalPopupMenuLayoutItem(width, 2));
                        width += 2;
                    }
                }
                height = 3;
            }
            else
            {
                int leftWidth, rightWidth;
                leftWidth = 0;
                rightWidth = 0;

                foreach (MenuItem item in mMenu)
                {
                    if (item is CommandMenuItem)
                    {
                        CommandMenuItem cmd = item as CommandMenuItem;
                        if (cmd.Title.Length > leftWidth)
                            leftWidth = cmd.Title.Length;
                        if (cmd.RightSide != null && cmd.RightSide.Length > rightWidth)
                            rightWidth = cmd.RightSide.Length;
                    }
                    else if (item is PopupMenuItem)
                    {
                        PopupMenuItem cmd = item as PopupMenuItem;
                        if (cmd.Title.Length > leftWidth)
                            leftWidth = cmd.Title.Length;
                        if (rightWidth < 1)
                            rightWidth = 1;
                    }
                }
                width = 3 + leftWidth;
                if (rightWidth > 0)
                    width = width + rightWidth + 1;
                height = mMenu.Count + 2;
            }
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
            if (mCurSel >= 0 && mCurSel < mMenu.Count)
            {
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
                else if (item is PopupMenuItem)
                {
                    PopupMenuItem subMenu = item as PopupMenuItem;
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

                    if (menu.CommandChosen == PopupCommandEscape)
                    {
                        //just do nothing
                    }
                    else if (menu.CommandChosen == PretranslatedButtonEscape)
                    {
                        OnMouseLButtonDown(mPretranslatedRow, mPretranslatedColumn, false, false, false);
                    }
                    else if (menu.CommandChosen == PopupCommandLeft)
                    {
                        if (mVertical)
                        {
                            Left();
                            Select(true);
                        }
                    }
                    else if (menu.CommandChosen == PopupCommandRight)
                    {
                        if (mVertical)
                        {
                            Right();
                            Select(true);
                        }
                    }
                    else if (menu.CommandChosen != PopupCommandNone)
                    {
                        mCommand = menu.CommandChosen;
                        Manager.Close(this);
                    }
                    return true;
                }
            }
            return false;
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (!shift && !ctrl && !alt)
            {
                if (scanCode == ScanCode.RIGHT)
                {
                    if (mVertical)
                        Right();
                    else
                    {
                        if (!Select(true) && mParent != null)
                        {
                            mCommand = PopupCommandRight;
                            Manager.Close(this);
                        }
                    }
                    return;
                }
                else if (scanCode == ScanCode.LEFT)
                {
                    if (mVertical)
                        Left();
                    else
                    {
                        if (mParent != null)
                        {
                            mCommand = PopupCommandLeft;
                            Manager.Close(this);
                        }
                    }
                    return;
                }
                else if (scanCode == ScanCode.DOWN)
                {
                    if (mVertical)
                        Select(true);
                    else
                        Right();
                    return;
                }
                else if (scanCode == ScanCode.UP)
                {
                    if (!mVertical)
                        Left();
                    return;
                }
                else if (scanCode == ScanCode.HOME)
                {
                    mCurSel = 0;
                    while (mCurSel < mMenu.Count && mMenu[mCurSel] is SeparatorMenuItem)
                        mCurSel++;
                    Invalidate();
                    return;
                }
                else if (scanCode == ScanCode.END)
                {
                    mCurSel = mMenu.Count - 1;
                    while (mCurSel >= 0 && mMenu[mCurSel] is SeparatorMenuItem)
                        mCurSel--;
                    Invalidate();
                    return;
                }
                else if (scanCode == ScanCode.ESCAPE)
                {
                    mCommand = PopupCommandEscape;
                    Manager.Close(this);
                    return;
                }
                else if (scanCode == ScanCode.RETURN)
                {
                    Select(false);
                    return;
                }
            }
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

        public override void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (ScreenToWindow(row, column, out int winRow, out int winColumn))
            {
                int newSel = -1;
                if (mVertical)
                {
                    for (int i = 0; i < mLayout.Count; i++)
                    {
                        VerticalPopupMenuLayoutItem item = mLayout[i];
                        if (winColumn >= item.offset && winColumn < item.offset + item.length)
                        {
                            newSel = i;
                            break;
                        }
                    }
                }
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

            if (mParent?.PreTranslateLButtonDown(row, column) == true)
            {
                mCommand = PretranslatedButtonEscape;
                Manager.Close(this);
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
            {
                for (int i = 0; i < mMenu.Count; i++)
                {
                    VerticalPopupMenuLayoutItem layout = mLayout[i];
                    MenuItem item = mMenu[i];
                    CanvasColor color;
                    string title;
                    int highlight = -1;

                    if (item is CommandMenuItem)
                    {
                        CommandMenuItem cmd = item as CommandMenuItem;
                        title = cmd.Title;
                        if (mCurSel == i)
                        {
                            if (cmd.Enabled)
                                color = mColors.MenuItemSelected;
                            else
                                color = mColors.MenuDisabledItemSelected;
                        }
                        else
                        {
                            if (cmd.Enabled)
                                color = mColors.MenuItem;
                            else
                                color = mColors.MenuDisabledItem;
                        }

                        if (cmd.HasHotkey)
                            highlight = cmd.HotKeyPosition;
                    }
                    else if (item is PopupMenuItem)
                    {
                        PopupMenuItem cmd = item as PopupMenuItem;
                        title = cmd.Title;
                        if (mCurSel == i)
                            color = mColors.MenuItemSelected;
                        else
                            color = mColors.MenuItem;

                        if (cmd.HasHotkey)
                            highlight = cmd.HotKeyPosition;
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
                    if (highlight >= 0)
                    {
                        if (mCurSel == i)
                            canvas.Write(1, layout.offset + highlight, mColors.MenuHotKeySelected);
                        else
                            canvas.Write(1, layout.offset + highlight, mColors.MenuHotKey);
                    }
                }
            }
            else
            {
                for (int i = 0; i < mMenu.Count; i++)
                {
                    MenuItem item = mMenu[i];

                    CanvasColor color;

                    if (item is CommandMenuItem)
                    {
                        CommandMenuItem cmd = item as CommandMenuItem;
                        if (mCurSel == i)
                        {
                            if (cmd.Enabled)
                                color = mColors.MenuItemSelected;
                            else
                                color = mColors.MenuDisabledItemSelected;
                        }
                        else
                        {
                            if (cmd.Enabled)
                                color = mColors.MenuItem;
                            else
                                color = mColors.MenuDisabledItem;
                        }
                        canvas.Fill(i + 1, 1, 1, Width - 2, color);
                        canvas.Write(i + 1, 2, cmd.Title);
                        if (cmd.HasHotkey)
                        {
                            if (mCurSel == i)
                                canvas.Write(i + 1, 2 + cmd.HotKeyPosition, mColors.MenuHotKeySelected);
                            else
                                canvas.Write(i + 1, 2 + cmd.HotKeyPosition, mColors.MenuHotKey);
                        }

                        if (cmd.RightSide != null)
                            canvas.Write(i + 1, Width - (cmd.RightSide.Length + 1), cmd.RightSide);
                        if (cmd.Checked)
                            canvas.Write(i + 1, 1, '\u221a');

                        if (mCurSel == i)
                            WindowManager.SetCaretPos(this, i + 1, 2);
                    }
                    else if (item is PopupMenuItem)
                    {
                        PopupMenuItem cmd = item as PopupMenuItem;
                        if (mCurSel == i)
                            color = mColors.MenuItemSelected;
                        else
                            color = mColors.MenuItem;
                        canvas.Fill(i + 1, 1, 1, Width - 2, color);
                        canvas.Write(i + 1, 2, cmd.Title);
                        canvas.Write(i + 1, Width - 2, '\u25ba');
                        if (cmd.HasHotkey)
                        {
                            if (mCurSel == i)
                                canvas.Write(i + 1, 2 + cmd.HotKeyPosition, mColors.MenuHotKeySelected);
                            else
                                canvas.Write(i + 1, 2 + cmd.HotKeyPosition, mColors.MenuHotKey);
                        }

                        if (mCurSel == i)
                            WindowManager.SetCaretPos(this, i + 1, 2);
                    }
                    else if (item is SeparatorMenuItem)
                    {
                        canvas.Fill(i + 1, 1, 1, Width - 2, '\u2500');
                        canvas.Write(i + 1, 0, '\u251c');
                        canvas.Write(i + 1, Width - 1, '\u2524');
                    }
                }
            }
        }
        #endregion
    }
}
