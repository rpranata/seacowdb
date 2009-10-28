using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.Exceptions
{
    public class DLRDBException : Exception
    {
        public DLRDBException(String message)
            : base(message)
        {
        }
    }
}
