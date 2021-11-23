using System;
using System.Collections.Generic;
using System.Text;
using Gehtsoft.Xce.Conio;

namespace Gehtsoft.Xce.Conio.Win
{
    public class DialogItemSingleLineTextBox : DialogItem
    {
        protected StringBuilder mText;
        protected int mOffset;
        protected int mCaret;
        protected int mSelectionStart;
        protected int mSelectionEnd;
        protected int mSelectionFirstPosition;
        protected bool mEnabled;
        protected bool mInFocus;
        protected bool mInsertMode;
        protected bool mReadonly;

        public string Text
        {
            get
            {
                return mText.ToString();
            }
            set
            {
                SetText(value);
            }
        }

        public override bool Enabled
        {
            get
            {
                return mEnabled;
            }
        }

        public bool Readonly
        {
            get
            {
                return mReadonly;
            }
            set
            {
                mReadonly = value;
            }
        }

        public override bool IsInputElement
        {
            get
            {
                return true;
            }
        }

        protected virtual int EditWidth
        {
            get
            {
                return Width;
            }
        }


        public DialogItemSingleLineTextBox(string text, int id, int row, int column, int width) : base(id, row, column, 1, width)
        {
            mText = new StringBuilder(text);
            mOffset = 0;
            mCaret = 0;
            mSelectionStart = -1;
            mSelectionEnd = -1;
            mEnabled = true;
            mInsertMode = true;
            mReadonly = false;
        }

        public void Enable(bool enable)
        {
            if (mEnabled != enable && Exists)
                Invalidate();
            mEnabled = enable;
        }

        public void SetText(string text)
        {
            mOffset = 0;
            mCaret = 0;
            mSelectionEnd = mSelectionStart = -1;
            mText = new StringBuilder(text);
            if (mInFocus)
                Manager.SetCaretPos(this, 0, mCaret - mOffset);
            if (Exists)
                Invalidate();
            if (Dialog != null)
                Dialog.OnItemChanged(this);
        }

        public void SetSel(int from, int to)
        {
            mSelectionStart = from;
            mSelectionEnd = to;
            mCaret = to;
            if (EditWidth > 0)
            {
                if (mCaret - mOffset >= EditWidth)
                    mOffset = mCaret - EditWidth + 1;
            }
        }


        public override void OnCreate()
        {
            mInFocus = false;
        }

        public override void OnSetFocus()
        {
            mInFocus = true;
            Manager.SetCaretType(mInsertMode ? 12 : 50, true);
            Manager.SetCaretPos(this, 0, mCaret - mOffset);
            Invalidate();
        }

        public override void OnKillFocus()
        {
            mInFocus = false;
            Manager.SetCaretType(12, false);
            Invalidate();
        }

        public override void OnPaint(Canvas canvas)
        {
            CanvasColor color, selColor;

            if (Enabled)
            {
                if (mInFocus)
                {
                    color = Dialog.Colors.DialogItemEditColorFocused;
                    selColor = Dialog.Colors.DialogItemEditSelectionFocused;
                }
                else
                {
                    color = Dialog.Colors.DialogItemEditColor;
                    selColor = Dialog.Colors.DialogItemEditSelection;
                }
            }
            else
            {
                color = Dialog.Colors.DialogItemEditColorDisabled;
                selColor = color;
            }

            canvas.Fill(0, 0, 1, EditWidth, ' ', color);
            if (mOffset < mText.Length)
                canvas.Write(0, 0, mText, mOffset, mText.Length - mOffset, color);
            if (mSelectionStart >= 0 && Enabled)
                canvas.Fill(0, mSelectionStart - mOffset, 1, mSelectionEnd - mSelectionStart, selColor);
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (!Dialog.PretranslateOnKeyPressed(scanCode, character, shift, ctrl, alt))
            {
                if (!ctrl && !alt && scanCode == ScanCode.LEFT)
                    Left(shift);
                if (!ctrl && !alt && scanCode == ScanCode.RIGHT)
                    Right(shift);
                if (!ctrl && !alt && scanCode == ScanCode.HOME)
                    Home(shift);
                if (!ctrl && !alt && scanCode == ScanCode.END)
                    End(shift);
                if (!ctrl && !alt && !shift && scanCode == ScanCode.DEL)
                    Delete();
                if (ctrl && !alt && !shift && scanCode == ScanCode.DEL)
                    DeleteToEnd();
                if (!ctrl && !alt && !shift && scanCode == ScanCode.BACK)
                    Backspace();
                if (ctrl && !alt && !shift && scanCode == ScanCode.BACK)
                    BackspaceToBegin();
                if (!ctrl && !alt && !shift && scanCode == ScanCode.INSERT)
                    ToggleInsert();
                if (ctrl && !alt && !shift && scanCode == ScanCode.X)
                    Cut();
                if (ctrl && !alt && !shift && scanCode == ScanCode.C)
                    Copy();
                if (ctrl && !alt && !shift && scanCode == ScanCode.V)
                    Paste();
                if (ctrl && !alt && !shift && scanCode == ScanCode.INSERT)
                    Copy();
                if (!ctrl && !alt && shift && scanCode == ScanCode.INSERT)
                    Paste();

                if (!ctrl && !alt && character >= ' ')
                    Type(character);
            }
        }



        private void Left(bool select)
        {
            if (select && mSelectionStart == -1)
                mSelectionFirstPosition = mCaret;
            if (mCaret > 0)
            {
                mCaret--;
                if (mCaret < mOffset)
                    mOffset = mCaret;
                Manager.SetCaretPos(this, 0, mCaret - mOffset);
                Invalidate();

                if (select)
                    UpdateSelect();
                else
                    mSelectionStart = mSelectionEnd = -1;
            }
        }

        private void Right(bool select)
        {
            if (select && mSelectionStart == -1)
                mSelectionFirstPosition = mCaret;

            mCaret++;
            if (mCaret - mOffset >= EditWidth)
                mOffset = mCaret - EditWidth + 1;
            Manager.SetCaretPos(this, 0, mCaret - mOffset);
            Invalidate();

            if (select)
                UpdateSelect();
            else
                mSelectionStart = mSelectionEnd = -1;
        }

        private void UpdateSelect()
        {
            if (mCaret < mSelectionFirstPosition)
            {
                mSelectionStart = mCaret;
                mSelectionEnd = mSelectionFirstPosition;
            }
            else
            {
                mSelectionStart = mSelectionFirstPosition;
                mSelectionEnd = mCaret;
            }
            if (mSelectionStart == mSelectionEnd)
            {
                mSelectionStart = mSelectionEnd = -1;
            }
        }

        private void Home(bool select)
        {
            if (select && mSelectionStart == -1)
                mSelectionFirstPosition = mCaret;

            mCaret = 0;
            if (mCaret < mOffset)
                mOffset = 0;

            Manager.SetCaretPos(this, 0, mCaret - mOffset);
            Invalidate();

            if (select)
                UpdateSelect();
            else
                mSelectionStart = mSelectionEnd = -1;
        }

        private void End(bool select)
        {
            if (select && mSelectionStart == -1)
                mSelectionFirstPosition = mCaret;

            mCaret = mText.Length;
            if (mCaret - mOffset >= EditWidth)
                mOffset = mCaret - EditWidth + 1;
            Manager.SetCaretPos(this, 0, mCaret - mOffset);
            Invalidate();

            if (select)
                UpdateSelect();
            else
                mSelectionStart = mSelectionEnd = -1;
        }

        private void Delete()
        {
            if (Readonly)
                return ;
            if (mSelectionStart >= 0)
            {
                bool c = false;
                if (mSelectionEnd >= mText.Length)
                    mSelectionEnd = mText.Length;
                if (mSelectionStart < mSelectionEnd)
                {
                    mText.Remove(mSelectionStart, mSelectionEnd - mSelectionStart);
                    c = true;
                }
                Invalidate();
                mCaret = mSelectionStart;
                if (mCaret < mOffset)
                    mOffset = mCaret;
                if (mCaret - mOffset >= EditWidth)
                    mOffset = mCaret - EditWidth + 1;
                Manager.SetCaretPos(this, 0, mCaret - mOffset);
                mSelectionStart = mSelectionEnd = -1;
                OnTextChangedByUser();
                if (c && Dialog != null)
                    Dialog.OnItemChanged(this);

            }
            else
            {
                if (mCaret < mText.Length)
                {
                    mText.Remove(mCaret, 1);
                    if (Dialog != null)
                        Dialog.OnItemChanged(this);
                    OnTextChangedByUser();
                }
                Invalidate();
            }
            Manager.SetCaretPos(this, 0, mCaret - mOffset);
        }

        private void DeleteToEnd()
        {
            if (Readonly)
                return;

            if (mCaret < mText.Length)
            {
                mText.Remove(mCaret, mText.Length - mCaret);
                if (Dialog != null)
                    Dialog.OnItemChanged(this);
                OnTextChangedByUser();
            }
            Invalidate();
        }

        private void Backspace()
        {
            if (Readonly)
                return;

            if (mSelectionStart >= 0)
            {
                if (mSelectionEnd >= mText.Length)
                    mSelectionEnd = mText.Length;
                mText.Remove(mSelectionStart, mSelectionEnd - mSelectionStart);
                Invalidate();
                mCaret = mSelectionStart;
                if (mCaret < mOffset)
                    mOffset = mCaret;
                if (mCaret - mOffset >= EditWidth)
                    mOffset = mCaret - EditWidth + 1;
                Manager.SetCaretPos(this, 0, mCaret - mOffset);
                mSelectionStart = mSelectionEnd = -1;
                if (Dialog != null)
                    Dialog.OnItemChanged(this);

                OnTextChangedByUser();
            }
            else
            {
                if (mCaret > 0)
                {
                    bool c = false;
                    if (mText.Length > mCaret - 1)
                    {
                        mText.Remove(mCaret - 1, 1);
                        c = true;
                    }
                    mCaret--;
                    if (mCaret < mOffset)
                        mOffset = mCaret;
                    Invalidate();
                    if (c && Dialog != null)
                        Dialog.OnItemChanged(this);
                    OnTextChangedByUser();
                }
            }
            Manager.SetCaretPos(this, 0, mCaret - mOffset);
        }

        private void BackspaceToBegin()
        {
            if (Readonly)
                return;

            bool c = false;
            if (mSelectionStart >= 0)
            {
                if (mSelectionEnd >= mText.Length)
                    mSelectionEnd = mText.Length;
                mText.Remove(mSelectionStart, mSelectionEnd - mSelectionStart);
                c = true;
                Invalidate();
                mCaret = mSelectionStart;
                if (mCaret < mOffset)
                    mOffset = mCaret;
                if (mCaret - mOffset >= EditWidth)
                    mOffset = mCaret - EditWidth + 1;
                Manager.SetCaretPos(this, 0, mCaret - mOffset);
                mSelectionStart = mSelectionEnd = -1;
            }
            if (mCaret > 0)
            {
                mText.Remove(0, mCaret);
                c = true;
                mCaret = 0;
                if (mCaret < mOffset)
                    mOffset = mCaret;
                Invalidate();
                if (Dialog != null)
                    Dialog.OnItemChanged(this);

            }
            if (c && Dialog != null)
                Dialog.OnItemChanged(this);
            OnTextChangedByUser();
            Manager.SetCaretPos(this, 0, mCaret - mOffset);
        }

        private void ToggleInsert()
        {
            mInsertMode = !mInsertMode;
            Manager.SetCaretType(mInsertMode ? 12 : 50, true);
        }

        private void Type(char c)
        {
            if (Readonly)
                return;

            bool ch = false;
            if (mSelectionStart >= 0)
            {
                ch = true;
                Delete();
            }

            if (mCaret >= mText.Length)
            {
                int cc = mCaret - mText.Length + (mInsertMode ? 0 : 1);
                mText.Append(' ', cc);
                ch = true;
            }

            if (mInsertMode)
                mText.Insert(mCaret, c);
            else
                mText[mCaret] = c;
            ch = true;
            mCaret++;
            if (mCaret - mOffset >= EditWidth)
                mOffset = mCaret - EditWidth + 1;
            Manager.SetCaretPos(this, 0, mCaret - mOffset);
            Invalidate();
            if (ch && Dialog != null)
                Dialog.OnItemChanged(this);
            OnTextChangedByUser();
        }

        private void Cut()
        {
            if (Readonly)
                return;

            if (mSelectionStart >= 0)
            {
                Copy();
                Delete();
            }
        }

        private void Copy()
        {
            if (mSelectionStart >= 0)
            {
                if (mSelectionEnd >= mText.Length)
                    mSelectionEnd = mText.Length;
                string selectionText = mText.ToString().Substring(mSelectionStart, mSelectionEnd - mSelectionStart);
                TextClipboardFactory.Clipboard.SetText(selectionText);
            }
        }

        private void Paste()
        {
            if (Readonly)
                return;

            string clipText = TextClipboardFactory.Clipboard.GetText(TextClipboardFormat.UnicodeText);
            if (clipText != null)
            {
                if (mSelectionStart >= 0)
                {
                    Delete();
                }
                if (mCaret >= mText.Length)
                {
                    int cc = mCaret - mText.Length;
                    mText.Append(' ', cc);
                }
                mText.Insert(mCaret, clipText);
                mCaret += clipText.Length;
                if (mCaret - mOffset >= EditWidth)
                    mOffset = mCaret - EditWidth + 1;
                Manager.SetCaretPos(this, 0, mCaret - mOffset);
                Invalidate();
                if (Dialog != null)
                    Dialog.OnItemChanged(this);
                OnTextChangedByUser();
            }
        }

        public override void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (Enabled)
            {
                mSelectionStart = mSelectionEnd = -1;
                mCaret = mOffset + column;
                Manager.SetCaretPos(this, 0, mCaret - mOffset);
                if (!mInFocus)
                    Manager.SetFocus(this);
                mSelectionFirstPosition = mCaret;
                Invalidate();
            }
        }

        public override void OnMouseMove(int row, int column, bool shift, bool ctrl, bool alt, bool leftButton, bool rightButton)
        {
            if (leftButton && mInFocus && Enabled)
            {
                mCaret = mOffset + column;
                Manager.SetCaretPos(this, 0, mCaret - mOffset);
                UpdateSelect();
                Invalidate();

            }
        }

        protected virtual void OnTextChangedByUser()
        {
        }
    }
}
