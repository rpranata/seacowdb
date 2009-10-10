using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.Exceptions
{
    public class UpdateException : Exception
    {
        public UpdateException(String message)
            : base(message)
        {
        }
    }
}
