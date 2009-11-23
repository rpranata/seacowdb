using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public abstract class Command
    {
        public abstract bool RunFor(string input);
        public abstract void Run(string command, Table table,
            DbEnvironment dbEnvironment);
    }
}