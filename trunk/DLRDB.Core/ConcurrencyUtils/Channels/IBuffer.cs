using System;

namespace DLRDB.Core.ConcurrencyUtils
{
    public interface IBuffer<T>
    {
        void Put(T data);
        T Take();
    }
}
