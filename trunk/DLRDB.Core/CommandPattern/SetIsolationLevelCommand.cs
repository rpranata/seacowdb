using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class SetIsolationLevelCommand : Command
    {
        private static int ISOLATION_LEVEL_INDEX = 3;

        public override bool RunFor(string input)
        {
            return input.StartsWith("set isolation level");
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            string[] parts = command.Split(' ');
            if (parts[ISOLATION_LEVEL_INDEX] == "readcommitted")
            {
                dbEnvironment.CreateTransactionForIsolationLevel = CreateReadCommitted;
                dbEnvironment.Writer.WriteLine("Using ReadCommitted isolation for next transaction - read only committed data");
            }
            else if (parts[ISOLATION_LEVEL_INDEX] == "readuncommitted")
            {
                dbEnvironment.CreateTransactionForIsolationLevel = CreateReadUncommited;
                dbEnvironment.Writer.WriteLine("Using ReadUncommitted isolation for next transaction - read any data");
            }
            else if (parts[ISOLATION_LEVEL_INDEX] == "repeatableread")
            {
                dbEnvironment.CreateTransactionForIsolationLevel = CreateRepeatableRead;
                dbEnvironment.Writer.WriteLine("Using RepeatableRead isolation for next transaction - read any data");
            }
            else if (parts[ISOLATION_LEVEL_INDEX] == "serializable")
            {
                dbEnvironment.CreateTransactionForIsolationLevel = CreateSerializable;
                dbEnvironment.Writer.WriteLine("Using Serializable isolation for next transaction - read any data");
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

        private Transaction CreateRepeatableRead()
        {
            return new RepeatableReadTransaction();
        }

        private Transaction CreateSerializable()
        {
            return new SerializableTransaction();
        }

    }
}
