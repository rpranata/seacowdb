using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class SelectCommand : Command
    {
        private static int SELECT_PARAM_INDEX = 1;
        
        public override bool RunFor(string input)
        { return input.Split(' ')[0] == "select"; }

        public override void Run(string command, Table table,
            DbEnvironment dbEnvironment)
        {
            String[] commands = command.Split(' ');
            
            if (commands[SELECT_PARAM_INDEX] == "*")
            {
                table.SelectAll(dbEnvironment.Writer, 
                    dbEnvironment.CurrentTransaction);
            }
            else
            {
                String[] arrSplitExpression = Regex
                    .Split(commands[SELECT_PARAM_INDEX], "-");

                Int32 startIndex = Convert.ToInt32(arrSplitExpression[0]);
                Int32 endIndex = Convert.ToInt32(arrSplitExpression[1]);

                table.Select(startIndex, endIndex,
                    dbEnvironment.CurrentTransaction, dbEnvironment.Writer);
            }

            dbEnvironment.Writer.WriteLine(DateTime.Now + " >> "
                + "Select operation completed.");
            dbEnvironment.Writer.Flush();
        }
    }
}
