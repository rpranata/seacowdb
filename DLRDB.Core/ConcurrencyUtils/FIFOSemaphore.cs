using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO;

namespace DLRDB.Core.ConcurrencyUtils
{
    /// <summary>
    /// First In First Out Semaphore. A Semaphore which enforces FIFO
    /// behaviour.
    /// </summary>
    public class FIFOSemaphore : Semaphore
    {
        public readonly Queue<Thread> _WaitQueue;
        
        public FIFOSemaphore(int token)
            : base(token)
        { this._WaitQueue = new Queue<Thread>(); }

        #region Functions

        public override void Acquire()
        {
            bool doWait = false;
			Thread tempThread = null;
            lock (this._Lock)
            {
                if (this._Token > 0)
                    this._Token--;
                else
				{
					tempThread = Thread.CurrentThread;
                    this._WaitQueue.Enqueue(tempThread);
                    doWait = true;
                    Monitor.Enter(tempThread);
				}
            }

            if (doWait)
            {
                //lock (tempThread)
                { Monitor.Wait(tempThread); }
                Monitor.Exit(tempThread);
            }
        }

        public override void Release()
        {
            lock (this._Lock)
            {
                if (this._WaitQueue.Count > 0)
                {
                    Thread tempThread = this._WaitQueue.Dequeue();
                    lock (tempThread)
                        Monitor.Pulse(tempThread);
                }
                else
                    this._Token++;
            }
        }

        #endregion
    }
}
