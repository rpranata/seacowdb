using System;
using System.Collections.Generic;
using System.Text;

namespace DLRDB.Core.ConcurrencyUtils
{
    public class Channel<T> : IBuffer<T>
    {
        private readonly Queue<T> _Queue;
        private readonly Semaphore _Token;
        private readonly Object _Lock;

        /// <summary>
        /// Channel is used for communication between Thread
        /// </summary>
        public Channel()
        {
            this._Lock = new Object();
            this._Token = new Semaphore(0);
            this._Queue = new Queue<T>();
        }

        #region Functions

        /// <summary>
        /// Adds data to the channel.
        /// </summary>
        /// <param name="data">Data that will be added.</param>
        public void Put(T data)
        {
            lock (this._Lock)
            {
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
            lock (this._Lock)
            {
                return this._Queue.Dequeue();
            }
        }    

        #endregion
    }
}
