using System;
using System.Collections.Generic;
using System.Text;

namespace DLRDB.Core.ConcurrencyUtils
{
    /// <summary>
    /// ReadWriteLock utility that utilises the FIFOSemaphore to prevent
    /// reader or writer starvation.
    /// </summary>
    public class ReadWriteLock
    {
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
        public void AcquireReader()
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
        }

        public void AcquireWriter()
        {
            lock (_WaitingLock)
            { _WaitingWriters++; }

            this._WriterTurnstile.Acquire();
            _Turnstile.Acquire();

            lock (_WaitingLock)
            { _WaitingWriters--; }
            
            this._Mutex.Acquire();
            this._Turnstile.Release();
        }

        public void ReleaseWriter()
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

        public void ReleaseReader()
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