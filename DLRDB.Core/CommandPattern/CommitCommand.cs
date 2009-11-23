using System;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class CommitCommand : Command
    {
        public override bool RunFor(string input)
        { return input.Equals("commit"); }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            // Not needed anymmore, since the transaction has been made
            // in the active command listener if the trasaction is null
            // dbEnvironment.CurrentTransaction = dbEnvironment
            // .CreateTransactionForIsolationLevel();
            dbEnvironment.Writer.WriteLine("=====================");
            dbEnvironment.Writer.WriteLine("Transaction committed.");
            dbEnvironment.Writer.Flush();
        }
    }
}
