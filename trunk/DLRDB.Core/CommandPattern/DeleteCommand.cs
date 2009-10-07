using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class DeleteCommand:ICommand
    {
        private List<Field> _ListDeleted;
        private List<Field> _ListCriteria;

        public DeleteCommand(List<Field> listDeleted, List<Field> listCriteria)
        {
            this._ListDeleted = listDeleted;
            this._ListCriteria = listCriteria;
        }

        #region ICommand Members

        public String ExecuteToMemory()
        {
            throw new NotImplementedException();
        }

        public String ExecuteToFile()
        {
            throw new NotImplementedException();
        }

        public void UnexecuteFromFile()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
