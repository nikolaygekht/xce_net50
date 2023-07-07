using Gehtsoft.Xce.Conio.Drawing;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Gehtsoft.Xce.Conio.Output
{
    internal class ConEmuConsoleOutput : Win32ConsoleOutput
    {
        private IntPtr mViewHandle, mMapPtr;

        public override ConioOutputMode Mode
        {
            get => ConioOutputMode.ConEmu;
        }

        public ConEmuConsoleOutput() : base()
        {
            SupportsTrueColor = false;
            mViewHandle = mMapPtr = IntPtr.Zero;
            var rgbChannelName = string.Format("Console_annotationInfo_{0:x}_{1:x}", 32, Win32.GetConsoleWindow());
            mViewHandle = Win32.OpenFileMapping(Win32.FileMapAccess.FileMapAllAccess, false, rgbChannelName);
            if (mViewHandle != IntPtr.Zero)
            {
                mMapPtr = Win32.MapViewOfFile(mViewHandle, Win32.FileMapAccess.FileMapAllAccess, 0, 0, 0);
                if (mMapPtr != IntPtr.Zero)
                    SupportsTrueColor = true;
                else
                {
                    Win32.CloseHandle(mViewHandle);
                    mViewHandle = IntPtr.Zero;
                }
            }
        }

        protected override bool Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (mViewHandle != IntPtr.Zero)
            {
                Win32.UnmapViewOfFile(mMapPtr);
                Win32.CloseHandle(mViewHandle);
                mMapPtr = mViewHandle = IntPtr.Zero;
            }
            return true;
        }

        public override Canvas BufferToCanvas(int row = 0, int column = 0, int rows = -1, int columns = -1)
        {
            var canvas = base.BufferToCanvas(row, column, rows, columns);   
            if (SupportsTrueColor)
                BufferToCanvasTrueColor(canvas, row, column, rows, columns);
            return canvas;
        }

        private void BufferToCanvasTrueColor(Canvas canvas, int row, int column, int rows, int columns)
        {
            if (row != mWindowTop && column != mWindowLeft &&
                rows != mWindowRows && columns != mWindowColumns)
                throw new InvalidOperationException("In true color mode the canvas must exactly match the visible window size");
            Win32.AnnotationHeader header = Marshal.PtrToStructure<Win32.AnnotationHeader>(mMapPtr);
            header.Locked = 1;
            Marshal.StructureToPtr(header, mMapPtr, false);

            int offset = header.StructSize;
            int size = canvas.Rows * canvas.Columns;
            int[] fg = canvas.ForegroundColor.Raw;
            int[] bg = canvas.BackgroundColor.Raw;
            int[] style = canvas.Style.Raw;

            Win32.AnnotationInfo info;
            for (int i = 0; i < size; i++)
            {
                info = MarshalEx.PtrToBitFieldStruct<Win32.AnnotationInfo>(mMapPtr, offset + i * 32);

                if (info.fg_valid != 0)
                    fg[i] = info.fg_color;
                else
                    fg[i] = -1;

                if (info.bk_valid != 0)
                    bg[i] = info.bk_color;
                else
                    bg[i] = -1;

                style[i] = info.style;
            }

            header.Locked = 0;
            Marshal.StructureToPtr(header, mMapPtr, false);
        }

        public override void PaintCanvasToBuffer(Canvas canvas, int bufferRow = 0, int bufferColumn = 0)
        {
            if (SupportsTrueColor)
                PaintCanvasToBufferTrueColors(canvas, bufferRow, bufferColumn);

            using var finalAction = new PaintCanvasToBufferFinalAction(this);
            base.PaintCanvasToBuffer(canvas, bufferRow, bufferColumn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PaintCanvasToBufferTrueColors(Canvas canvas, int bufferRow, int bufferColumn)
        {
            if (bufferRow != mWindowTop && bufferColumn != mWindowLeft &&
                canvas.Rows != mWindowRows && canvas.Columns != mWindowColumns)
                throw new InvalidOperationException("In true color mode the canvas must exactly match the visible window size");

            var header = Marshal.PtrToStructure<Win32.AnnotationHeader>(mMapPtr);
            header.Locked = 1;
            Marshal.StructureToPtr(header, mMapPtr, false);

            int offset = header.StructSize;
            int size = canvas.Rows * canvas.Columns;

            int[] fg = canvas.ForegroundColor.Raw;
            int[] bg = canvas.BackgroundColor.Raw;
            int[] style = canvas.Style.Raw;

            Win32.AnnotationInfo info = new Win32.AnnotationInfo();
            for (int i = 0; i < size; i++)
            {
                int canvasRow = i / canvas.Columns;
                int canvasColumn = i - canvasRow * canvas.Columns;
                if (fg[i] != -1 && bg[i] != -1)
                {
                    info.fg_valid = 1;
                    info.fg_color = fg[i];
                    info.bk_valid = 1;
                    info.bk_color = bg[i];
                }
                else
                {
                    info.fg_valid = 0;
                    info.fg_color = 0;
                    info.bk_valid = 0;
                    info.bk_color = 0;
                }

                if (style[i] >= 0 && style[i] < 512)
                    info.style = style[i];
                else
                    info.style = 0;

                int cellOffset = (bufferRow + canvasRow) * mScreenBufferColumns + bufferColumn + canvasColumn;
                int valueOffset = offset + cellOffset * 32;
                MarshalEx.BitFieldStructToPtr(info, mMapPtr, valueOffset);
            }
        }

        private sealed class PaintCanvasToBufferFinalAction : IDisposable
        {
            private readonly ConEmuConsoleOutput mOutput;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PaintCanvasToBufferFinalAction(ConEmuConsoleOutput output)
            {
                mOutput = output;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if (mOutput.SupportsTrueColor)
                {
                    var header = Marshal.PtrToStructure<Win32.AnnotationHeader>(mOutput.mMapPtr);
                    header.FlushCounter++;
                    header.Locked = 0;
                    Marshal.StructureToPtr(header, mOutput.mMapPtr, false);
                }
            }
        }

        public override void Clear()
        {
            base.Clear();

            if (SupportsTrueColor)
            {
                var header = Marshal.PtrToStructure<Win32.AnnotationHeader>(mMapPtr);
                header.Locked = 1;
                int offset = header.StructSize;
                int size = header.BufferSize / 32;
                Win32.AnnotationInfo info = new Win32.AnnotationInfo();
                for (int i = 0; i < size; i++)
                    MarshalEx.BitFieldStructToPtr(info, mMapPtr, offset + i * 32);

                header.FlushCounter++;
                header.Locked = 0;
                Marshal.StructureToPtr(header, mMapPtr, false);
            }
        }
    }

}
