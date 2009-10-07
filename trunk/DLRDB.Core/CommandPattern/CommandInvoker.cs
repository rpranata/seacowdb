using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.CommandPattern
{
    public class CommandInvoker
    {
        private List<Command> _ListCommand;

        public CommandInvoker()
        {
            this._ListCommand = new List<Command>();
        }

        public void QueueCommand(String expression)
        {

        }

        public bool Commit()
        {
            return false;
        }

        public bool Rollback()
        {
            return false;
        }

    }
}
