using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win.Test
{
    internal static class MainMenuFactory
    {
        private static PopupMenu gMenu;

        public const int ExitId = 1;
        public const int MessageBoxId = 2;
        public const int TestFocusId = 3;
        public const int TestFileDialogId = 4;
        public const int TestDialog1Id = 5;
        public const int TestDialog2Id = 6;

        public static PopupMenu Menu
        {
            get
            {
                if (gMenu == null)
                    CreateMenu();
                return gMenu;
            }
        }

        private static void CreateMenu()
        {
            PopupMenuItem bar = new PopupMenuItem("bar");
            PopupMenuItem submenu = bar.CreatePopup("Examples");
            submenu.CreateCommand("Show &Message Box", MessageBoxId);
            submenu.CreateCommand("Test &Focus", TestFocusId);
            submenu.CreateCommand("Test F&ile Dialog", TestFileDialogId);
            submenu.CreateCommand("Test Dialog &1", TestDialog1Id);
            submenu.CreateCommand("Test Dialog &2", TestDialog2Id);
            submenu.CreateSeparator();
            submenu.CreateCommand("&Exit", ExitId);
            gMenu = new PopupMenu(bar, Program.CurrentSheme, true);
        }
    }
}
