using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class InsertCommand : Command
    {
        private List<Field> _ListInserted;

        public InsertCommand(ITable table,List<Field> listInserted)
            :base(table)
        {
            this._ListInserted = listInserted;
        }

        #region ICommand Members

        public override String ExecuteToMemory()
        {
            //String tempResult = "";

            //if (base._TheTable.Insert(this._ListInserted))
            //{
            //    tempResult = "Row has been successfully inserted";
            //}
            //else
            //{
            //    tempResult = "There was an error occured when trying to insert the row";
            //}
            
            //return tempResult;
            
            throw new NotImplementedException();

        }

        public override String ExecuteToFile()
        {
            throw new NotImplementedException();
        }

        public override void UnexecuteFromFile()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
