using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DLRDB.Core.ConcurrencyUtils
{
    /// <summary>
    /// Basic Semaphorewith expanded Acquire and Release functionalities.
    /// </summary>
    public class Semaphore : ISync
	{
        protected int _Token;
        protected readonly Object _Lock;

        /// <summary>
        /// Constructor of the semaphore class takes 1 parameter
        /// as initial number of token(s) available.
        /// </summary>
        /// <param name="token">Initial number of token(s).</param>
        public Semaphore(int token)
        {
            this._Lock = new Object();
            this._Token = token;
        }
        
		#region Functions
		
        /// <summary>
        /// Acquires the token from this semaphore. This acquire waits
        /// until it gets the token. For timeout acquire, try to look
        /// at TryAcquire().
        /// </summary>
		public virtual void Acquire() 
            { this.TryAcquire(-1); }
		
        /// <summary>
        /// Acquires 1 token from this semaphore. If there is no token
        /// available, it will timeout after a particular time.
        /// </summary>
        /// <param name="ms">Timeout in millisecond.</param>
        /// <returns></returns>
		public virtual bool TryAcquire(int ms)
		{
			double endTime = (System.DateTime.Now.Ticks / 10000) + ms;
			lock(this._Lock)
			{
				for(;;)
				{
					if (this._Token > 0)
					{
						this._Token--;
						return true;
					}
					
					if (ms != -1)
					{
						double now = System.DateTime.Now.Ticks / 10000;
						ms = (int) (endTime - now);
                        if (ms <= 0)
                        { return false; }
					}

					Monitor.Wait(this._Lock, ms);
				}
			}
		}

        
        
        
        
        
        
        /// <summary>
        /// Releases 1 token from this semaphore
        /// Note : not interruptible, try ForceRelease().
        /// </summary>
        public virtual void Release()
        {
            lock (this._Lock)
            {
                this._Token++;
                Monitor.PulseAll(this._Lock);
            }
        }
		
        /// <summary>
        /// Releases 1 token from this semaphore, and this works 
        /// with interrupt.
        /// </summary>
		public virtual void ForceRelease()
		{
			bool wasInterrupted = false;
			for(;;)
			{
				try
				{
					this.Release();
					break;
				}
				catch (ThreadInterruptedException)
				{
					wasInterrupted = true;
					Thread.Sleep(0);
				}
			}

            if (wasInterrupted)
                { Thread.CurrentThread.Interrupt(); }
		}
		#endregion		
    }
}