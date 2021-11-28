using System;
using System.Runtime.InteropServices;

namespace Gehtsoft.Xce.Conio
{
    public sealed class TemporaryPointer : IDisposable
    {
        private GCHandle mHandle;
        public IntPtr Pointer { get; private set; }

        internal TemporaryPointer(object v)
        {
            mHandle = GCHandle.Alloc(v, GCHandleType.Pinned);
            Pointer = mHandle.AddrOfPinnedObject();
        }

        ~TemporaryPointer()
        {
            if (Pointer != IntPtr.Zero)
                mHandle.Free();
        }

        public void Dispose()
        {
            if (Pointer != IntPtr.Zero)
            {
                mHandle.Free();
                Pointer = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }
    }
}
