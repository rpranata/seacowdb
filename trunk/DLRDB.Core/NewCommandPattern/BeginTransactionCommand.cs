using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class BeginTransactionCommand : Command
    {
        public override bool RunFor(string input)
        {
            return input.Equals("begin transaction");
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            //Not needed anymmore, since the transaction has been made in the active
            //command listener if the trasaction is null
            //dbEnvironment.CurrentTransaction = dbEnvironment.CreateTransactionForIsolationLevel();
            dbEnvironment.Writer.WriteLine("Transaction using " + 
                dbEnvironment.CurrentTransaction.ToString().ToLower() + " is started");
            dbEnvironment.Writer.WriteLine("========================================");
            dbEnvironment.Writer.Flush();
        }
    }
}
