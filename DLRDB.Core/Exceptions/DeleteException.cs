﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.Exceptions
{
    public class DeleteException : Exception
    {
        public DeleteException(String message)
            : base(message)
        {
        }
    }
}