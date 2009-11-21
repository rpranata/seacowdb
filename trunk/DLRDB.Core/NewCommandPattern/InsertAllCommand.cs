using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    class InsertAllCommand : Command
    {
     
        public override bool RunFor(string input)
        {
            return input.Split(' ')[0] == "insertall";
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            String response = "";
            Row myNewRow = null;
            int numOfRowsToInsert = 10000000;
            DateTime start = DateTime.Now;

            for (int i = 1; i <= numOfRowsToInsert; i++)
            {
                myNewRow = table.NewRow();
                myNewRow.Fields[1].Value = myNewRow.Fields[1].NativeToBytes("NewName");
                myNewRow.Fields[2].Value = myNewRow.Fields[2].NativeToBytes(10 + (i % 10));

                table.Insert(myNewRow, dbEnvironment.CurrentTransaction);

                dbEnvironment.Writer.Write(".");
                if (i % 80 - (3 + ("" + numOfRowsToInsert).Length) == 0)
                {
                    dbEnvironment.Writer.WriteLine(" (" + i + ")");
                }
                dbEnvironment.Writer.Flush();
            }


            DateTime end = DateTime.Now;
            TimeSpan theSpan = end - start;

            dbEnvironment.Writer.WriteLine(DateTime.Now + " >>> " + "insert takes " + theSpan.TotalMilliseconds + " ms");
            dbEnvironment.Writer.WriteLine(DateTime.Now + " >>> " + Environment.NewLine + response);
            dbEnvironment.Writer.Flush();
        }
    }
}
