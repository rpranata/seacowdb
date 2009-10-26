using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.NewCommandPattern
{
    public class SetIsolationLevelCommand : Command
    {
        public override bool RunFor(string input)
        {
            return input.StartsWith("set isolation level");
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            string[] parts = command.Split(' ');

            if (parts[3] == "readcommitted")
            {
                dbEnvironment.CreateTransactionForIsolationLevel = CreateReadCommitted;
                dbEnvironment.Writer.WriteLine("Using ReadCommitted isolation for next transaction - read only committed data");
            }
            else if (parts[3] == "readuncommitted")
            {
                dbEnvironment.CreateTransactionForIsolationLevel = CreateReadUncommited;
                dbEnvironment.Writer.WriteLine("Using ReadUncommitted isolation for next transaction - read any data");
            }
            else
                dbEnvironment.Writer.WriteLine("Error: Unknown transaction isolation level");

            dbEnvironment.Writer.Flush();
        }

        public Transaction CreateReadCommitted()
        {
            return new ReadCommittedTransaction();
        }

        private Transaction CreateReadUncommited()
        {
            return new ReadUncommittedTransaction();
        }
    }
}
