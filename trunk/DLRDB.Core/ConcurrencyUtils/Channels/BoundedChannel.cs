using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DLRDB.Core.ConcurrencyUtils
{
    public class BoundedChannel<T> : IBuffer<T>
    {
        private readonly Queue<T> _Queue;
        private readonly Semaphore _Token;
        private readonly Semaphore _TopBound;
        private readonly Object _Object;

        /// <summary>
        /// Channel is used for communication between Thread, this channel is bounded.
        /// </summary>
        /// <param name="topBound">Size of the channel</param>
        public BoundedChannel(int topBound)
        {
            this._Token = new Semaphore(0);
            this._TopBound = new Semaphore(topBound);
            this._Queue = new Queue<T>();
            this._Object = new Object();
        }

        #region Functions

        /// <summary>
        /// Adds data to the channel.
        /// </summary>
        /// <param name="data">Data that will be added.</param>
        public void Put(T data)
        {
            lock (this._Object)
            {
                this._TopBound.Acquire();
                this._Queue.Enqueue(data);
                this._Token.ForceRelease();
            }
        }

        /// <summary>
        /// Removes data from the channel.
        /// </summary>
        /// <returns>Data taken.</returns>
        public T Take()
        {
            this._Token.Acquire();
            lock(this._Object)
            {
                this._TopBound.ForceRelease();
                return _Queue.Dequeue();
            }
        }

        #endregion

    }
}
