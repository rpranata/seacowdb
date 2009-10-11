using System;

namespace DLRDB.Core.ConcurrencyUtils
{
    /// <summary>
    /// Interface for the Semaphore.
    /// </summary>
    interface ISync
    {
        void Acquire();
        void Release();
    }
}
