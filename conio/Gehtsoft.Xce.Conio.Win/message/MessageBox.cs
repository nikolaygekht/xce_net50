using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public enum MessageBoxButton
    {
        Cancel = Dialog.DialogResultCancel,
        Ok = Dialog.DialogResultOK,
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
        public string Title { get; }

        public MessageBoxButton ID { get; }

        public MessageBoxButtonInfo(string title, MessageBoxButton ID)
        {
            Title = title;
            this.ID = ID;
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

    public static class MessageBox
    {
        private static readonly string[] mSeparators = new string[] { "\r\n" };

        public static MessageBoxButton Show(WindowManager mgr, IColorScheme colors, string message, string title, MessageBoxButtonInfo[] buttons)
        {
            int i;

            ValidateParameters(mgr, ref colors, ref message, ref title, buttons);

            string[] messageLines = message.Split(mSeparators, StringSplitOptions.None);

            int messageLinesCount = messageLines.Length;
            if (messageLinesCount > 10)
                messageLinesCount = 10;

            int buttonsWidth = 0;
            for (i = 0; i < buttons.Length; i++)
            {
                int l = Math.Max(buttons[i].Title?.Length ?? 6, 16);
                buttonsWidth += l;
            }

            //a space separator
            buttonsWidth += buttons.Length - 1;
            int splitWidth = mgr.ScreenWidth - 14;
            int dialogWidth = messageLines.Max(s => Math.Max(s.Length, splitWidth));

            if (dialogWidth < buttonsWidth)
                dialogWidth = buttonsWidth;

            int dialogHeight = messageLinesCount + 1;

            MessageBoxDialog dlg = new MessageBoxDialog(title, colors, dialogHeight + 2, dialogWidth + 2);

            for (i = 0; i < messageLinesCount; i++)
            {
                string s = messageLines[i];
                if (s.Length > splitWidth)
                    s = string.Concat(s.AsSpan(0, splitWidth - 3), "...");
                dlg.AddItem(new DialogItemLabel(s, 1000 + i, i, 0));
            }

            int buttonRow = messageLinesCount;
            int buttonPos = (dialogWidth - buttonsWidth) / 2;

            for (i = 0; i < buttons.Length; i++)
            {
                string s = buttons[i].Title ?? "< null >";
                if (s.Length > 16)
                    s = s[..16];
                dlg.AddItem(new DialogItemButton(s, (int)buttons[i].ID, buttonRow, buttonPos));
                buttonPos += s.Length + 1;
            }

            i = dlg.DoModal(mgr);

            return (MessageBoxButton)i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ValidateParameters(WindowManager mgr, ref IColorScheme colors, ref string message, ref string title, MessageBoxButtonInfo[] buttons)
        {
            if (mgr == null)
                throw new ArgumentNullException(nameof(mgr));
            colors ??= ColorScheme.White;
            message ??= "null";
            title ??= "null";
            if (buttons == null)
                throw new ArgumentNullException(nameof(buttons));
            if (buttons.Length < 1 || buttons.Length > 4)
                throw new ArgumentException("invalid length", nameof(buttons));
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    throw new ArgumentException($"null item {i}", nameof(buttons));
            }
        }

        private static readonly MessageBoxButtonInfo[] mOkButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Ok >", MessageBoxButton.Ok) };
        private static readonly MessageBoxButtonInfo[] mOkCancelButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Ok >", MessageBoxButton.Ok), new MessageBoxButtonInfo("< &Cancel >", MessageBoxButton.Cancel) };
        private static readonly MessageBoxButtonInfo[] mYesCancelButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Yes >", MessageBoxButton.Yes), new MessageBoxButtonInfo("< &Cancel >", MessageBoxButton.Cancel) };
        private static readonly MessageBoxButtonInfo[] mYesNoButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Yes >", MessageBoxButton.Yes), new MessageBoxButtonInfo("< &No >", MessageBoxButton.No) };
        private static readonly MessageBoxButtonInfo[] mYesNoCancelButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Yes >", MessageBoxButton.Yes), new MessageBoxButtonInfo("< &No >", MessageBoxButton.No), new MessageBoxButtonInfo("< &Cancel >", MessageBoxButton.Cancel) };
        private static readonly MessageBoxButtonInfo[] mAbortRetryIgnoreButtons = new MessageBoxButtonInfo[] { new MessageBoxButtonInfo("< &Abort >", MessageBoxButton.Abort), new MessageBoxButtonInfo("< &Retry >", MessageBoxButton.Retry), new MessageBoxButtonInfo("< &Ignore >", MessageBoxButton.Ignore) };

        public static MessageBoxButton Show(WindowManager mgr, IColorScheme colors, string message, string title, MessageBoxButtonSet buttonSet)
        {
            return buttonSet switch
            {
                MessageBoxButtonSet.Ok => Show(mgr, colors, message, title, mOkButtons),
                MessageBoxButtonSet.OkCancel => Show(mgr, colors, message, title, mOkCancelButtons),
                MessageBoxButtonSet.YesCancel => Show(mgr, colors, message, title, mYesCancelButtons),
                MessageBoxButtonSet.YesNo => Show(mgr, colors, message, title, mYesNoButtons),
                MessageBoxButtonSet.YesNoCancel => Show(mgr, colors, message, title, mYesNoCancelButtons),
                MessageBoxButtonSet.AbortRetryIgnore => Show(mgr, colors, message, title, mAbortRetryIgnoreButtons),
                _ => throw new ArgumentException("Unsupported value", nameof(buttonSet)),
            };
        }
    }
}
