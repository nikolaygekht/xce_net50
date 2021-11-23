using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public enum MessageBoxButton
    {
        Ok = Dialog.DialogResultOK,
        Cancel = Dialog.DialogResultCancel,
        Yes = 1,
        No,
        Abort,
        Retry,
        Ignore,
        Option1,
        Option2,
        Option3,
        Option4,
        Unknown,
    }

    public enum MessageBoxButtonSet
    {
        Ok,
        OkCancel,
        YesNo,
        YesCancel,
        YesNoCancel,
        AbortRetryIgnore,
    }

    public class MessageBoxButtonInfo
    {
        string mTitle;
        MessageBoxButton mID;

        public string Title
        {
            get
            {
                return mTitle;
            }
        }

        public MessageBoxButton ID
        {
            get
            {
                return mID;
            }
        }

        public MessageBoxButtonInfo(string title, MessageBoxButton ID)
        {
            mTitle = title;
            mID = ID;
        }
    }

    internal class MessageBoxDialog : Dialog
    {
        public MessageBoxDialog(string title, IColorScheme colors, int height, int width) : base(title, colors, false, height, width)
        {
        }

        public override void OnItemClick(DialogItem item)
        {
            if (item is DialogItemButton)
                EndDialog(item.ID);
        }
    }

    public class MessageBox
    {
        private static string[] mSeparators = new string[] { "\r\n" };

        public static MessageBoxButton Show(WindowManager mgr, IColorScheme colors, string message, string title, MessageBoxButtonInfo[] buttons)
        {
            int i, j;

            if (mgr == null)
                throw new ArgumentNullException("mgr");
            if (colors == null)
                colors = ColorScheme.White;
            if (message == null)
                message = "null";
            if (title == null)
                title = "null";
            if (buttons == null)
                throw new ArgumentNullException("buttons");
            if (buttons.Length < 1 || buttons.Length > 4)
                throw new ArgumentException("invalid length", "buttons");
            for (i = 0; i < buttons.Length; i++)
                if (buttons[i] == null)
                    throw new ArgumentException("null item", "buttons");

            string[] messageLines;
            messageLines = message.Split(mSeparators, StringSplitOptions.None);
            int messageLinesCount = messageLines.Length;
            if (messageLinesCount > 10)
                messageLinesCount = 10;

            int buttonsWidth = 0;
            for (i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].Title == null)
                    buttonsWidth += 6;
                else
                {
                    j = buttons[i].Title.Length;
                    if (j > 16)
                        j = 16;
                    buttonsWidth += j;
                }
            }

            //a space separator
            buttonsWidth += (buttons.Length - 1);
            int splitWidth = mgr.ScreenWidth - 14;

            int textWidth = 0;
            for (i = 0; i < messageLinesCount; i++)
            {
                j = messageLines[i].Length;
                if (j > splitWidth)
                    j = splitWidth;
                if (textWidth < j)
                    textWidth = j;
            }

            int dialogWidth = textWidth;
            if (dialogWidth < buttonsWidth)
                dialogWidth = buttonsWidth;

            int dialogHeight = messageLinesCount + 1;

            MessageBoxDialog dlg = new MessageBoxDialog(title, colors, dialogHeight + 2, dialogWidth + 2);
            for (i = 0; i < messageLinesCount; i++)
            {
                string s = messageLines[i];
                if (s.Length > splitWidth)
                    s = s.Substring(0, splitWidth - 3) + "...";
                dlg.AddItem(new DialogItemLabel(s, 1000 + i, i, 0));
            }

            int buttonRow = messageLinesCount;
            int buttonPos = (dialogWidth - buttonsWidth) / 2;

            for (i = 0; i < buttons.Length; i++)
            {
                string s = buttons[i].Title;
                if (s == null)
                    s = "< null >";
                if (s.Length > 16)
                    s = s.Substring(0, 16);
                dlg.AddItem(new DialogItemButton(s, (int)buttons[i].ID, buttonRow, buttonPos));
                buttonPos += s.Length + 1;
            }

            i = dlg.DoModal(mgr);

            return (MessageBoxButton)i;
        }

        private static MessageBoxButtonInfo[] mOkButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Ok >", MessageBoxButton.Ok) };
        private static MessageBoxButtonInfo[] mOkCancelButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Ok >", MessageBoxButton.Ok), new MessageBoxButtonInfo("< &Cancel >", MessageBoxButton.Cancel) };
        private static MessageBoxButtonInfo[] mYesCancelButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Yes >", MessageBoxButton.Yes), new MessageBoxButtonInfo("< &Cancel >", MessageBoxButton.Cancel) };
        private static MessageBoxButtonInfo[] mYesNoButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Yes >", MessageBoxButton.Yes), new MessageBoxButtonInfo("< &No >", MessageBoxButton.No) };
        private static MessageBoxButtonInfo[] mYesNoCancelButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Yes >", MessageBoxButton.Yes), new MessageBoxButtonInfo("< &No >", MessageBoxButton.No), new MessageBoxButtonInfo("< &Cancel >", MessageBoxButton.Cancel) };
        private static MessageBoxButtonInfo[] mAbortRetryIgnoreButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Abort >", MessageBoxButton.Abort), new MessageBoxButtonInfo("< &Retry >", MessageBoxButton.Retry), new MessageBoxButtonInfo("< &Ignore >", MessageBoxButton.Ignore) };



        public static MessageBoxButton Show(WindowManager mgr, IColorScheme colors, string message, string title, MessageBoxButtonSet buttonSet)
        {
            switch (buttonSet)
            {
                case MessageBoxButtonSet.Ok:
                    return Show(mgr, colors, message, title, mOkButtons);
                case MessageBoxButtonSet.OkCancel:
                    return Show(mgr, colors, message, title, mOkCancelButtons);
                case MessageBoxButtonSet.YesCancel:
                    return Show(mgr, colors, message, title, mYesCancelButtons);
                case MessageBoxButtonSet.YesNo:
                    return Show(mgr, colors, message, title, mYesNoButtons);
                case MessageBoxButtonSet.YesNoCancel:
                    return Show(mgr, colors, message, title, mYesNoCancelButtons);
                case MessageBoxButtonSet.AbortRetryIgnore:
                    return Show(mgr, colors, message, title, mAbortRetryIgnoreButtons);
                default:
                    throw new ArgumentException("Unsupported value", "buttonSet");
            }
        }
    }
}
