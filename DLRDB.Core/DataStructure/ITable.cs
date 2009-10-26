using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.DataStructure
{
    public interface ITable
    {        
        /// <summary>
        /// This function assumes the AND operator for the list of criteria given. Uses the GetRowsByCriteria(referenced Rows) method, parses through all results and reconstructs a new Row based on the data provided to ensure the original data integrity.
        /// </summary>
        /// <param name="selectedFields">List of Fields to indication which Columns we want returned. In SQL Syntax, this refers to input between SELECT and FROM keywords.</param>
        /// <param name="criteria">List of Fields to indicated what is/are the search criteria(s) by Column and Value pairs.</param>
        /// <returns></returns>
        //List<Row> FetchRows(List<String> selectedFields, List<Field> criteria);

        Row[] Select(int startIndex, int endIndex);
        
        /// <summary>
        /// Insert command. A List of Fields are given, a new Row is created, and inserted to the Table's Dictionary of Rows. No validation is performed in regardsin to maintaing the integrity of Field Types(Int32, String), or whether certain Fields need to be of a certain type. 
        /// </summary>
        /// <param name="insertedFields">List of Fields for to created the new Row.</param>
        /// <returns>True if the operation succeeds, False if not.</returns>
        //bool Insert(List<Field> insertedFields);
        
        /// <summary>
        /// Update command. Performs a select based on criteria derived from the original Row data, then matches it's Fields to the updatedFields, and changes the values. Validation is done to ensure that the user CAN'T update a Row flagged as DELETED.
        /// </summary>
        /// <param name="updatedFields">List of Fields containing the NEW data for the Row.</param>
        /// <param name="criteria">List of Fields to allow seeking of the ORIGINAL Row, and which Fields are to be affected up the Update.</param>
        /// <returns></returns>
        //bool Update(List<Field> updatedFields, List<Field> criteria);
       
        /// <summary>
        /// Delete Function. List of Fields parameter is to indicate the seek criteria for the Row(s) to be flagged for DELETED. Note: If a generic List of Fields is provided, ALL Rows which match the criteria will be flagged as DELETED.
        /// </summary>
        /// <param name="criteria">List of Fields to indicate the seek criteria.</param>
        /// <returns>True if successful, False if not.</returns>
        //bool Delete(List<Field> criteria);

        int RowCount();
    }
}
