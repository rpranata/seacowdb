using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class UpdateCommand:Command, DLRDB.Core.CommandPattern.IUpdateCommand 
    {
        private List<Field> _ListUpdated;
        private List<Field> _ListCriteria;

        public UpdateCommand(ITable table, List<Field> listUpdated, List<Field> listCriteria)
            : base(table)
        {
            this._ListUpdated = listUpdated;
            this._ListCriteria = listCriteria;
        }

        #region ICommand Members

        public override String ExecuteToMemory()
        {
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
