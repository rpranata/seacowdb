using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public abstract class Command : ICommand
    {
        protected DLRDB.Core.DataStructure.ITable _TheTable;

        public Command(ITable table)
        {
            this._TheTable = table;
        }

        #region ICommand Members

        public abstract String ExecuteToMemory();

        public abstract String ExecuteToFile();

        public abstract void UnexecuteFromFile();

        #endregion
    }
}
