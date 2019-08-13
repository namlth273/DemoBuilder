using System;
using System.Threading;

namespace ĐemoMultiThread
{
    public class BlockingCounter : IDisposable
    {
        private int _count;
        private readonly object _counterLock = new object();

        private bool _isClosed = false;
        private volatile bool _isDisposed = false;

        private readonly int _maxSize = 0;

        private readonly ManualResetEvent _finished = new ManualResetEvent(false);

        public BlockingCounter(int maxSize = 0)
        {
            if (maxSize < 0)
                throw new ArgumentOutOfRangeException("maxSize");
            _maxSize = maxSize;
        }

        public void WaitableIncrement(int timeoutMs = Timeout.Infinite)
        {
            lock (_counterLock)
            {
                while (_maxSize > 0 && _count >= _maxSize)
                {
                    CheckClosedOrDisposed();
                    if (!Monitor.Wait(_counterLock, timeoutMs))
                        throw new TimeoutException("Failed to wait for counter to decrement.");
                }

                CheckClosedOrDisposed();
                _count++;

                if (_count == 1)
                {
                    Monitor.PulseAll(_counterLock);
                }

            }
        }

        public void WaitableDecrement(int timeoutMs = Timeout.Infinite)
        {
            lock (_counterLock)
            {
                try
                {
                    while (_count == 0)
                    {
                        CheckClosedOrDisposed();
                        if (!Monitor.Wait(_counterLock, timeoutMs))
                            throw new TimeoutException("Failed to wait for counter to increment.");
                    }

                    CheckDisposed();

                    _count--;

                    if (_maxSize == 0 || _count == _maxSize - 1)
                        Monitor.PulseAll(_counterLock);
                }
                finally
                {
                    if (_isClosed && _count == 0)
                        _finished.Set();
                }
            }
        }

        void CheckClosedOrDisposed()
        {
            if (_isClosed)
                throw new Exception("The counter is closed");
            CheckDisposed();
        }

        void CheckDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("The counter has been disposed.");
        }

        public void Close()
        {
            lock (_counterLock)
            {
                CheckDisposed();
                _isClosed = true;
                Monitor.PulseAll(_counterLock);
            }
        }

        public bool WaitForFinish(int timeoutMs = Timeout.Infinite)
        {
            CheckDisposed();
            lock (_counterLock)
            {
                if (_count == 0)
                    return true;
            }
            return _finished.WaitOne(timeoutMs);
        }

        public void CloseAndWait(int timeoutMs = Timeout.Infinite)
        {
            Close();
            WaitForFinish(timeoutMs);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                lock (_counterLock)
                {
                    // Wake up all waiting threads, so that they know the object 
                    // is disposed and there's nothing to wait anymore
                    Monitor.PulseAll(_counterLock);
                }
                _finished.Close();
            }
        }
    }
}