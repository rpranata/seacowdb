using System;
using System.Collections.Generic;
using System.Text;

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
            { _WaitingReaders++; }

            lock (this)
            {
                if (this._ReaderCount == 0)
                { this._Mutex.Acquire(); }
                this._ReaderCount++;
            }
            lock (_WaitingLock)
            {
                _WaitingReaders--;
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
            lock (_WaitingLock)
            { _WaitingWriters++; }

            this._WriterTurnstile.Acquire();
            _Turnstile.Acquire();

            lock (_WaitingLock)
            { _WaitingWriters--; }
            
            this._Mutex.Acquire();
            this._Turnstile.Release();

            return new RWLock(true, this);
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
                { _WriterTurnstile.Release(); }
            }
        }

        private void ReleaseReader()
        {
            lock (this)
            {
                if (this._ReaderCount > 0)
                { this._ReaderCount--; }
                if (this._ReaderCount == 0)
                { this._Mutex.Release(); }
            }
        }

        #endregion
    }
}