using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DLRDB.Core.ConcurrencyUtils
{
    public abstract class ActiveObject
    {
        protected Thread _Thread;

        public ActiveObject()
        {
            this._Thread = new Thread(new ThreadStart(Run));
        }

        #region Functions

        public virtual void Run()
        {
            while (true)
            {
                this.DoSomething();
            }
        }

        public abstract void DoSomething();

        public void Stop()
        {
            this._Thread.Abort();
        }

        public void Start()
        {
            this._Thread.Start();
        }

        public void Join()
        {
            this._Thread.Join();
        }

        public void Interrupt()
        {
			try
			{
            	this._Thread.Interrupt();
			}
			catch (ThreadInterruptedException)
			{
				throw;
			}
        }

        #endregion
    }
}
