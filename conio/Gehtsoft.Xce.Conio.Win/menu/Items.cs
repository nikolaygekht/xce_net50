using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    /// <summary>
    /// Base class for menu items
    /// </summary>
    public abstract class MenuItem
    {
        public MenuItem()
        {
        }
    }

    /// <summary>
    /// Menu item separator
    /// </summary>
    public class SeparatorMenuItem : MenuItem
    {
        public SeparatorMenuItem()
        {
        }
    }

    public abstract class TitledMenuItem : MenuItem
    {
        private string mTitle;
        private char mHotKey;
        private int mHotkeyPosition;

        internal TitledMenuItem(string title)
        {
            if (title == null)
                throw new ArgumentNullException("title");

            mHotkeyPosition = StringUtil.ProcessHotKey(ref title);
            if (mHotkeyPosition >= 0)
                mHotKey = title[mHotkeyPosition];
            mTitle = title;
        }

        /// <summary>
        /// Command name
        /// </summary>
        public string Title
        {
            get
            {
                return mTitle;
            }
        }

        public bool HasHotkey
        {
            get
            {
                return mHotkeyPosition >= 0;
            }
        }

        public char HotKey
        {
            get
            {
                if (!HasHotkey)
                    throw new InvalidOperationException();

                return mHotKey;
            }
        }

        public int HotKeyPosition
        {
            get
            {
                return mHotkeyPosition;
            }
        }
    }

    /// <summary>
    /// Menu command
    /// </summary>
    public class CommandMenuItem : TitledMenuItem
    {
        private string mRightSide;
        private int mCommand;
        private bool mEnabled;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title">The name of the command</param>
        /// <param name="command">The command object</param>
        public CommandMenuItem(string title, int command) : base(title)
        {
            mRightSide = null;
            mCommand = command;
            mEnabled = true;
            Checked = false;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title">the name of the command</param>
        /// <param name="rightSide">the command comment (for example key shortcut) located at right side</param>
        /// <param name="command">the command object</param>
        public CommandMenuItem(string title, string rightSide, int command) : base(title)
        {
            mRightSide = rightSide;
            mCommand = command;
            mEnabled = true;
            Checked = false;
        }



        /// <summary>
        /// right-side command comment
        /// </summary>
        public string RightSide
        {
            get
            {
                return mRightSide;
            }
        }

        /// <summary>
        /// command object
        /// </summary>
        public int Command
        {
            get
            {
                return mCommand;
            }
        }

        public bool Checked { get; set; }

        public bool Enabled
        {
            get
            {
                return mEnabled;
            }
            set
            {
                mEnabled = value;
            }
        }
    }

    /// <summary>
    /// Popup-menu
    /// </summary>
    public class PopupMenuItem : TitledMenuItem, IEnumerable<MenuItem>
    {
        private List<MenuItem> mItems = new List<MenuItem>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title">the title of the menu</param>
        public PopupMenuItem(string title) : base(title)
        {
        }

        /// <summary>
        /// Number of menu items
        /// </summary>
        public int Count
        {
            get
            {
                return mItems.Count;
            }
        }

        /// <summary>
        /// Menu item by index
        /// </summary>
        /// <param name="index">The zero-based index of the menu item</param>
        public MenuItem this[int index]
        {
            get
            {
                return mItems[index];
            }
        }

        IEnumerator<MenuItem> IEnumerable<MenuItem>.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }

        public void AddItem(MenuItem item)
        {
            mItems.Add(item);
        }

        public void CreateCommand(string title, int command)
        {
            AddItem(new CommandMenuItem(title, command));
        }

        public void CreateCommand(string title, string rightside, int command)
        {
            AddItem(new CommandMenuItem(title, rightside, command));
        }

        public void CreateSeparator()
        {
            AddItem(new SeparatorMenuItem());
        }

        public PopupMenuItem CreatePopup(string title)
        {
            PopupMenuItem popup = new PopupMenuItem(title);
            AddItem(popup);
            return popup;
        }

        public void Clear()
        {
            mItems.Clear();
        }
    }
}
