using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.Exceptions
{
    public class SelectException : Exception 
    {
        public SelectException(String message) : base (message)
        {   
        }
    }
}
