using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.Exceptions
{
    public class DLRDBException : Exception
    {
        public DLRDBException(String message) : base(message) { }
    }

    public class SelectException : DLRDBException
    {
        public SelectException(String message) : base(message) { }
    }

    public class UpdateException : DLRDBException
    {
        public UpdateException(String message) : base(message) { }
    }

    public class DeleteException : DLRDBException
    {
        public DeleteException(String message) : base(message) { }
    }
}