using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.CommandPattern
{
    public class SelectCommand:Command
    {
        private List<String> _ListSelected;
        private List<Field> _ListCriteria;
        
        private int _StartIndex;
        private int _EndIndex;
        
        public SelectCommand(ITable table)
            :base(table)
        {
            this._ListSelected = new List<String>();
            this._ListCriteria = new List<Field>();
        }

        public SelectCommand(ITable table,List<String> listSelected, List<Field> listCriteria)
            : this(table)
        {
            this._ListSelected = listSelected;
            this._ListCriteria = listCriteria;
        }

        public SelectCommand(ITable table,int startIndex, int endIndex)
            : this(table)
        {
            this._StartIndex = startIndex;
            this._EndIndex = endIndex;
        }

        #region ICommand Members

        public override String ExecuteToMemory()
        {
            String tempResult = "";

            Row[] results = null;

            // listResult = base._TheTable.Select(this._ListSelected, this._ListCriteria);
            results = base._TheTable.Select(this._StartIndex,this._EndIndex);

            for (int i = 0; i < results.Length; i++) 
            {
                tempResult += Environment.NewLine + results[i].ToString();
            }

            tempResult += Environment.NewLine + " [" + results.Count() + "] rows selected";

            return tempResult;
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
