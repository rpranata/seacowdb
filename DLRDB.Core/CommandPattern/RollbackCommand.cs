using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    class RollbackCommand : Command
    {
        public override bool RunFor(string input)
        {
            return input.Equals("rollback");
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            dbEnvironment.CurrentTransaction.Rollback();
            dbEnvironment.Writer.WriteLine("Transaction is rollbacked");
            dbEnvironment.Writer.WriteLine("=========================");
            dbEnvironment.Writer.Flush();
        }
    }
}
