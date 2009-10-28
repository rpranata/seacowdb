using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.NewCommandPattern
{
    class DeleteCommand : Command
    {
        private static int DELETE_PARAM_INDEX = 1;
        private StreamWriter _Writer;

        public override bool RunFor(string input)
        {
            return input.Split(' ')[0] == ("delete");
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            String[] commands = command.Split(' ');
            this._Writer = dbEnvironment.Writer;

            // Deleting the data
            // ===================================
            int deletedRows = 0;
            if (commands[1] == "*")
            {
                deletedRows = table.DeleteAll(dbEnvironment.CurrentTransaction, this._Writer);
            }
            else
            {
                String[] arrSplitExpression = Regex.Split(commands[DELETE_PARAM_INDEX], "-");

                Int32 startIndex = Convert.ToInt32(arrSplitExpression[0]);
                Int32 endIndex = Convert.ToInt32(arrSplitExpression[1]);

                deletedRows = table.Delete(startIndex, endIndex, dbEnvironment.CurrentTransaction, this._Writer);
            }

            this._Writer.WriteLine(DateTime.Now + " >>> " + "Finish deleting " + deletedRows + " row(s)");
            this._Writer.Flush();
        }
    }
}
