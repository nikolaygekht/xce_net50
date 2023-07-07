using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public class WindowManager : IDisposable, IConsoleInputListener
    {
        private readonly LinkedList<Window> mTopLevelWindows = new LinkedList<Window>();
        private Canvas mScreenCanvas;
        private IConsoleOutput mConsoleOutput;
        private IConsoleInput mConsoleInput;
        private bool mForceRedraw = false;
        private Window mFocusWindow;
        private readonly LinkedList<Window> mModalStack = new LinkedList<Window>();
        private Window mCaptureMouseWindow = null;
        private Window mCaretWindow = null;
        private int mCaretRow, mCaretColumn;
        private int mLayoutCode;
        private KeyboardLayout mLayout = null;
        private readonly static KeyboardLayouts mLayouts = new KeyboardLayouts();
        private readonly static CanvasColor mDefaultColor = new CanvasColor(0x03);

        public int ScreenHeight => mConsoleOutput.VisibleRows;

        public int ScreenWidth => mConsoleOutput.VisibleColumns;

        public KeyboardLayout KeyboardLayout
        {
            get
            {
                if (mLayout == null || mLayout.LayoutCode != mLayoutCode)
                    mLayout = mLayouts[mLayoutCode];
                return mLayout;
            }
        }

        #region constructor/destuctor
        public WindowManager(IConsoleOutput output, IConsoleInput input)
        {
            mConsoleOutput = output;
            mScreenCanvas = new Canvas(mConsoleOutput.VisibleRows, mConsoleOutput.VisibleColumns);
            mConsoleInput = input;
            mLayoutCode = mConsoleInput.CurrentLayout;
            ShowCaret(false);
        }

        ~WindowManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            foreach (Window window in mTopLevelWindows)
                window.DoClose();

            mTopLevelWindows.Clear();

            mConsoleInput = null;

            mConsoleOutput = null;

            mScreenCanvas = null;
        }
        #endregion

        #region window management
        /// <summary>
        /// Create new window
        /// </summary>
        /// <param name="window">window object</param>
        /// <param name="parent">parent window or null to create a top-level windiow</param>
        /// <param name="row">row of top-left corner</param>
        /// <param name="column">column of top-left corner</param>
        /// <param name="height">height of the window</param>
        /// <param name="width">width of the window</param>
        public void Create(Window window, Window parent, int row, int column, int height, int width)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            window.Manager = this;
            window.Create(parent);
            window.Move(row, column, height, width);
            if (parent == null)
                mTopLevelWindows.AddLast(window);
        }

        public void DoModal(Window window, int row, int column, int height, int width)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            CreateModal(window, row, column, height, width);
            Window focus = mFocusWindow;
            focus?.OnKillFocus();
            window.Show(true);
            SetFocus(window);
            while (window.Exists)
                PumpMessage(-1);
            mFocusWindow = focus;
            mFocusWindow?.OnSetFocus();
        }

        public void ShowPopupMenu(PopupMenu menu, int row, int column)
        {
            menu.DoLayout(out int height, out int width);
            DoModal(menu, row, column, height, width);
        }

        public void CreateModal(Window window, int row, int column, int height, int width)
        {
            mModalStack.AddLast(window);
            Create(window, null, row, column, height, width);
        }

        public void Close(Window window)
        {
            if (window == null)
                throw new ArgumentNullException(nameof(window));
            if (window.Exists)
            {
                if (window.Parent == null)
                {
                    LinkedListNode<Window> _window = mTopLevelWindows.Find(window);
                    if (_window != null)
                        mTopLevelWindows.Remove(_window);
                    _window = mModalStack.Find(window);
                    if (_window != null)
                        mModalStack.Remove(_window);
                    mForceRedraw = true;
                }
                window.DoClose();
            }
            if (mFocusWindow == window)
                mFocusWindow = null;
            if (mCaptureMouseWindow == window)
                mCaptureMouseWindow = null;
        }

        /// <summary>
        /// Set focus to a window
        /// </summary>
        /// <param name="window"></param>
        public void SetFocus(Window window)
        {
            if (window != null && mModalStack.Count > 0)
            {
                Window lastModal = mModalStack.Last.Value;
                if (window != lastModal && !window.HasParent(lastModal))
                    return;
            }

            mFocusWindow?.OnKillFocus();
            mFocusWindow = window;
            mFocusWindow?.OnSetFocus();
        }

        public Window GetFocus()
        {
            return mFocusWindow;
        }

        public bool CaptureMouse(Window window)
        {
            if (mCaptureMouseWindow != null)
                return false;
            mCaptureMouseWindow = window;
            return true;
        }

        public void ReleaseMouse(Window window)
        {
            if (mCaptureMouseWindow == window)
                mCaptureMouseWindow = null;
        }

        /// <summary>
        /// Get window by position
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public Window WindowFromPos(int row, int column, bool visibleOnly)
        {
            for (LinkedListNode<Window> n = mTopLevelWindows.Last; n != null; n = n.Previous)
            {
                Window window = n.Value;
                if (visibleOnly && !window.Visible)
                    continue;
                if (window.ScreenToWindow(row, column, out int windowRow, out int windowColumn))
                    return window.ChildFromPos(windowRow, windowColumn, visibleOnly);
            }
            return null;
        }

        /// <summary>
        /// Get window by position
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public Window WindowFromPos(int row, int column)
        {
            return WindowFromPos(row, column, false);
        }

        public void BringToFront(Window window)
        {
            if (window.Parent == null)
            {
                LinkedListNode<Window> _window = mTopLevelWindows.Find(window);
                if (_window != null)
                {
                    mTopLevelWindows.Remove(_window);
                    mTopLevelWindows.AddLast(window);
                    mForceRedraw = true;
                }
            }
            else
            {
                window.Parent.BringToFront(window);
            }
        }
        #endregion

        #region message
        public void PumpMessage(int timeout)
        {
            int layout = mConsoleInput.CurrentLayout;

            if (layout != mLayoutCode)
            {
                mLayoutCode = layout;
                mFocusWindow?.OnKeyboardLayoutChanged();
            }

            bool paint = false;

            if (mForceRedraw)
                paint = true;
            else
                paint = mTopLevelWindows.Any(w => !w.Valid && w.Visible);

            if (paint)
            {
                RedrawScreen();
                mForceRedraw = false;
            }

            UpdateCaretPos();
            mConsoleInput.Read(this, timeout);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RedrawScreen()
        {
            mConsoleOutput.UpdateSize();
            if (mScreenCanvas.Rows != mConsoleOutput.VisibleRows || mScreenCanvas.Columns != mConsoleOutput.VisibleColumns)
                mScreenCanvas = new Canvas(mConsoleOutput.VisibleRows, mConsoleOutput.VisibleColumns);
            mScreenCanvas.Fill(0, 0, mScreenCanvas.Rows, mScreenCanvas.Columns, ' ', mDefaultColor);
            foreach (Window window in mTopLevelWindows)
            {
                if (window.Visible)
                {
                    window.Paint();
                    Canvas canvas = window.Canvas;
                    if (canvas != null)
                        mScreenCanvas.Paint(window.Row, window.Column, canvas);
                }
            }
            mConsoleOutput.PaintCanvasToScreen(mScreenCanvas);
        }
        #endregion

        #region input events
        /// <summary>
        /// Enable shortcuts Alt-Space-C (close)
        ///                  Alt-Space-N (next)
        /// </summary>
        public bool WindowKeyboardShortcutsEnabled { get; set; } = false;

        private bool mAltSpace = false;

        public void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (WindowKeyboardShortcutsEnabled && scanCode == ScanCode.SPACE && !shift && !ctrl && alt)
            {
                mAltSpace = true;
                return;
            }

            if (mFocusWindow != null)
                OnKeyPressed_FocusWindowAltSpace(scanCode, character, shift, ctrl, alt);

            mAltSpace = false;

            int layout = mConsoleInput.CurrentLayout;
            if (layout != mLayoutCode)
            {
                mLayoutCode = layout;
                mFocusWindow?.OnKeyboardLayoutChanged();
            }
        }

        private void OnKeyPressed_FocusWindowAltSpace(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            if (!WindowKeyboardShortcutsEnabled || !mAltSpace)
            {
                mFocusWindow?.OnKeyPressed(scanCode, character, shift, ctrl, alt);
                return;
            }

            bool handled = false;
            bool noModifiers = !ctrl && !shift && !alt;

            if (scanCode == ScanCode.C && noModifiers)
            {
                Close(mFocusWindow);
                handled = true;
            }
            else if (scanCode == ScanCode.N && noModifiers)
            {
                OnKeyPressed_FocusWindowAltSpace_Next();
                handled = true;
            }
            else if (scanCode == ScanCode.UP || scanCode == ScanCode.DOWN || scanCode == ScanCode.LEFT || scanCode == ScanCode.RIGHT && !ctrl && !shift)
            {
                OnKeyPressed_FocusWindowAltSpace_MoveOrSize(scanCode, alt);
                handled = true;
            }

            if (!handled)
                mFocusWindow?.OnKeyPressed(scanCode, character, shift, ctrl, alt);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnKeyPressed_FocusWindowAltSpace_Next()
        {
            if (!mModalStack.Contains(mFocusWindow) && mTopLevelWindows.Count > 1)
            {
                for (var node = mTopLevelWindows.First; node != null; node = node.Next)
                {
                    Window topLevelWindow = node.Value;
                    if (topLevelWindow == mFocusWindow || topLevelWindow.Contains(mFocusWindow, true))
                    {
                        node = node.Next ?? mTopLevelWindows.First;
                        SetFocus(node.Value);
                        break;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnKeyPressed_FocusWindowAltSpace_MoveOrSize(ScanCode scanCode, bool alt)
        {
            Window window = mFocusWindow;

            while (window != null)
            {
                if (window is WindowBorderContainer)
                    break;
                window = window.Parent;
            }

            if (window == null)
                return;

            if (alt)
                OnKeyPressed_FocusWindowAltSpace_Size(scanCode, window);
            else
                OnKeyPressed_FocusWindowAltSpace_Move(scanCode, window);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnKeyPressed_FocusWindowAltSpace_Move(ScanCode scanCode, Window window)
        {
            if (scanCode == ScanCode.UP && window.Row > 0)
                window.Move(window.Row - 1, window.Column, window.Height, window.Width);
            else if (scanCode == ScanCode.DOWN && window.Row + window.Height < (window.Parent == null ? ScreenHeight : window.Parent.Height))
                window.Move(window.Row + 1, window.Column, window.Height, window.Width);
            else if (scanCode == ScanCode.LEFT && window.Column > 0)
                window.Move(window.Row, window.Column - 1, window.Height, window.Width);
            else if (scanCode == ScanCode.RIGHT && window.Column + window.Width < (window.Parent == null ? ScreenWidth : window.Parent.Width))
                window.Move(window.Row, window.Column + 1, window.Height, window.Width);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnKeyPressed_FocusWindowAltSpace_Size(ScanCode scanCode, Window window)
        {
            if (scanCode == ScanCode.UP && window.Height > 3)
                window.Move(window.Row, window.Column, window.Height - 1, window.Width);
            else if (scanCode == ScanCode.DOWN && window.Row + window.Height < ScreenHeight)
                window.Move(window.Row, window.Column, window.Height + 1, window.Width);
            else if (scanCode == ScanCode.LEFT && window.Width > 10)
                window.Move(window.Row, window.Column, window.Height, window.Width - 1);
            else if (scanCode == ScanCode.RIGHT && window.Column + window.Width < ScreenWidth)
                window.Move(window.Row, window.Column, window.Height, window.Width + 1);
        }

        public void OnKeyReleased(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            int layout = mConsoleInput.CurrentLayout;
            if (layout != mLayoutCode)
            {
                mLayoutCode = layout;
                mFocusWindow?.OnKeyboardLayoutChanged();
            }
        }

        public void OnMouseMove(int row, int column, bool shift, bool ctrl, bool alt, bool lb, bool rb)
        {
            if (mCaptureMouseWindow != null)
            {
                mCaptureMouseWindow.OnMouseMove(row, column, shift, ctrl, alt, lb, rb);
                return;
            }

            Window window = WindowFromPos(row, column, true);
            if (window != null)
            {
                window.ScreenToWindow(row, column, out int windowRow, out int windowColumn);
                window.OnMouseMove(windowRow, windowColumn, shift, ctrl, alt, lb, rb);
            }
        }

        public void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (mCaptureMouseWindow != null)
            {
                mCaptureMouseWindow.OnMouseLButtonDown(row, column, shift, ctrl, alt);
                return;
            }

            Window window = WindowFromPos(row, column, true);
            if (window != null)
            {
                window.ScreenToWindow(row, column, out int windowRow, out int windowColumn);
                window.OnMouseLButtonDown(windowRow, windowColumn, shift, ctrl, alt);
            }
        }

        public void OnMouseLButtonUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (mCaptureMouseWindow != null)
            {
                mCaptureMouseWindow.OnMouseLButtonUp(row, column, shift, ctrl, alt);
                return;
            }

            Window window = WindowFromPos(row, column, true);
            if (window != null)
            {
                window.ScreenToWindow(row, column, out int windowRow, out int windowColumn);
                window.OnMouseLButtonUp(windowRow, windowColumn, shift, ctrl, alt);
            }
        }

        public void OnMouseRButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (mCaptureMouseWindow != null)
            {
                mCaptureMouseWindow.OnMouseRButtonDown(row, column, shift, ctrl, alt);
                return;
            }

            Window window = WindowFromPos(row, column, true);
            if (window != null)
            {
                window.ScreenToWindow(row, column, out int windowRow, out int windowColumn);
                window.OnMouseRButtonDown(windowRow, windowColumn, shift, ctrl, alt);
            }
        }

        public void OnMouseRButtonUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (mCaptureMouseWindow != null)
            {
                mCaptureMouseWindow.OnMouseRButtonUp(row, column, shift, ctrl, alt);
                return;
            }

            Window window = WindowFromPos(row, column, true);
            if (window != null)
            {
                window.ScreenToWindow(row, column, out int windowRow, out int windowColumn);
                window.OnMouseRButtonUp(windowRow, windowColumn, shift, ctrl, alt);
            }
        }

        public void OnMouseWheelUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (mFocusWindow != null)
            {
                mFocusWindow.ScreenToWindow(row, column, out int windowRow, out int windowColumn);
                mFocusWindow.OnMouseWheelUp(windowRow, windowColumn, shift, ctrl, alt);
            }
        }

        public void OnMouseWheelDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (mFocusWindow != null)
            {
                mFocusWindow.ScreenToWindow(row, column, out int windowRow, out int windowColumn);
                mFocusWindow.OnMouseWheelDown(windowRow, windowColumn, shift, ctrl, alt);
            }
        }

        public void OnGetFocus(bool shift, bool ctrl, bool alt)
        {
            int layout = mConsoleInput.CurrentLayout;
            if (layout != mLayoutCode)
            {
                mLayoutCode = layout;
                mFocusWindow?.OnKeyboardLayoutChanged();
            }
        }

        public void OnScreenBufferChanged(int rows, int columns)
        {
            mConsoleOutput.UpdateSize();
            mScreenCanvas = new Canvas(mConsoleOutput.VisibleRows, mConsoleOutput.VisibleColumns);
            foreach (Window window in mTopLevelWindows)
                window.OnScreenSizeChanged(rows, columns);
        }
        #endregion

        #region caret
        public void SetCaretPos(Window caretWindow, int row, int column)
        {
            mCaretWindow = caretWindow;
            mCaretRow = row;
            mCaretColumn = column;
            UpdateCaretPos();
        }

        private void UpdateCaretPos()
        {
            mConsoleOutput.Cursor.GetCursorPosition(out int crow, out int ccolumn);
            int row, column;
            if (mCaretWindow?.Exists == true)
                mCaretWindow.WindowToScreen(mCaretRow, mCaretColumn, out row, out column);
            else
            {
                row = mCaretRow;
                column = mCaretColumn;
            }
            if (crow != row || ccolumn != column)
                mConsoleOutput.Cursor.SetCursorPosition(row, column);
        }

        public void ShowCaret(bool show)
        {
            mConsoleOutput.Cursor.CursorVisible = show;
        }

        public void SetCaretType(int caretSize, bool show)
        {
            mConsoleOutput.Cursor.CursorSize = caretSize;
            mConsoleOutput.Cursor.CursorVisible = show;
        }

        public void OnIdle()
        {
            foreach (var window in mTopLevelWindows)
                window.OnIdle();
        }
        #endregion
    }
}
