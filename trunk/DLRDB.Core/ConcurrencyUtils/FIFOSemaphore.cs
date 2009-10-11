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
                {
                    //Console.WriteLine(Thread.CurrentThread.Name + " decrements the token");
                    this._Token--;
                }
                else
				{
					tempThread = Thread.CurrentThread;
					//Console.WriteLine(Thread.CurrentThread.Name + " waits for token");
                    this._WaitQueue.Enqueue(tempThread);
                    doWait = true;
				}
            }

            if (doWait)
            {
                lock (tempThread)
                { Monitor.Wait(tempThread); }
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
                    {
                        //Console.WriteLine("Pulsing " + tempThread.Name);
                        Monitor.Pulse(tempThread);
                    }
                }
                else
                {
                    //Console.WriteLine(Thread.CurrentThread.Name + " releases the token");
                    this._Token++;
                }
            }
        }

        #endregion
    }
}
