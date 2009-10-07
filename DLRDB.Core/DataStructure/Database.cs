using System;
using System.Collections;
using System.Collections.Generic;

namespace DLRDB.Core.DataStructure
{
    public class Database
    {
        private Table[] _Tables;
        private String _Name;

        /// <summary>
        /// Constructor. Parameter is to establish the Name of this Database.
        /// </summary>
        /// <param name="name"></param>
        public Database(String name)
        {
            this._Name = name;
            _Tables = new Table[1];
        }

        /// <summary>
        /// Gets the Name of this Database Object. Non mutable.
        /// </summary>
        public String Name
        {
            get
            {
                return this._Name;
            }
        }

        /// <summary>
        /// Checks the Collection of Tables associated to this Database and returns the relevant Table based on the parameter.
        /// </summary>
        /// <param name="tableName">String Table Name to seek by.</param>
        /// <returns>Table object that matches the parameter. Returns null if no such Table exists.</returns>
        /*public Table getTableByName(String tableName)
        {
            Table resultTable = null;
            if (this._DictTable.TryGetValue(tableName, out resultTable) == false)
            {
                resultTable = null;
            }

            return resultTable;
        }*/

        public Table getTable(int index)
        {
            return this._Tables[index];
        }

        public Table getTable(String name)
        {
            for (int i = 0; i < _Tables.Length; i++)
            {
                if (this._Tables[i].Name == name)
                { return this._Tables[i]; }
            }
            return null;
        }

        /// <summary>
        /// Adds Table to this Database.
        /// </summary>
        /// <param name="newTable">Table objet to be added to this Database's Collection of Tables.</param>
        /// <returns>True if successful, False if not.</returns>
        public bool AddTable(Table newTable)
        {
            bool isSuccess = false;
            try
            {
                Table[] newTables = new Table[this._Tables.Length + 1];
                this._Tables.CopyTo(newTables, 0);
                newTables[_Tables.Length] = newTable;
                this._Tables = newTables;
                isSuccess = true;
            }
            catch (Exception) { }
            
            return isSuccess;
        }        
    }
}
