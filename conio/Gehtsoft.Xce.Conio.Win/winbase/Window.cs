using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    public class Window : IDisposable
    {
        #region Constructor/Destructor
        private bool mExists;

        internal WindowManager Manager { get; set; }

        public WindowManager WindowManager => Manager;

        public bool Exists => mExists;

        /// <summary>
        /// Constructor
        /// </summary>
        public Window()
        {
            mExists = false;
            mRow = mColumn = mWidth = mHeight = 0;
            mVisible = false;
            mParent = null;
            mValid = false;
            mCanvas = null;
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Window()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose overridable
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (mExists)
                DoClose();
        }
        #endregion

        #region Parent/Child relationship
        private readonly LinkedList<Window> mChildren = new LinkedList<Window>();

        /// <summary>
        /// Get a parent window
        /// </summary>
        public Window Parent => mParent;

        private Window mParent;

        public bool Contains(Window win, bool inDepth)
        {
            foreach (var child in mChildren)
            {
                if (child == win)
                    return true;
                if (inDepth && child.Contains(win, true))
                    return true;
            }
            return false;
        }

        internal bool HasParent(Window parent)
        {
            if (Parent == null)
                return false;
            if (parent == Parent)
                return true;
            return Parent.HasParent(parent);
        }

        internal void BringToFront(Window window)
        {
            LinkedListNode<Window> _window = mChildren.Find(window);
            if (_window != null)
            {
                mChildren.Remove(_window);
                mChildren.AddLast(window);
                Invalidate();
                Manager.BringToFront(this);
            }
        }

        /// <summary>
        /// Get a list of children windows
        /// </summary>
        public IEnumerable<Window> Children => mChildren;

        /// <summary>
        /// Create a window
        /// </summary>
        internal void Create(Window parent)
        {
            mParent = parent;
            if (Parent != null)
                Parent.mChildren.AddLast(this);
            mExists = true;
            OnCreate();
        }

        public virtual void OnCreate()
        {
        }

        public virtual void OnClose()
        {
        }

        /// <summary>
        /// close window and free all resources
        /// </summary>
        internal void DoClose()
        {
            OnClose();

            while (mChildren.First != null)
                Manager.Close(mChildren.First.Value);

            if (Parent != null)
            {
                LinkedListNode<Window> _this = Parent.mChildren.Find(this);
                if (_this != null)
                    Parent.mChildren.Remove(_this);
                Parent.Invalidate();
                mParent = null;
            }

            mCanvas = null;
            mExists = false;
        }

        public void Close()
        {
            WindowManager.Close(this);
        }
        public void SetFocus()
        {
            WindowManager.SetFocus(this);
        }
        #endregion

        #region Position and visibility
        private int mRow, mColumn, mWidth, mHeight;
        private bool mVisible;

        public int Row => mRow;

        public int Column => mColumn;

        public int Height => mHeight;

        public int Width => mWidth;

        public bool Visible => mVisible;

        public virtual void OnSizeChanged()
        {
        }

        /// <summary>
        /// Move a window
        /// </summary>
        /// <param name="row">new line for top-left corner</param>
        /// <param name="column">new column for top-left corner</param>
        /// <param name="height">new height</param>
        /// <param name="width">new width</param>
        public void Move(int row, int column, int height, int width)
        {
            if (height < 0)
                throw new ArgumentException(Resources.ArgumentMustNotBeNegative, nameof(height));
            if (width < 0)
                throw new ArgumentException(Resources.ArgumentMustNotBeNegative, nameof(width));
            mRow = row;
            mColumn = column;
            if (mHeight != height || mWidth != width)
            {
                mCanvas = null;
            }
            mHeight = height;
            mWidth = width;
            Invalidate();
            OnSizeChanged();
        }

        public virtual void OnShow(bool visible)
        {
        }

        /// <summary>
        /// Show or hide the window
        /// </summary>
        /// <param name="visible"></param>
        public void Show(bool visible)
        {
            mVisible = visible;
            if (visible)
                Invalidate();
            OnShow(visible);
        }
        #endregion

        #region Drawing
        private bool mValid;
        private Canvas mCanvas;

        /// <summary>
        /// Returns the flag indicating whether the window is valid
        /// </summary>
        public bool Valid => mValid;

        internal Canvas Canvas => mCanvas;

        /// <summary>
        /// Invalidate the whole window
        /// </summary>
        virtual public void Invalidate()
        {
            mValid = false;
            Parent?.Invalidate();
        }

        private static readonly CanvasColor mDefaultColor = new CanvasColor(0x03);

        public virtual void OnPaint(Canvas canvas)
        {
            canvas.Fill(0, 0, mHeight, mWidth, ' ', mDefaultColor);
        }

        /// <summary>
        /// Force window re-paint
        /// </summary>
        internal void Paint()
        {
            if (mValid)
                return;

            if (mWidth == 0 || mHeight == 0)
            {
                mValid = true;
                return;
            }

            if (mCanvas == null)
                mCanvas = new Canvas(mHeight, mWidth);

            OnPaint(mCanvas);

            foreach (Window child in mChildren.Where(child => child.Visible && child.Exists))
            {
                if (!child.Valid)
                    child.Paint();
                if (child.Canvas != null)
                    mCanvas.Paint(child.Row, child.Column, child.Canvas);
            }
            mValid = true;
        }
        #endregion

        #region hit tests
        /// <summary>
        /// Convert parent window coordintates into this window coordinates
        /// </summary>
        /// <param name="parentRow"></param>
        /// <param name="parentColumn"></param>
        /// <param name="windowRow"></param>
        /// <param name="windowColumn"></param>
        /// <returns></returns>
        public bool ParentToWindow(int parentRow, int parentColumn, out int windowRow, out int windowColumn)
        {
            windowRow = parentRow - mRow;
            windowColumn = parentColumn - mColumn;
            return windowRow >= 0 && windowRow < mHeight && windowColumn >= 0 && windowColumn < mWidth;
        }

        /// <summary>
        /// Converets window coordinates into parent coordinates
        /// </summary>
        /// <param name="windowRow"></param>
        /// <param name="windowColumn"></param>
        /// <param name="parentRow"></param>
        /// <param name="parentColumn"></param>
        public void WindowToParent(int windowRow, int windowColumn, out int parentRow, out int parentColumn)
        {
            parentRow = windowRow + mRow;
            parentColumn = windowColumn + mColumn;
        }

        /// <summary>
        /// Conver screen coordinates to window coordinates
        /// </summary>
        /// <param name="screenRow"></param>
        /// <param name="screenColumn"></param>
        /// <param name="windowRow"></param>
        /// <param name="windowColumn"></param>
        /// <returns>true if coordinates matches the window</returns>
        public bool ScreenToWindow(int screenRow, int screenColumn, out int windowRow, out int windowColumn)
        {
            if (Parent == null)
                return ParentToWindow(screenRow, screenColumn, out windowRow, out windowColumn);
            else
            {
                Parent.ScreenToWindow(screenRow, screenColumn, out int parentRow, out int parentColumn);
                return ParentToWindow(parentRow, parentColumn, out windowRow, out windowColumn);
            }
        }

        /// <summary>
        /// Convert window coordinates to the screen coordinates.
        /// </summary>
        /// <param name="windowRow"></param>
        /// <param name="windowColumn"></param>
        /// <param name="screenRow"></param>
        /// <param name="screenColumn"></param>
        public void WindowToScreen(int windowRow, int windowColumn, out int screenRow, out int screenColumn)
        {
            if (Parent == null)
                WindowToParent(windowRow, windowColumn, out screenRow, out screenColumn);
            else
            {
                WindowToParent(windowRow, windowColumn, out int parentRow, out int parentColumn);
                Parent.WindowToScreen(parentRow, parentColumn, out screenRow, out screenColumn);
            }
        }

        public Window ChildFromPos(int windowRow, int windowColumn, bool visibleOnly)
        {
            for (LinkedListNode<Window> item = mChildren.Last; item != null; item = item.Previous)
            {
                Window w = item.Value;
                if (visibleOnly && !w.Visible)
                    continue;
                if (w.ParentToWindow(windowRow, windowColumn, out int childRow, out int childColumn))
                    return w.ChildFromPos(childRow, childColumn, visibleOnly);
            }
            return this;
        }

        public Window ChildFromPos(int windowRow, int windowColumn)
        {
            return ChildFromPos(windowRow, windowColumn, false);
        }
        #endregion

        #region Focus-related events
        public virtual void OnSetFocus()
        {
        }

        public virtual void OnKillFocus()
        {
        }

        public virtual void OnKeyPressed(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
        }

        public virtual void OnMouseMove(int row, int column, bool shift, bool ctrl, bool alt, bool leftButton, bool rightButton)
        {
        }

        public virtual void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
        }

        public virtual void OnMouseLButtonUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
        }

        public virtual void OnMouseRButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
        }

        public virtual void OnMouseRButtonUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
        }

        public virtual void OnMouseWheelUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
        }

        public virtual void OnMouseWheelDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
        }

        public virtual void OnScreenSizeChanged(int height, int width)
        {
        }

        public virtual void OnKeyboardLayoutChanged()
        {
        }

        public virtual void OnIdle()
        {
            foreach (var window in mChildren)
                window.OnIdle();
        }
        #endregion
    }
}
