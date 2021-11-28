using System;
using System.Collections.Generic;
using System.Text;
using CanvasColor = Gehtsoft.Xce.Conio.CanvasColor;

namespace Gehtsoft.Xce.Conio.Win
{
    public class Window : IDisposable
    {
        #region Constructor/Destructor
        private bool mExists;

        internal WindowManager Manager { get; set; }

        public WindowManager WindowManager
        {
            get
            {
                return Manager;
            }
        }

        public bool Exists
        {
            get
            {
                return mExists;
            }
        }

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
        private LinkedList<Window> mChildren = new LinkedList<Window>();
        private Window mParent;

        /// <summary>
        /// Get a parent window
        /// </summary>
        public Window Parent
        {
            get
            {
                return mParent;
            }
        }

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
            if (mParent == null)
                return false;
            if (parent == mParent)
                return true;
            return mParent.HasParent(parent);
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
        public IEnumerable<Window> Children
        {
            get
            {
                return mChildren;
            }
        }

        /// <summary>
        /// Create a window
        /// </summary>
        internal void Create(Window parent)
        {
            mParent = parent;
            if (mParent != null)
                mParent.mChildren.AddLast(this);
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

            if (mParent != null)
            {
                LinkedListNode<Window> _this = mParent.mChildren.Find(this);
                if (_this != null)
                    mParent.mChildren.Remove(_this);
                mParent.Invalidate();
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
        bool mVisible;

        public int Row
        {
            get
            {
                return mRow;
            }
        }

        public int Column
        {
            get
            {
                return mColumn;
            }
        }

        public int Height
        {
            get
            {
                return mHeight;
            }
        }

        public int Width
        {
            get
            {
                return mWidth;
            }
        }

        public bool Visible
        {
            get
            {
                return mVisible;
            }
        }

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
                throw new ArgumentException(Resources.ArgumentMustNotBeNegative, "height");
            if (width < 0)
                throw new ArgumentException(Resources.ArgumentMustNotBeNegative, "width");
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
        public bool Valid
        {
            get
            {
                return mValid;
            }
        }

        internal Canvas Canvas
        {
            get
            {
                return mCanvas;
            }
        }

        /// <summary>
        /// Invalidate the whole window
        /// </summary>
        virtual public void Invalidate()
        {
            mValid = false;
            if (mParent != null)
                mParent.Invalidate();
        }

        private static CanvasColor mDefaultColor = new CanvasColor(0x03);

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
            if (mWidth != 0 && mHeight != 0)
            {
                if (mCanvas == null)
                    mCanvas = new Canvas(mHeight, mWidth);
            }
            else
            {
                mValid = true;
                return;
            }

            OnPaint(mCanvas);

            foreach (Window child in mChildren)
            {
                if (child.Visible && child.Exists)
                {
                    if (!child.Valid)
                        child.Paint();
                    if (child.Canvas != null)
                        mCanvas.Paint(child.Row, child.Column, child.Canvas);
                }
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
            if (windowRow >= 0 && windowRow < mHeight && windowColumn >= 0 && windowColumn < mWidth)
                return true;
            else
                return false;
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
            if (mParent == null)
                return ParentToWindow(screenRow, screenColumn, out windowRow, out windowColumn);
            else
            {
                int parentRow, parentColumn;
                mParent.ScreenToWindow(screenRow, screenColumn, out parentRow, out parentColumn);
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
            if (mParent == null)
                WindowToParent(windowRow, windowColumn, out screenRow, out screenColumn);
            else
            {
                int parentRow, parentColumn;
                WindowToParent(windowRow, windowColumn, out parentRow, out parentColumn);
                mParent.WindowToScreen(parentRow, parentColumn, out screenRow, out screenColumn);
            }
        }

        public Window ChildFromPos(int windowRow, int windowColumn, bool visibleOnly)
        {
            for (LinkedListNode<Window> item = mChildren.Last; item != null; item = item.Previous)
            {
                int childRow, childColumn;
                Window w = item.Value;
                if (visibleOnly && !w.Visible)
                    continue;
                if (w.ParentToWindow(windowRow, windowColumn, out childRow, out childColumn))
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
        #endregion
    }
}
