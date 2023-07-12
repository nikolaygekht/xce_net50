using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    /// <summary>
    /// The container class for a sizeable window
    /// </summary>
    public class WindowBorderContainer : Window
    {
        private enum DragMode
        {
            None,
            Move,
            Size,
        }

        private Window mClientArea;
        private string mTitle;
        private readonly CanvasColor mBorderColor;
        private readonly BoxBorder mBorder;
        private DragMode mDragMode = DragMode.None;
        private int mPrevRow, mPrevColumn;
        private readonly bool mMoveable;
        private readonly bool mSizeable;

        protected WindowBorderContainer(string title, BoxBorder border, CanvasColor borderColor, bool moveable, bool sizeable)
        {
            mTitle = title;
            mBorder = border;
            mClientArea = null;
            mBorderColor = borderColor;
            mMoveable = moveable;
            mSizeable = sizeable;
        }

        protected void AttachClientArea(Window clientArea)
        {
            mClientArea = clientArea;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="title">window title</param>
        /// <param name="border">window border style</param>
        /// <param name="borderColor">border color</param>
        /// <param name="clientArea">client area window</param>
        /// <param name="moveable">is window moveable</param>
        /// <param name="sizeable">is window sizeable</param>
        public WindowBorderContainer(string title, BoxBorder border, CanvasColor borderColor, Window clientArea, bool moveable, bool sizeable)
        {
            mTitle = title;
            mBorder = border;
            mClientArea = clientArea;
            mBorderColor = borderColor;
            mMoveable = moveable;
            mSizeable = sizeable;
        }

        public override void OnSetFocus()
        {
            mClientArea?.SetFocus();
        }

        public override void OnPaint(Canvas canvas)
        {
            canvas.Box(0, 0, Height, Width, mBorder, mBorderColor, ' ');
            if (mSizeable)
                canvas.Write(Height - 1, Width - 1, "\u2194");
            if (CtrlSpaceMode)
                canvas.Write(0, 0, '\u2055');
            canvas.Write(0, 2, '[');
            canvas.Write(0, 3, mTitle);
            canvas.Write(0, 3 + mTitle.Length, ']');
        }

        public override void OnCreate()
        {
            Manager.Create(mClientArea, this, 0, 0, 0, 0);
            mClientArea.Show(true);
        }

        public override void OnSizeChanged()
        {
            GetClientArea(out int row, out int col, out int height, out int width);
            mClientArea.Move(row, col, height, width);
        }

        public override void OnMouseLButtonDown(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (row == 0 && mDragMode == DragMode.None && mMoveable)
            {
                WindowToScreen(row, column, out mPrevRow, out mPrevColumn);
                if (Manager.CaptureMouse(this))
                    mDragMode = DragMode.Move;
            }
            if (row == Height - 1 && column == Width - 1 && mSizeable)
            {
                WindowToScreen(row, column, out mPrevRow, out mPrevColumn);
                if (Manager.CaptureMouse(this))
                    mDragMode = DragMode.Size;
            }
        }

        public override void OnMouseLButtonUp(int row, int column, bool shift, bool ctrl, bool alt)
        {
            if (mDragMode == DragMode.Move)
            {
                int drow, dcolumn;
                drow = row - mPrevRow;
                dcolumn = column - mPrevColumn;
                if (drow != 0 || dcolumn != 0)
                    Move(Row + drow, Column + dcolumn, Height, Width);
            }
            else if (mDragMode == DragMode.Size)
            {
                int drow, dcolumn;
                ScreenToWindow(row, column, out int wrow, out int wcolumn);
                if (wrow == Height - 1 && wcolumn == Width - 1)
                {
                    drow = row - mPrevRow;
                    dcolumn = column - mPrevColumn;
                    if ((drow != 0 || dcolumn != 0) && Height + drow >= 3 && Width + dcolumn >= 3)
                        Move(Row, Column, Height + drow, Width + dcolumn);
                    mPrevRow = row;
                    mPrevColumn = column;
                }
            }

            if (mDragMode != DragMode.None)
            {
                Manager.ReleaseMouse(this);
                mDragMode = DragMode.None;
            }
        }

        public override void OnMouseMove(int row, int column, bool shift, bool ctrl, bool alt, bool leftButton, bool rightButton)
        {
            if (leftButton)
            {
                if (mDragMode == DragMode.Move)
                {
                    int drow, dcolumn;
                    drow = row - mPrevRow;
                    dcolumn = column - mPrevColumn;
                    if (drow != 0 || dcolumn != 0)
                        Move(Row + drow, Column + dcolumn, Height, Width);
                    mPrevRow = row;
                    mPrevColumn = column;
                }
                else if (mDragMode == DragMode.Size)
                {
                    int drow, dcolumn;
                    drow = row - mPrevRow;
                    dcolumn = column - mPrevColumn;
                    if ((drow != 0 || dcolumn != 0) && Height + drow >= 3 && Width + dcolumn >= 3)
                    {
                        Move(Row, Column, Height + drow, Width + dcolumn);
                        mPrevRow = row;
                        mPrevColumn = column;
                    }
                }
            }
            else if (mDragMode != DragMode.None)
            {
                Manager.ReleaseMouse(this);
                mDragMode = DragMode.None;
            }
        }

        protected virtual bool ProcessWindowKeyCommand(ScanCode scanCode, char character, bool shift, bool ctrl, bool alt)
        {
            return false;
        }


        public virtual void GetClientArea(out int row, out int col, out int height, out int width)
        {
            row = 1;
            col = 1;
            height = Height - 2;
            width = Width - 2;
        }

        public string Title
        {
            get
            {
                return mTitle;
            }
            set
            {
                mTitle = value;
                Invalidate();
            }
        }
    }
}
