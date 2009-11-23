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

        public const int METADATA_TOTAL_LENGTH
            = METADATA_TRADEMARK_LENGTH + METADATA_MAJOR_VERSION_LENGTH 
            + METADATA_MINOR_VERSION_LENGTH + METADATA_DETAIL_VERSION_LENGTH 
            + METADATA_NUM_ROWS_LENGTH + METADATA_NEXT_PK_LENGTH 
            + METADATA_NUM_USED_PHYSICAL_ROWS_LENGTH 
            + METADATA_NUM_AVAILABLE_PHYSICAL_ROWS_LENGTH;

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

        public readonly ReadWriteLock _TableLock;

        private readonly Object _Lock = new Object();

        #endregion

        /// <summary>
        /// Constructor: Method performs a read of the metadata in the
        /// file to determine parameters for the construction of rows.
        /// </summary>
        /// <param name="name">String Name of Table. Not mutable.</param>
        /// <param name="filename">String Name of File. Not mutable.</param>
        public Table(String name, String filename)
        {
            // TODO: Open the file
            // read in the number of rows
            // read in the file version - check

            this._TableLock = new ReadWriteLock();

            this._Name = name;
            this._FileName = filename;

            // Constructs columns (Currently hardcoded)
            this._Columns = new Column[3];

            this._Columns[0] = new Column("ID", typeof(System.Int32), 4);
            this._Columns[1] = new Column("Name", typeof(System.String), 20);
            this._Columns[2] = new Column("Age", typeof(System.Int32), 4);

            this._MyFileStream = new FileStream(this._FileName,
                FileMode.OpenOrCreate, FileAccess.ReadWrite);

            #region "ReadMetadata"

            // Reads the file Metadata, originally a separate method,
            // placed in here for to achieve Thread safety

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
                { _Rows[i] = new WeakReference(null); }
            }
            else
            { throw new Exception("Seacow file not found"); }

            #endregion
        }

        /// <summary>
        /// Method to update the metadata on the file. Note: the call
        /// to this procedure must be surrounded by lock for safety.
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
        /// Accessor: returns the Table Name. Non mutable.
        /// </summary>
        public String Name
        { get { return this._Name; } }

        /// <summary>
        /// Accessor: returns the array of Columns 
        /// associated with this Table. Non mutable.
        /// </summary>
        public Column[] Columns
        { get { return this._Columns; } }

        /// <summary>
        /// Select operation based on lowRange and highRange parameters.
        /// The database is structured that the first ID begins with a 1,
        /// and auto increments.
        /// </summary>
        /// <param name="lowRange">The lowRange to start the seek from.</param>
        /// <param name="highRange">The high Range to seek towards.</param>
        /// <returns>Row Array of results.</returns>
        public void Select(int lowRange, int highRange, 
            Transaction theTransaction, TextWriter output)
        {
            // Conditional to establish if the range is valid.
            if (ValidateSelectRange(lowRange, highRange))
            {
                Row[] results = new Row[highRange - lowRange + 1];
                Row read;

                theTransaction.StartReadTable(this);

                for (int i = (lowRange - 1); i <= (highRange - 1); i++)
                {
                    lock (this._Lock)
                    {
                        read = _Rows[i].Target as Row;
                        if (read == null)
                        { read = ReadRowFromDisk(i); }
                    }

                    theTransaction.StartReadRow(read);

                    if (read.State != RowStateFlag.TRASH)
                    { read.OutputTo(output); }

                    theTransaction.EndReadRow(read);

                    read = null;
                }
                theTransaction.EndReadTable(this);
            }
            else
            { 
                throw new SelectException
                    ("Invalid range supplied for Select operation.");
            }

        }

        /// <summary>
        /// Internal method to obtain Rows by reference for update and
        /// delete operations. Note: Assumption that range has been validated
        /// prior to this procedure call. Returns an array of Rows, NOT 
        /// Weak Reference AS Rows.
        /// </summary>
        /// <param name="lowRange">The lowRange to start the seek from.</param>
        /// <param name="highRange">The highRange to seek towards.</param>
        /// <returns></returns>
        private Row[] FetchRows(int lowRange, int highRange)
        {
            Row[] results = new Row[highRange - lowRange + 1];
            Row read;

            int index = 0;

            for (int i = (lowRange - 1); i <= (highRange - 1); i++)
            {
                lock (this._Lock)
                {
                    read = this._Rows[i].Target as Row;
                    if (read == null)
                    { read = ReadRowFromDisk(i); }
                }

                if (read.State == RowStateFlag.CLEAN)
                {
                    results[index] = read;
                    index++;
                }
            }
            return results;
        }

        /// <summary>
        /// Internal method to read the Row from 
        /// disk based on the index parameter.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private Row ReadRowFromDisk(int index)
        {
            Row result = null;
            result = new Row(this, index + 1, this._MyFileStream);

            //File stream takes care of read lock
            result.ReadFromDisk();

            lock (this._Lock)
            { this._Rows[index].Target = result; }

            return result;
        }

        /// <summary>
        /// Internal function to validate the supplied ranges.
        /// It checks if the end index is lesser that the start index,
        /// or if the end index is beyond the number of physical rows.
        /// The use of positive integers for the ranges are enforced
        /// here as well.
        /// </summary>
        /// <param name="startIndex">The indicated start index.</param>
        /// <param name="endIndex">The indicated end index.</param>
        /// <returns>true if the range is valid, false if not.</returns>
        private Boolean ValidateSelectRange(int startIndex, int endIndex)
        {
            bool isValid = true;
            int tempNumOfUsedPhysicalRows;

            lock (this._Lock)
            { tempNumOfUsedPhysicalRows = this._PhysicalRows; }

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
        public void SelectAll(TextWriter output, Transaction theTransaction)
        { Select(1, this._PhysicalRows, theTransaction, output); }

        /// <summary>
        /// Update operation based on range of affected or affectable
        /// rows, and the values to update to.
        /// </summary>
        /// <param name="lowRange">The lowRange to seek and update by.</param>
        /// <param name="highRange">The highRange to seektowards.</param>
        /// <param name="arrValueUpdates">The values to update to.</param>
        /// <returns>The number of affected rows.</returns>
        public int Update(int lowRange, int highRange, 
            Transaction theTransaction, params Object[] arrValueUpdates)
        {
            int numberOfAffectedRows = 0;

            if (ValidateSelectRange(lowRange, highRange))
            {
                Row[] arrSelectedRows = FetchRows(lowRange, highRange);
                Boolean isChangesMade = false;
                int lastRowNumber = -1;

                foreach (Row tempRow in arrSelectedRows
                    .Where(tempRow => tempRow != null))
                {
                    isChangesMade = false;
                    theTransaction.StartWriteRow(tempRow);
                    Row currentRow = tempRow;

                    // Start from index 1 (because index 0 is the ID)
                    for (int i = 1; i < this.Columns.Length; i++)
                    {
                        if (arrValueUpdates[i] != null)
                        {
                            isChangesMade = true;
                            currentRow.Fields[i].Value = currentRow
                                .Fields[i].NativeToBytes(arrValueUpdates[i]);
                        }
                    }

                    if (isChangesMade)
                    {
                        numberOfAffectedRows++;
                        theTransaction.AddCommitAction(
                            () => { currentRow.WriteToDisk(); } );

                        theTransaction.AddRollbackAction(
                            () => { currentRow.ReadFromDisk(); } );
                    }

                    lastRowNumber = currentRow.RowNum;
                    theTransaction.EndWriteRow(tempRow);
                }
            }
            else
            {
                throw new UpdateException
                    ("Invalid range supplied for Update operation.");
            }
            return numberOfAffectedRows;
        }

        /// <summary>
        /// Function to perform a batch update. Updates ALL Rows with new
        /// values based on parameters.
        /// </summary>
        /// <param name="theTransaction">The current associated Transaction.
        /// </param>
        /// <param name="arrValueUpdates">Object[] of the new values.</param>
        /// <returns></returns>
        public int UpdateAll(Transaction theTransaction, 
            params Object[] arrValueUpdates)
        { 
            return Update(1, this._PhysicalRows, 
                theTransaction, arrValueUpdates);
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
        /// <returns>The number of affected Rows.</returns>
        public int Delete(int lowRange, int highRange, Transaction theTransaction, TextWriter output)
        {
            int numberOfAffectedRows = 0;

            if (ValidateSelectRange(lowRange, highRange))
            {
                Row[] arrSelectedRows = FetchRows(lowRange, highRange);

                // To indicate whether in a row, we have some changes
                theTransaction.StartWriteTable(this);

                foreach (Row tempRow in arrSelectedRows.Where(tempRow => tempRow != null))
                {
                    Row currentRow = tempRow;
                    numberOfAffectedRows++;

                    theTransaction.StartWriteRow(currentRow);

                    currentRow.State = RowStateFlag.TRASH;

                    lock (_Lock)
                    { _ActualRows--; }

                    theTransaction.AddCommitAction(
                    () =>
                    {
                        currentRow.WriteToDisk();
                        lock (this._Lock)
                        { UpdateMetadata(); }
                    });

                    theTransaction.AddRollbackAction(
                    () =>
                    {
                        currentRow.ReadFromDisk();
                        lock (this._Lock)
                        {
                            //undo the -- of the actual rows
                            _ActualRows++;
                            UpdateMetadata();
                        }
                    });

                    theTransaction.EndWriteRow(currentRow);
                }
                theTransaction.EndWriteTable(this);
            }
            else
            {
                throw new DeleteException
                    ("Invalid range supplied for Delete operation.");
            }
            return numberOfAffectedRows;
        }

        /// <summary>
        /// Function to perform a batch Delete operation. Deletes ALL Rows.
        /// Note: _PhysicalRows read outside of lock. This may get stale data,
        /// but will be valid.
        /// </summary>
        /// <param name="theTransaction">The current associated Transaction.
        /// </param>
        /// <param name="output">The associated output stream to write to
        /// Console.</param>
        /// <returns>The number of affected Rows.</returns>
        public int DeleteAll(Transaction theTransaction, TextWriter output)
        { return Delete(1, this._PhysicalRows, theTransaction, output); }

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
                { tempField[index] = new Int32Field(tempColumn); }
                else if (tempColumn.NativeType == typeof(System.String))
                { tempField[index] = new StringField(tempColumn); }
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
        public Row Insert(Row row, Transaction theTransaction)
        {
            theTransaction.StartWriteTable(this);
            int tempNumOfAvailablePhysicalRows;

            lock (this._Lock)
            { tempNumOfAvailablePhysicalRows = this._PotentialRows; }

            if (tempNumOfAvailablePhysicalRows <= MIN_THRESHOLD_TO_RESIZE_ROWS)
            { GrowTable(); }

            // Set the next auto incement ID
            int tempNextPK;
            int tempNumOfUsedPhysicalRows;
            
            // rendy put this
            theTransaction.StartWriteRow(row);

            lock (this._Lock)
            {
                tempNextPK = this._NextPK;
                this._NextPK++;
                tempNumOfUsedPhysicalRows = this._PhysicalRows;
                this._PhysicalRows++;
                this._PotentialRows--;

                row.Fields[0].Value = row.Fields[0].NativeToBytes(tempNextPK);
                row.RowNum = tempNumOfUsedPhysicalRows + 1;

                // Once we write it to disk, its state will become CLEAN
                row.State = RowStateFlag.CLEAN;

                this._ActualRows++;

                // Append the row that has just been inserted to the collection
                this._Rows[this._PhysicalRows - 1].Target = row;
            }

            //rendy put this
            theTransaction.EndWriteRow(row);

            theTransaction.AddCommitAction(
                () =>
                {
                    row.WriteToDisk();
                    lock (_Lock)
                    { this.UpdateMetadata(); }
                }
                );

            theTransaction.AddRollbackAction(
                () =>
                {
                    row.State = RowStateFlag.TRASH;
                    row.WriteToDisk();
                    lock (_Lock)
                    {
                        _ActualRows--;
                        UpdateMetadata();
                    }
                }
                );

            theTransaction.EndWriteTable(this);
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
                { tempRows[i] = new WeakReference(null); }

                // Restore it back
                this._Rows = tempRows;
            }
        }

        /// <summary>
        /// Function to read the file on disk. Note: Assumption is made that
        /// pointer has already been moved to the "appropriate" position.
        /// </summary>
        /// <param name="count">int length of the Byte[].</param>
        /// <returns>Byte[] of the data from the file on disk.</returns>
        public static Byte[] ReadBytesFromDisk
            (FileStream myFileStream, int count)
        {
            Byte[] arrResult = new Byte[count];
            myFileStream.Read(arrResult, 0, count);
            return arrResult;
        }

        /// <summary>
        /// Function to read the Byte at the current position of the pointer.
        /// Note: Assumption is made that pointer has already been moved to 
        /// the "appropriate" position.
        /// </summary>
        /// <param name="myFileStream">FileStream for File I/O.</param>
        /// <returns>Byte that has been read.</returns>
        public static Byte ReadByteFromDisk(FileStream myFileStream)
        { return Table.ReadBytesFromDisk(myFileStream, 1)[0]; }

        /// <summary>
        /// Function to write Byte[] to file on disk. Note: Assumption is made
        /// that pointer has already been moved to the "appropriate" position.
        /// </summary>
        /// <param name="myFileStream">FileStream for File I/O.</param>
        /// <param name="arrData">Byte[] to be written.</param>
        /// <param name="count">int length of data to be written.</param>
        public static void WriteBytesToDisk(FileStream myFileStream, 
            Byte[] arrData, int count)
        { myFileStream.Write(arrData, 0, count); }

        /// <summary>
        /// Function to write a single Byte to file on disk. Note: Assumption
        /// is made that pointer has already been moved to the "appropriate"
        /// position.
        /// </summary>
        /// <param name="myFileStream">FileStream for File I/O.</param>
        /// <param name="data">Byte value to be written.</param>
        public static void WriteByteToDisk(FileStream myFileStream, Byte data)
        { myFileStream.WriteByte(data); }

        /// <summary>
        /// Function to calculate the length of Bytes per Row.
        /// </summary>
        /// <returns>int length of Bytes.</returns>
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