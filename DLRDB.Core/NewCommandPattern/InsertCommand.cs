using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.NewCommandPattern
{
    class InsertCommand : Command
    {
        private static int INSERT_PARAM_INDEX = 1;
        private StreamWriter _Writer;

        public override bool RunFor(string input)
        {
            return input.Split(' ')[0] == ("insert");
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            String[] commands = command.Split(' ');
            this._Writer = dbEnvironment.Writer;

            Row newRow = null;
            DateTime start = DateTime.Now;
            String[] insertParams = Regex.Split(commands[INSERT_PARAM_INDEX], ",");
            if (insertParams.Length == 2)
            {
                newRow = table.NewRow();
                newRow.Fields[1].Value = newRow.Fields[1].NativeToBytes(insertParams[0]);
                newRow.Fields[2].Value = newRow.Fields[2].NativeToBytes(Int32.Parse(insertParams[1]));
                table.Insert(newRow, dbEnvironment.CurrentTransaction);

                DateTime end = DateTime.Now;
                TimeSpan theSpan = end - start;
                this._Writer.WriteLine(DateTime.Now + " >>> " + "insert takes " + theSpan.TotalMilliseconds + " ms");
                this._Writer.WriteLine(DateTime.Now + " >>> " + "1 row inserted");
                this._Writer.WriteLine(" ");
            }
            else
            {
                this._Writer.WriteLine(DateTime.Now + " >>> Parameter provided is insufficient");
                this._Writer.WriteLine(" ");
            }

            this._Writer.Flush();
        }
    }
}
