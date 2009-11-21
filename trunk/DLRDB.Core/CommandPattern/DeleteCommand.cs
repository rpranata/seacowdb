using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    class DeleteCommand : Command
    {
        private static int DELETE_PARAM_INDEX = 1;

        public override bool RunFor(string input)
        {
            return input.Split(' ')[0] == ("delete");
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            String[] commands = command.Split(' ');

            // Deleting the data
            // ===================================
            int deletedRows = 0;
            if (commands[1] == "*")
            {
                deletedRows = table.DeleteAll(dbEnvironment.CurrentTransaction, dbEnvironment.Writer);
            }
            else
            {
                String[] arrSplitExpression = Regex.Split(commands[DELETE_PARAM_INDEX], "-");

                Int32 startIndex = Convert.ToInt32(arrSplitExpression[0]);
                Int32 endIndex = Convert.ToInt32(arrSplitExpression[1]);

                deletedRows = table.Delete(startIndex, endIndex, dbEnvironment.CurrentTransaction, dbEnvironment.Writer);
            }

            dbEnvironment.Writer.WriteLine(DateTime.Now + " >>> " + "Finish deleting " + deletedRows + " row(s)");
            dbEnvironment.Writer.Flush();
        }
    }
}
