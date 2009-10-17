using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DLRDB.Core.ConcurrencyUtils;
using DLRDB.Core.Exceptions;
using System.Threading;

namespace DLRDB.Core.DataStructure
{
    /// <summary>
    /// Data Structure used to represent a Table in memory.
    /// </summary>
    public class Table
    {
        #region Initialisers

        private const String TRADEMARK = "SEACOW";

        private const int METADATA_TRADEMARK_LENGTH = 6;
        private const int METADATA_MAJOR_VERSION_LENGTH = 1;
        private const int METADATA_MINOR_VERSION_LENGTH = 1;
        private const int METADATA_DETAIL_VERSION_LENGTH = 1;        
        private const int METADATA_NUM_ROWS_LENGTH = 4;
        private const int METADATA_NUM_USED_PHYSICAL_ROWS_LENGTH = 4;
        private const int METADATA_NUM_AVAILABLE_PHYSICAL_ROWS_LENGTH = 4;
        private const int METADATA_NEXT_PK_LENGTH = 4;
        
        public const int METADATA_TOTAL_LENGTH =
            METADATA_TRADEMARK_LENGTH + METADATA_MAJOR_VERSION_LENGTH + METADATA_MINOR_VERSION_LENGTH +
            METADATA_DETAIL_VERSION_LENGTH + METADATA_NUM_ROWS_LENGTH + METADATA_NEXT_PK_LENGTH +
            METADATA_NUM_USED_PHYSICAL_ROWS_LENGTH + METADATA_NUM_AVAILABLE_PHYSICAL_ROWS_LENGTH;

        private const int ROW_SIZE_INCREMENT = 1000;
        private const int MIN_THRESHOLD_TO_RESIZE_ROWS = 0;

        private WeakReference[] _Rows;
        private readonly Column[] _Columns;
        private readonly String _Name;
        private readonly String _FileName;

        private byte _MajorVersion;
        private byte _MinorVersion;
        private byte _DetailVersion;

        private int _ActualRows;    // Rows that are not Flagged TRASH
        private int _NextPK;        // Next available Primary Key
        private int _PhysicalRows;  // Rows on file occupied by real Rows
        private int _PotentialRows; // Potential unoccupied Rows on file

        private readonly FileStream _MyFileStream;

        private readonly ReadWriteLock _TableLock;

        private readonly Object _Lock = new Object();

        #endregion

        /// <summary>
        /// Constructor. Will perform a read of the metadata in the
        /// file to determine parameters for the construction of rows.
        /// </summary>
        /// <param name="name">Name of Table. Not mutable.</param>
        /// <param name="filename">Name of File. Not mutable.</param>
        public Table(String name, String filename)
        {   
            // TODO: Open the file
            // read in the number of rows
            // read in the file version - check

            this._TableLock = new ReadWriteLock();

            this._Name = name;
            this._FileName = filename;
            
            // Constructs columns (Currently hardcoded)
            // ===================
            this._Columns = new Column[3];

            this._Columns[0] = new Column("ID",typeof(System.Int32), 4);
            this._Columns[1] = new Column
                ("Name", typeof(System.String), 20);
            this._Columns[2] = new Column
                ("Age", typeof(System.Int32), 4);

            this._MyFileStream = new FileStream(this._FileName, 
                FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        
            #region "ReadMetadata"

            // ReadMetadata();
            // ==============

            this._MyFileStream.Seek(0, SeekOrigin.Begin);

            // Conditional checks if the file is a valid seacow file
            if (TRADEMARK == ASCIIEncoding.ASCII.GetString
                (ReadBytesFromDisk(this._MyFileStream,
                    METADATA_TRADEMARK_LENGTH)))
            {
                this._MajorVersion = ReadByteFromDisk(this._MyFileStream);
                this._MinorVersion = ReadByteFromDisk(this._MyFileStream);
                this._DetailVersion = ReadByteFromDisk(this._MyFileStream);

                this._ActualRows = BitConverter.ToInt32
                    (ReadBytesFromDisk(this._MyFileStream,
                        METADATA_NUM_ROWS_LENGTH), 0);

                this._NextPK = BitConverter.ToInt32
                    (ReadBytesFromDisk(this._MyFileStream,
                        METADATA_NEXT_PK_LENGTH), 0);

                this._PhysicalRows = BitConverter.ToInt32
                    (ReadBytesFromDisk(this._MyFileStream,
                        METADATA_NUM_USED_PHYSICAL_ROWS_LENGTH), 0);

                this._PotentialRows = BitConverter.ToInt32
                    (ReadBytesFromDisk(this._MyFileStream,
                        METADATA_NUM_USED_PHYSICAL_ROWS_LENGTH), 0);

                this._Rows = new WeakReference[this._PhysicalRows
                    + this._PotentialRows];
                for (int i = 0; i < _Rows.Length; i++)
                {
                    _Rows[i] = new WeakReference(null);
                }
            }
            else
            { throw new Exception("Seacow file not found"); }

            #endregion

            // ===========

        }

        /// <summary>
        /// This call to this procedure must be surrounded by LOCK
        /// </summary>
        private void UpdateMetadata()
        {
           
                this._MyFileStream.Seek(0, SeekOrigin.Begin);

                this._MyFileStream.Write(ASCIIEncoding.Default.GetBytes
                    (TRADEMARK), 0, METADATA_TRADEMARK_LENGTH);

                this._MyFileStream.Write(System.BitConverter.GetBytes
                    (this._MajorVersion), 0, METADATA_MAJOR_VERSION_LENGTH);

                this._MyFileStream.Write(System.BitConverter.GetBytes
                    (this._MinorVersion), 0, METADATA_MINOR_VERSION_LENGTH);

                this._MyFileStream.Write(System.BitConverter.GetBytes
                    (this._DetailVersion), 0, METADATA_DETAIL_VERSION_LENGTH);

                this._MyFileStream.Write(System.BitConverter.GetBytes
                     (this._ActualRows), 0, METADATA_NUM_ROWS_LENGTH);

                this._MyFileStream.Write(System.BitConverter.GetBytes
                    (this._NextPK), 0, METADATA_NEXT_PK_LENGTH);

                this._MyFileStream.Write(System.BitConverter.GetBytes
                    (this._PhysicalRows), 0,
                        METADATA_NUM_USED_PHYSICAL_ROWS_LENGTH);

                this._MyFileStream.Write(System.BitConverter.GetBytes
                    (this._PotentialRows), 0,
                    METADATA_NUM_AVAILABLE_PHYSICAL_ROWS_LENGTH);
           
        }

        /// <summary>
        /// Gets the Table Name. String value.
        /// </summary>
        public String Name
        { get { return this._Name; } }

        public Column[] Columns
        { get { return this._Columns; } }

        /// <summary>
        /// Select operation based on lowRange and highRange parameters.
        /// The database is structured that the first ID begins with a 1,
        /// and auto increments.
        /// </summary>
        /// <param name="lowRange">The low Range to start the seek from.
        /// </param>
        /// <param name="highRange">The high Range to start the seek from.
        /// </param>
        /// <returns>Row Array of results.</returns>
        public void Select(int lowRange, int highRange, TextWriter output)
        {
            // Conditional to establish if the range is valid.
            if (ValidateSelectRange(lowRange, highRange))
            {
                Row[] results = new Row[highRange - lowRange + 1];
                Row read;

                for (int i = (lowRange - 1); i <= (highRange - 1); i++)
                {
                    this._TableLock.AcquireReader();
                    lock (this._Lock)
                    {
                        read = _Rows[i].Target as Row;
                        if (read == null)
                        {
                            //this._TableLock.ReleaseReader();
                            read = ReadRowFromDisk(i);
                            //this._TableLock.AcquireReader();
                        }
                    }


                    if (read.State == RowStateFlag.CLEAN)
                    {
                        read.OutputTo(output);
                    }

                    read = null;

                    this._TableLock.ReleaseReader();
                }
                //return results;
            }
            else
            {
                throw new SelectException(
                    "Invalid range supplied for Select operation.");
            }

        }

        /// <summary>
        /// Assume that the lowRange and HighRange has been validated
        /// This will be called by the Update and Delete only !!!!
        /// </summary>
        /// <param name="lowRange"></param>
        /// <param name="highRange"></param>
        /// <returns></returns>
        private Row[] Select(int lowRange, int highRange)
        {
           
                Row[] results = new Row[highRange - lowRange + 1];
                Row read;

                int index = 0;

                for (int i = (lowRange - 1); i <= (highRange - 1); i++)
                {
                    this._TableLock.AcquireReader();
                    lock (this._Lock)
                    {
                        read = this._Rows[i].Target as Row;
                        if (read == null)
                        {
                            //this._TableLock.ReleaseReader();
                            read = ReadRowFromDisk(i);
                            //this._TableLock.AcquireReader();
                        }
                       
 					}

                    if (read.State == RowStateFlag.CLEAN)
                    {
                        results[index] = read;
                        index++;
                    }

                    this._TableLock.ReleaseReader();
                }

                return results;
            
        }



        private Row ReadRowFromDisk(int i)
        {
            Row result = null;
            
            result = new Row(this, i + 1, this._MyFileStream);

            //TODO:REDO!
            result.ReadFromDisk();

            lock (this._Lock)
            {
                this._Rows[i].Target = result;
            }
            
            return result;
            
        }

        /// <summary>
        /// Internal function to validate the supplied ranges.
        /// It checks if the end index is lesser that the start index,
        /// or if the end index is beyond the number of physical rows.
        /// The use of positive integers for the ranges are enforced
        /// here as well
        /// </summary>
        /// <param name="startIndex">The indicated start index.</param>
        /// <param name="endIndex">The indicated end index.</param>
        /// <returns>true if the range is valid, false if not.</returns>
        private Boolean ValidateSelectRange(int startIndex, int endIndex)
        {
            bool isValid = true;

            int tempNumOfUsedPhysicalRows;

            lock (this._Lock)
            {
                tempNumOfUsedPhysicalRows = this._PhysicalRows;
            }

            if (endIndex >= startIndex)
            {
                if ((tempNumOfUsedPhysicalRows == 0))
                { isValid = false; }
                else
                {
                    if ((startIndex < 1) || (endIndex < 1))
                    { isValid = false; }
                    else if (tempNumOfUsedPhysicalRows < endIndex)
                    { isValid = false; }
                    else if (tempNumOfUsedPhysicalRows < startIndex)
                    { isValid = false; }
                }
            }
            else
            { isValid = false; }

            return isValid;
        }

        /// <summary>
        /// Function to select and return all rows with a CLEAN state 
        /// in the table. Automatically defaults the start range to 1
        /// and the end range to the number of physical rows.
        /// </summary>
        /// <returns>Row Array of results.</returns>
        public void SelectAll(TextWriter output)
        { 
            Select(1,this._PhysicalRows, output); 
        }

        /// <summary>
        /// Update operation based on range of affected or affectable
        /// rows, and the values to update to.
        /// </summary>
        /// <param name="lowRange">The start range to seek and update by.
        /// </param>
        /// <param name="highRange">The end range to seek and update by.
        /// </param>
        /// <param name="arrValueUpdates">The values to update to.
        /// </param>
        /// <returns>The number of affected rows.</returns>
        public int Update(int lowRange, int highRange, params Object [] arrValueUpdates)
        {
            int numberOfAffectedRows = 0;
            
            if (ValidateSelectRange(lowRange, highRange))
            {
                Row[] arrSelectedRows = Select(lowRange, highRange);

                // To indicate whether in a row, we have some changes

                Boolean isChangesMade = false;

                int lastRowNumber = -1;

                foreach (Row tempRow in arrSelectedRows.Where(tempRow => tempRow != null))
                {
                    isChangesMade = false;

                    this._TableLock.AcquireReader();
                    //tempRow.RowMemoryLock.AcquireWriter();

                    // Start from index 1 (because index 0 is the ID)
                    for (int i = 1; i < this.Columns.Length; i++)
                    {
                        if (arrValueUpdates[i] != null)
                        {
                            isChangesMade = true;
                            try
                            {
                                tempRow.Fields[i].Value = tempRow.Fields[i]
                                    .NativeToBytes(arrValueUpdates[i]);
                            }
                            catch (Exception ex)
                            {
                                int a = 10;

                            }
                        }
                    }

                    if (isChangesMade)
                    {
                        numberOfAffectedRows++;

                        tempRow.State = RowStateFlag.CLEAN;
                        tempRow.WriteToDisk();
                        
                    }

                    lastRowNumber = tempRow.RowNum;
                    //tempRow.RowMemoryLock.ReleaseWriter();
                    this._TableLock.ReleaseReader();
                }                
            }
            else
            {
                throw new UpdateException
                    ("Invalid range supplied for Update operation.");
            }

            return numberOfAffectedRows;
        }

        public int UpdateAll(params Object[] arrValueUpdates)
        {
            return Update(1, this._PhysicalRows, arrValueUpdates);
        }

        /// <summary>
        /// Delete operation based on range of supplied start and end
        /// ranges. Delete in this version of the system changes the 
        /// row's State to TRASH, which flags it for deletion. The row
        /// affected such will no longer exist in regards to any other
        /// queries thereafter.
        /// </summary>
        /// <param name="lowRange">The start range to seek and delete by.
        /// </param>
        /// <param name="highRange">The end range to seek and delete by.
        /// </param>
        /// <returns></returns>
        public int Delete(int lowRange, int highRange, TextWriter output)
        {
            int numberOfAffectedRows = 0;

            if (ValidateSelectRange(lowRange, highRange))
            {
                Row[] arrSelectedRows = Select(lowRange, highRange);

                // To indicate whether in a row, we have some changes

                foreach (Row tempRow in arrSelectedRows.Where(tempRow => tempRow != null))
                {
                    numberOfAffectedRows++;
                    
                    this._TableLock.AcquireReader();
                    //tempRow.RowMemoryLock.AcquireWriter();
                   
                    tempRow.State = RowStateFlag.TRASH;
                    tempRow.WriteToDisk();
                    
                    //tempRow.RowMemoryLock.ReleaseWriter();
                    this._TableLock.ReleaseReader();
                }

            }
            else
            {
                throw new DeleteException
                    ("Invalid range supplied for Delete operation.");
            }
            return numberOfAffectedRows;
        }
        
        public int RowCount()
        {
            int tempResult;

            lock (this._Lock)
            {
                tempResult = this._Rows.Length;
            }

            return tempResult; 
        }

        public int DeleteAll(TextWriter output)
        {
            return Delete(1, this._PhysicalRows, output);
        }

        /// <summary>
        /// Function for the Table to create and return a new Row object.
        /// </summary>
        /// <returns>A new Row object with empty Fields based on
        /// the Table's Column defintion.</returns>
        public Row NewRow()
        {
            Field[] tempField = new Field[this.Columns.Length];
            
            int index = 0;
            foreach (Column tempColumn in this.Columns)
            {
                if (tempColumn.NativeType == typeof(System.Int32))
                { tempField[index] = new Int32Field (tempColumn); }
                else if (tempColumn.NativeType == typeof(System.String))
                { tempField[index] = new StringField (tempColumn); }
                index++;
            }

            Row tempNewRow = new Row(this, this._MyFileStream);
            tempNewRow.State = RowStateFlag.ADDED;

            return tempNewRow;
        }

        /// <summary>
        /// Insert operation based on the Row object parameter. Appends
        /// the new Row to the end of the Table.
        /// </summary>
        /// <param name="row">Row to be added to the Table.</param>
        /// <returns></returns>
        public Row InsertRow(Row row)
        {
            this._TableLock.AcquireWriter();

            int tempNumOfAvailablePhysicalRows;
            lock (this._Lock)
            {
                tempNumOfAvailablePhysicalRows = this._PotentialRows;
            }
            
            if (tempNumOfAvailablePhysicalRows <= MIN_THRESHOLD_TO_RESIZE_ROWS)
            {
                // grow the table
                GrowTable();
            }

            // Set the next auto incement ID

            int tempNextPK;
            lock (this._Lock)
            {
                tempNextPK = this._NextPK;
            }
            row.Fields[0].Value = row.Fields[0].NativeToBytes(tempNextPK);

            int tempNumOfUsedPhysicalRows;
            lock (this._Lock)
            {
                tempNumOfUsedPhysicalRows = this._PhysicalRows;
            }
            row.RowNum = tempNumOfUsedPhysicalRows + 1;
            
            // Once we write it to disk, its state will become CLEAN
            row.State = RowStateFlag.CLEAN;
            row.WriteToDisk();

            lock (this._Lock)
            {
                this._ActualRows++;
                this._PhysicalRows++;
                this._PotentialRows--;
                this._NextPK++;
         
                this.UpdateMetadata();
          
                // Put the row that has just been inserted
                this._Rows[this._PhysicalRows - 1].Target = row;
            }

            // Thread.Sleep(3000);
            this._TableLock.ReleaseWriter();

            return row;
        }

        /// <summary>
        /// Function to grow the table by increments of 1000. This action
        /// is performed to both the collection of Rows in Memory and the
        /// file on the system.
        /// </summary>
        private void GrowTable()
        {
            // Updating the metadata
            int newPotentialRows;
            int newTotalRows;

            lock (this._Lock)
            {
                // Persist the old number of physical rows
                newPotentialRows = this._PhysicalRows; 
                
                // Multiplying the size of the file by factor of two
                newTotalRows = newPotentialRows + this._PhysicalRows;

                // Just in case, at the first time, we have no rows at all
                if (newTotalRows <= 0)
                {
                    newTotalRows = 1000;
                    newPotentialRows = 1000;
                }

                // Sets the File size to correspond to data
                this._MyFileStream.SetLength(METADATA_TOTAL_LENGTH
                    + newTotalRows * GetBytesLengthPerRow());

                // Persists the new amount of Potential 
                // Rows to reflect File
                this._PotentialRows = newPotentialRows;
            
                this.UpdateMetadata();

            
                // Create the new sized collection 
                // and copy the old collection over
                WeakReference[] tempRows = new WeakReference[newTotalRows];

                this._Rows.CopyTo(tempRows, 0);
           

                for (int i = _Rows.Length; i < tempRows.Length; i++)
                {
                    tempRows[i] = new WeakReference(null);
                }

                // Restore it back
                this._Rows = tempRows;
                
            }

        }

        /// <summary>
        /// We assume that the pointer has already been moved to the "appropriate" position
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static Byte[] ReadBytesFromDisk(FileStream myFileStream,int count)
        {
            Byte [] arrResult = new Byte[count];
            myFileStream.Read(arrResult, 0, count);
            return arrResult;
        }

        public static Byte ReadByteFromDisk(FileStream myFileStream)
        { return Table.ReadBytesFromDisk(myFileStream,1)[0]; }

        public static void WriteBytesToDisk(FileStream myFileStream, Byte[] arrData, int count)
        { myFileStream.Write(arrData,0,count); }

        public static void WriteByteToDisk(FileStream myFileStream, Byte data)
        { myFileStream.WriteByte(data); }

        private int GetBytesLengthPerRow()
        {
            int totalBytes = 0;
            totalBytes += Row.ROWSTATE_LENGTH;
            foreach (Column tempColumn in this.Columns)
            {
                // Calculate the total length for this row
                totalBytes += tempColumn.Length;
            }
            return totalBytes;
        }
    }
}
#region oldstuff
/*
        /// <summary>
        /// Select operation based on lowRange and highRange parameters.
        /// The database is structured that the first ID begins with a 1,
        /// and auto increments.
        /// </summary>
        /// <param name="lowRange">The low Range to start the seek from.
        /// </param>
        /// <param name="highRange">The high Range to start the seek from.
        /// </param>
        /// <returns>Row Array of results.</returns>
        public Row[] Select(int lowRange, int highRange)
        {            
            // Conditional to establish if the range is valid.
            if (ValidateSelectRange(lowRange,highRange))
            {               
                Row[] results = new Row[highRange - lowRange + 1];
              
                int index = 0;

                for (int i = (lowRange-1); i <= (highRange-1); i++)
                {
                    bool isRowNull = false;

                    this._TableLock.ReaderLock();                        
                        if (this._Rows[i] == null)
                        {
                            isRowNull = true;
                        }
                    this._TableLock.ReleaseReader();

                    if (isRowNull)
                    {
                        this._TableLock.WriterLock();
                            this._Rows[i] = new Row(this, i + 1, 
                                this._MyFileStream);
                        this._TableLock.ReleaseWriter();
                    }

                    this._TableLock.ReaderLock();

                        if (isRowNull)
                        {
                            this._Rows[i].RowMemoryLock.WriterLock();
                                this._Rows[i].ReadFromDisk();
                            this._Rows[i].RowMemoryLock.ReleaseWriter();
                        }

                        this._Rows[i].RowMemoryLock.ReaderLock();
                            if (this._Rows[i].State == RowStateFlag.CLEAN)
                            {
                                results[index] = this._Rows[i];
                                index++;
                            }
                            else
                            {
                                Console.WriteLine("this._Rows[" + i 
                                    + "].State is not CLEAN, instead => " 
                                    + this._Rows[i].State);
                            }
                        this._Rows[i].RowMemoryLock.ReleaseReader();
                    
                    this._TableLock.ReleaseReader();
                }
                return results;
            }
            else
            {
                throw new SelectException(
                    "Invalid range supplied for Select operation.");
            }

        }

///// <summary>
///// Gets the index of the Field based on the name. Used for referencing.
///// </summary>
///// <param name="fieldName">Field Name to seek by.</param>
///// <returns>Integer Index associated with the Field Name for this Table.</returns>
//public Int32 getFieldIndexByNamef(String fieldName)
//{
//    int index = -1;

//    if (this._DictFieldDefinition.TryGetValue(fieldName, out index) == false)
//    {
//        index = -1;
//    }

//    return index;
//}

/// <summary>
/// Gets all referenced Rows by the List of Fields passed in as a criteria. Does checking to ensure the Row is in a DEFAULT State. Criteria involves all data that comes AFTER the WHERE keyword in SQL Syntax.
/// </summary>
/// <param name="criteria">List of Row Objects indication which Column should have which Value to seek by.</param>
/// <returns>List of Row Objects that meet the seek criteria(s).</returns>
/*private List<Row> getRowsByCriteria(List<Field> criteria)
{
    List<Row> listResult = new List<Row>();
            
    bool isCriteriaMatched = true;

    foreach (KeyValuePair<int, Row> kvpRow in this._Rows)
    {
        isCriteriaMatched = true;

        if (kvpRow.Value.StateFlag == RowStateFlag.CLEAN)
        {
            foreach (Field criteriaField in criteria)
            {
                if (kvpRow.Value.GetField(criteriaField.FieldColumn.Name).Value.Equals(criteriaField.Value) == false)
                {
                    isCriteriaMatched = false;
                    break;
                }
            }
        }
        else
        {
            // This row is deleted, we're not going to process it
            isCriteriaMatched = false;
        }

        if (isCriteriaMatched)
        {
            listResult.Add(kvpRow.Value);
        }
    }

    return listResult;
}*/

/// <summary>
/// This function assumes the AND operator for the list of criteria given. Uses the GetRowsByCriteria(referenced Rows) method, parses through all results and reconstructs a new Row based on the data provided to ensure the original data integrity.
/// </summary>
/// <param name="selectedFields">List of Fields to indication which Columns we want returned. In SQL Syntax, this refers to input between SELECT and FROM keywords.</param>
/// <param name="criteria">List of Fields to indicated what is/are the search criteria(s) by Column and Value pairs.</param>
/// <returns></returns>
//public List<Row> Select(List<String> selectedFields, List<Field> criteria)
//{
//    List<Row> listResult = new List<Row>();
//    List<Row> listMatchedResult = getRowsByCriteria(criteria);
//    Row tempReturnedRow = null;

//    foreach (Row matchedRow in listMatchedResult)
//    {
//        Dictionary<String, Field> tempDictReturnedField = new Dictionary<String, Field>();

//        foreach (String tempField in selectedFields)
//        {
//            tempDictReturnedField.Add(tempField, matchedRow.GetField(tempField));
//        }

//        tempReturnedRow = new Row(tempDictReturnedField);
//        listResult.Add(tempReturnedRow);
//    }


//    return listResult;

//}

//public List<Row> Select(int startIndex, int endIndex)
//{
//    List<Row> listResult = new List<Row>();
//    Row[] listMatchedResult = selectRange(startIndex,endIndex);
//    Row tempReturnedRow = null;

//    foreach (Row matchedRow in listMatchedResult)
//    {
//        Dictionary<String, Field> tempDictReturnedField = new Dictionary<String, Field>();

//        foreach (Column tempColumn in this._ListColumns)
//        {
//            tempDictReturnedField.Add(tempColumn.Name, matchedRow.GetField(tempColumn.Name));
//        }

//        tempReturnedRow = new Row(tempDictReturnedField);
//        listResult.Add(tempReturnedRow);
//    }


//    return listResult;

//}


//public Dictionary<Int32, Row> Select(List<Field> selectedFields)
//{
//    return this._Rows;
//}

/// <summary>
/// Insert command. A List of Fields are given, a new Row is created, and inserted to the Table's Dictionary of Rows. No validation is performed in regardsin to maintaing the integrity of Field Types(Int32, String), or whether certain Fields need to be of a certain type. 
/// </summary>
/// <param name="insertedFields">List of Fields for to created the new Row.</param>
/// <returns>True if the operation succeeds, False if not.</returns>
//public bool Insert(List<Field> insertedFields)
//{
//    bool isSuccess = false;

//    try
//    {
//        Dictionary<String, Field> tempDictField = new Dictionary<string, Field>();
//        foreach (Field tempField in insertedFields)
//        {
//            tempDictField.Add(tempField.FieldColumn.Name, tempField);
//        }

//        Row newRow = new Row(tempDictField);
//        newRow.StateFlag = RowStateFlag.CLEAN;

//        this._Rows.Add(_Rows.Count,newRow);
//        newRow.ParentTable = this;

//        isSuccess = true;
//    }
//    catch (Exception)
//    {
//    }

//    return isSuccess;
//}

/// <summary>
/// Update command. Performs a select based on criteria derived from the original Row data, then matches it's Fields to the updatedFields, and changes the values. Validation is done to ensure that the user CAN'T update a Row flagged as DELETED.
/// </summary>
/// <param name="updatedFields">List of Fields containing the NEW data for the Row.</param>
/// <param name="criteria">List of Fields to allow seeking of the ORIGINAL Row, and which Fields are to be affected up the Update.</param>
/// <returns></returns>
//public bool Update(List<Field> updatedFields, List<Field> criteria)
//{
//    bool isSuccess = false;

//    List<Row> listMatchedResult = getRowsByCriteria(criteria);

//    try
//    {
//        if (listMatchedResult.Count > 0)
//        {
//            foreach (Row matchedRow in listMatchedResult)
//            {
//                foreach (Field updatedField in updatedFields)
//                {
//                    matchedRow.GetField(updatedField.FieldColumn.Name).Value = updatedField.Value;
//                }
//            }

//            isSuccess = true;
//        }
//    }
//    catch (Exception)
//    {
//    }

//    return isSuccess;
//}

/// <summary>
/// Delete Function. List of Fields parameter is to indicate the seek criteria for the Row(s) to be flagged for DELETED. Note: If a generic List of Fields is provided, ALL Rows which match the criteria will be flagged as DELETED.
/// </summary>
/// <param name="criteria">List of Fields to indicate the seek criteria.</param>
/// <returns>True if successful, False if not.</returns>
//public bool Delete(List<Field> criteria)
//{
//    bool isSuccess = false;

//    List<Row> listMatchedResult = getRowsByCriteria(criteria);

//    try
//    {
//        if (listMatchedResult.Count > 0)
//        {
//            foreach (Row matchedRow in listMatchedResult)
//            {
//                matchedRow.StateFlag = RowStateFlag.TRASH;
//            }

//            isSuccess = true;
//        }
//    }
//    catch (Exception)
//    {
//    }

//    return isSuccess;
//}

/// <summary>
/// Adds a Field to the Table Definition, Index is auto allocated based on the current number of Key Value Pairs, similar to an AUTO INCREMENT.
/// </summary>
/// <param name="fieldName"></param>
/// <param name="fieldType"></param>
//public bool AddColumn(String columnName,System.Type fieldType, Int32 length)
//{
//    // Alter Table?
//    /*Column newColumn = new Column(columnName, fieldType, length);
//    this._ListColumns.Add(newColumn);
//    return newColumn;*/

//    bool isSuccess = false;
//    try
//    {
//        Column newColumn = new Column(columnName, fieldType, length);
//        Column[] newColumns = new Column[this._Columns.Length + 1];
//        this._Columns.CopyTo(newColumns, 0);
//        newColumns[_Tables.Length] = newColumns;
//        this._Columns = newColumns;
//        isSuccess = true;
//    }
//    catch (Exception) { }

//    return isSuccess;
//}

//public Dictionary <Int32, Field> convertToIndexedColumn(List<Field> criteria)
//{
//    Dictionary<Int32, Field> dictCriteriaValuePair = new Dictionary<int, Field>();
//    foreach (Field tempField in criteria)
//    {
//        dictCriteriaValuePair.Add(this.getFieldIndexByName(tempField.Name), tempField);
//    }

//    return dictCriteriaValuePair;
//}

//public List<Column> Columns
//{
//    get
//    {
//        return this.Columns;
//    }
//}
#endregion
