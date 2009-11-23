using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class InsertCommand : Command
    {
        private static int INSERT_PARAM_INDEX = 1;

        public override bool RunFor(string input)
        { return input.Split(' ')[0] == ("insert"); }

        public override void Run(string command, Table table,
            DbEnvironment dbEnvironment)
        {
            String[] commands = command.Split(' ');
            
            Row newRow = null;
            DateTime start = DateTime.Now;
            String[] insertParams = Regex.Split
                (commands[INSERT_PARAM_INDEX], ",");
            if (insertParams.Length == 2)
            {
                newRow = table.NewRow();
                newRow.Fields[1].Value = newRow.Fields[1]
                    .NativeToBytes(insertParams[0]);
                newRow.Fields[2].Value = newRow.Fields[2]
                    .NativeToBytes(Int32.Parse(insertParams[1]));
                table.Insert(newRow, dbEnvironment.CurrentTransaction);

                DateTime end = DateTime.Now;
                TimeSpan theSpan = end - start;
                dbEnvironment.Writer.WriteLine(DateTime.Now + " >> " 
                    + "Inserted: 1 row in " + theSpan.TotalMilliseconds
                    + " ms.");
                dbEnvironment.Writer.WriteLine(" ");
            }
            else
            {
                dbEnvironment.Writer.WriteLine(DateTime.Now 
                    + " >> Error: Insufficient parameters provided.");
                dbEnvironment.Writer.WriteLine(" ");
            }
            dbEnvironment.Writer.Flush();
        }
    }
}
