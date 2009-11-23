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
        /// Constructor: Parameter is to establish the Name of this Database.
        /// </summary>
        /// <param name="name"></param>
        public Database(String name)
        {
            this._Name = name;
            _Tables = new Table[1];
        }

        /// <summary>
        /// Accessor: returns the Name of this Database Object. Non mutable.
        /// </summary>
        public String Name
        { get {  return this._Name; } }

        /// <summary>
        /// Accessor: returns Table by matching the Table.Name to
        /// the parameter provided.
        /// </summary>
        /// <param name="index">String name parameter to 
        /// match the Table name to seek by.</param>
        /// <returns></returns>
        public Table GetTable(int index)
        { return this._Tables[index]; }

        /// <summary>
        /// Accessor: returns Table by matching the collection of 
        /// Tables to the index parameter provided.
        /// </summary>
        /// <param name="name">int index parameter to 
        /// match the Table index to seek by.</param>
        /// <returns></returns>
        public Table GetTable(String name)
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
        /// <param name="newTable">Table objet to be added 
        /// to this Database's Collection of Tables.</param>
        /// <returns>true if successful, false if not.</returns>
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