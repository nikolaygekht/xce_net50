using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gehtsoft.Xce.TextBuffer
{
    internal sealed class MutexSlim : IDisposable
    {
        private readonly SemaphoreSlim mSemaphore = new SemaphoreSlim(1, 1);

        private sealed class MutexLock : IDisposable
        {
            private bool mDisposed;
            private readonly SemaphoreSlim mSemaphore;

            public MutexLock(SemaphoreSlim semaphore)
            {
                mSemaphore = semaphore;
            }

            ~MutexLock()
            {
                Dispose();
            }

            public void Dispose()
            {
                if (!mDisposed)
                {
                    mSemaphore.Release();
                    mDisposed = true;
                    GC.SuppressFinalize(this);
                }
            }
        }

        public IDisposable Lock()
        {
            mSemaphore.Wait();
            return new MutexLock(mSemaphore);
        }

        public async Task<IDisposable> LockAsync()
        {
            await mSemaphore.WaitAsync();
            return new MutexLock(mSemaphore);
        }

        public void Dispose()
        {
            mSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}


