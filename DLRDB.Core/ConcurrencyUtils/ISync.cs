using System;

namespace DLRDB.Core.ConcurrencyUtils
{
    interface ISync
    {
        void Acquire();
        void Release();
    }
}
