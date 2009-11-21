using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    class UpdateCommand : Command
    {
        private static int UPDATE_PARAM_INDEX = 1;
        
        public override bool RunFor(string input)
        {
            return input.Split(' ')[0] == ("update");
        }

        public override void Run(string command, Table table, DbEnvironment dbEnvironment)
        {
            String[] commands = command.Split(' ');
           
            //Supported format : 
            //-> UPDATE 1-2,dany,22
            //-> UPDATE *,hoon,22
            //-> UPDATE *,rendy,
            int updatedRows = 0;

            // Split by comma
            // ==================
            String[] updateParams = Regex.Split(commands[UPDATE_PARAM_INDEX], ",");

            // Allocate for (length - 1), because one of the element of arrSplitByComma will be the "range" 
            // - which is about to be ignored
            // Moreover, we add the number of element by 1, to allocate for the auto generated ID
            Object[] arrUpdatedValue = new Object[(updateParams.Length - 1) + 1];
            for (int i = 1; i < updateParams.Length; i++)
            {
                arrUpdatedValue[i] = updateParams[i];
            }

            if (updateParams[UPDATE_PARAM_INDEX] == "*")
            {
                updatedRows = table.UpdateAll(dbEnvironment.CurrentTransaction, arrUpdatedValue);

            }
            else
            {
                String[] arrRange = Regex.Split(updateParams[0], "-");
                Int32 startIndex = Convert.ToInt32(arrRange[0]);
                Int32 endIndex = Convert.ToInt32(arrRange[1]);

                updatedRows = table.Update(startIndex, endIndex, dbEnvironment.CurrentTransaction, arrUpdatedValue);

            }

            dbEnvironment.Writer.WriteLine(DateTime.Now + " >>> " + "Finish updating " + updatedRows + " row(s)");
            dbEnvironment.Writer.Flush();
        }
    }
}
