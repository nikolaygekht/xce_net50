namespace Gehtsoft.Xce.Conio.Win.Test.Test1
{
    class MyClientArea : Window
    {
        private bool mInFocus;
        private string mLastMessage;
        private CanvasColor mWindowColor;

        internal MyClientArea(CanvasColor windowColor)
        {
            mInFocus = false;
            mWindowColor = windowColor;
            mLastMessage = "((none))";
        }

        public override void OnCreate()
        {
            Show(true);
        }

        public override void OnPaint(Canvas canvas)
        {
            canvas.Fill(0, 0, Height, Width, ' ', mWindowColor);
            if (mInFocus)
                canvas.Write(0, 0, "in focus");
            else
                canvas.Write(0, 0, "not focus");
            canvas.Write(1, 0, mLastMessage);
        }

        public override void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            mLastMessage = string.Format("key: {0} ('{1}')", ScanCodeParser.KeyCodeToName(scanCode, shift, ctrl, alt), character < 0x20 ? $"\\x{(int)character:x}" : new string(character, 1));
            Invalidate();
        }

        public override void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            WindowManager.SetFocus(this);
            mLastMessage = string.Format("lbd: {0} {1}", row, column);
        }

        public override void OnMouseMove(int row, int column, bool shift, bool ctrl, bool alt, bool leftButton, bool rightButton)
        {
            mLastMessage = string.Format("mm: {0} {1}", row, column);
            Invalidate();
        }

        public override void OnMouseRButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            MyModal1 dlg = new MyModal1(this, Program.CurrentSheme.DialogBorder);
            WindowManager.CreateModal(dlg, 20, 20, 20, 20);
            dlg.Show(true);
            dlg.SetFocus();
        }

        public override void OnSetFocus()
        {
            mInFocus = true;
            WindowManager.SetCaretPos(this, 0, 0);
            WindowManager.ShowCaret(true);
            Invalidate();
        }

        bool mOnClose = false;
        public override void OnClose()
        {
            if (mOnClose)
                return;
            mOnClose = true;
            if (mInFocus)
                WindowManager.ShowCaret(false);
            WindowManager.Close(Parent);
        }

        public override void OnSizeChanged()
        {
        }

        public override void  OnKillFocus()
        {
            mInFocus = false;
            WindowManager.ShowCaret(false);
            Invalidate();
        }
    }
}
