using System;
using System.Collections.Generic;
using System.Text;

namespace Gehtsoft.Xce.Conio.Win
{
    /// <summary>
    /// Base class for dialog item
    /// </summary>
    public abstract class DialogItem : Window
    {
        private int mRow;
        private int mColumn;
        private int mWidth;
        private int mHeight;
        private Dialog mDialog;

        new public int Row => mRow;

        new public int Column => mColumn;

        new public int Height => mHeight;

        new public int Width => mWidth;

        public Dialog Dialog => mDialog;

        internal void SetDialog(Dialog dlg)
        {
            mDialog = dlg;
        }

        public abstract bool IsInputElement
        {
            get;
        }

        public abstract bool Enabled
        {
            get;
        }

        public virtual bool HasHotKey => false;

        public virtual char HotKey => throw new NotImplementedException();

        public int ID { get; }

        public virtual void Click()
        {
        }

        protected DialogItem(int id, int row, int column, int height, int width)
        {
            ID = id;
            mRow = row;
            mColumn = column;
            mHeight = height;
            mWidth = width;
        }

        protected DialogItem(int id, int row, int column)
        {
            ID = id;
            mRow = row;
            mColumn = column;
        }

        internal void SetDimesions(int height, int width)
        {
            if (Exists)
                Move(mRow, mColumn, height, width);
            else
            {
                mHeight = height;
                mWidth = width;
            }
        }

        public override void OnSizeChanged()
        {
            base.OnSizeChanged();
            mRow = base.Row;
            mColumn = base.Column;
            mHeight = base.Height;
            mWidth = base.Width;
        }

        public void Reposition(int row, int column, int height, int width)
        {
            if (Exists)
                Move(row, column, height, width);
            else
            {
                mRow = row;
                mColumn = column;
                mHeight = height;
                mWidth = width;
            }
        }

        public virtual void OnHotkeyActivated()
        {
        }
    }
}
