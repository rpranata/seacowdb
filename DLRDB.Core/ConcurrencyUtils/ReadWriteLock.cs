using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DLRDB.Core.ConcurrencyUtils
{
    public interface ILock : IDisposable
    {
        void Release();
    }

    /// <summary>
    /// ReadWriteLock utility that utilises the FIFOSemaphore to prevent
    /// reader or writer starvation.
    /// </summary>
    public class ReadWriteLock
    {
        private System.Threading.Thread _CurrentWriterThread = null;
        private List<System.Threading.Thread> _CurrentReaderThread = new List<System.Threading.Thread>();
        private class RWLock : ILock
        {
            private bool isWriter;
            private bool released = false;
            private ReadWriteLock parent;

            public RWLock(bool writer, ReadWriteLock parent)
            {
                isWriter = writer;
                this.parent = parent;
            }

            public void Release()
            {
                if (!released)
                {
                    released = true;
                    if (parent == null) return;
                    if (isWriter)
                        parent.ReleaseWriter();
                    else
                        parent.ReleaseReader();
                }
            }

            #region IDisposable Members

            public void Dispose()
            {
                Release();
            }

            #endregion
        }

        #region Initialisers

        private readonly FIFOSemaphore _WriterTurnstile 
            = new FIFOSemaphore(1);

        private readonly FIFOSemaphore _Turnstile 
            = new FIFOSemaphore(1); 

        private readonly FIFOSemaphore _Mutex 
            = new FIFOSemaphore(1);

        private readonly object _WaitingLock = new object();
        
        private int _ReaderCount = 0;
        private int _WaitingReaders = 0;
        private int _WaitingWriters = 0;
        
        private bool hasBlockedTurnstile = false;

        #endregion

        #region Functions
        public ILock AcquireReader()
        {
            this._Turnstile.Acquire();
            this._Turnstile.Release();

            lock (_WaitingLock)
            {
                // if this thread has already hold the read lock, we'd just return no nore lock
                if (_CurrentReaderThread.Contains(Thread.CurrentThread) ||
                    Thread.CurrentThread == _CurrentWriterThread)
                {
                    return new RWLock(false, null);
                }
                _WaitingReaders++; 
            }

            lock (this)
            {
                if (this._ReaderCount == 0)
                { this._Mutex.Acquire(); }
                this._ReaderCount++;
            }
            lock (_WaitingLock)
            {
                _WaitingReaders--;
                _CurrentReaderThread.Add(Thread.CurrentThread);
                if (_WaitingReaders == 0 && hasBlockedTurnstile)
                {
                    _Turnstile.Release();
                    _WriterTurnstile.Release();
                    hasBlockedTurnstile = false;
                }
            }

            return new RWLock(false, this);
        }

        public ILock AcquireWriter()
        {
            System.Threading.Thread localThread = null;
            lock (_WaitingLock)
            {
                localThread = this._CurrentWriterThread;
            }

            if ((localThread != null) && (localThread == System.Threading.Thread.CurrentThread))
            {
                // this Thread has acquired writer lock before

                return new RWLock(true, null);
            }
            else
            {
                lock (_WaitingLock)
                { _WaitingWriters++; }

                this._WriterTurnstile.Acquire();
                _Turnstile.Acquire();

                lock (_WaitingLock)
                { _WaitingWriters--; }

                _CurrentWriterThread = System.Threading.Thread.CurrentThread;

                this._Mutex.Acquire();
                this._Turnstile.Release();

                return new RWLock(true, this);
            }

        }

        private void ReleaseWriter()
        {
            this._Mutex.Release();

            lock (_WaitingLock)
            {
                if (_WaitingReaders > 0)
                {
                    //There is both waiting writers and readers
                    // readers are past the turnstile. block the
                    // writer at the turnstile and any new readers.
                    this._Turnstile.Acquire();
                    hasBlockedTurnstile = true;
                }
                else
                {
                    this._CurrentWriterThread = null;
                    _WriterTurnstile.Release(); 
                }
            }
        }

        private void ReleaseReader()
        {
            lock (_WaitingLock)
            {
                _CurrentReaderThread.Remove(Thread.CurrentThread);

                if (this._ReaderCount > 0)
                { 
                    this._ReaderCount--; 
                }
                if (this._ReaderCount == 0)
                { 
                    this._Mutex.Release(); 
                }
            }
        }

        #endregion
    }
}