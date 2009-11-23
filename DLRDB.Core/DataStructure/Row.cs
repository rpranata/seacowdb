using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using DLRDB.Core.ConcurrencyUtils;

namespace DLRDB.Core.DataStructure
{
    /// <summary>
    /// State of the Row. 
    /// EMPTY(0) indicates an empty or potential Row.
    /// CLEAN(1) indicates CRUD can be performed to the Row. 
    /// DIRTY(2) indicates updates have been peformed on the Row,
    /// and that the changes have not been persisted to file.
    /// TRASH(3) indicates the Row has been flagged for deletion.
    /// ADDED(4) indicates a newly added Row that exists on Memory,
    /// but not file.
    /// </summary>
    public enum RowStateFlag : byte
    {
        EMPTY = 0,
        CLEAN = 1,
        DIRTY = 2,
        TRASH = 3,
        ADDED = 4
    }
    
    public class Row
    {
        // DANGEROUS FIELDS (mutable, requires locks)
        // ==========================================
        private int _RowNum;
        private RowStateFlag _State;
        private int _RowBytesLength = 0;
        private int _RowBytesStart = 0;

        private readonly Table _ParentTable;
        private readonly Field[] _Fields;
        private readonly FileStream _MyFileStream;
        private readonly Object _Lock = new Object();
        private readonly ReadWriteLock _RowLock;
        public const int ROWSTATE_LENGTH = 1;
        // ==========================================
        // END OF DANGEROUS FIELDS
       
        /// <summary>
        /// Constructor: Used for constructing the table to read data
        /// from the disk.
        /// </summary>
        /// <param name="parent">Associated parent Table reference.</param>
        /// <param name="rowNum">Associated Row index
        /// in Table collection.</param>
        /// <param name="myFileStream">Associated
        /// FileStream for File I/O.</param>
        public Row(Table parent, int rowNum, FileStream myFileStream)
            : this (parent,myFileStream)
        { this.RowNum = rowNum; }

        /// <summary>
        /// Constructor: for Row Object based on associated parent Table
        /// and associated Filestream for File I/O.
        /// </summary>
        /// <param name="parent">Associated parent Table reference.</param>
        /// <param name="myFileStream">Associated
        /// FileStream for File I/O.</param>
        public Row(Table parent, FileStream myFileStream)
        {
            this._ParentTable = parent;

            // Field Initialization
            // ========================
            this._Fields = new Field[this._ParentTable.Columns.Length];
            int index = 0;
            
            foreach (Column tempColumn in this._ParentTable.Columns)
            {
                if ((tempColumn.NativeType == typeof(System.Int32)))
                    { this._Fields[index] = new Int32Field(tempColumn); }
                else if ((tempColumn.NativeType == typeof(System.String)))
                    { this._Fields[index] = new StringField(tempColumn); }

                index++;
            }

            this._MyFileStream = myFileStream;
            this._RowLock = new ReadWriteLock();           
        }

        /// <summary>
        /// Accessor: returns the ReadWriteLock
        /// associated with this Row. Non mutable.
        /// </summary>
        public ReadWriteLock RowLock { get { return this._RowLock; } }

        /// <summary>
        /// Accessor: returns an array of Fields
        /// associated with this Row. Non mutable.
        /// </summary>
        public Field[] Fields { get { return this._Fields; } }

        /// <summary>
        /// Accessor/Mutator: gets/sets the State of the Row. 
        /// EMPTY(0) indicates an empty or potential Row.
        /// CLEAN(1) indicates CRUD can be performed to the Row. 
        /// DIRTY(2) indicates updates have been peformed on the Row,
        /// and that the changes have not been persisted to file.
        /// TRASH(3) indicates the Row has been flagged for deletion.
        /// ADDED(4) indicates a newly added Row that exists on Memory,
        /// but not file.
        /// </summary>
        public RowStateFlag State
        {
            get { return this._State; }
            set { lock (this._Lock){this._State = value;} }
        }

        /// <summary>
        /// Accessor/Mutator: gets/sets the row number which serves
        /// as the index of the Row in its parent Table Row collection.
        /// </summary>
        public int RowNum
        {
            get { return this._RowNum; }
            set 
            {
                lock (this._Lock)
                {
                    this._RowNum = value;

                    _RowBytesLength = 0;
                    _RowBytesLength += ROWSTATE_LENGTH;
                    foreach (Column tempColumn in this._ParentTable.Columns)
                    {
                        // Calculate the total length for this row
                        _RowBytesLength += tempColumn.Length;
                    }

                    // CalculateRowBytesStart
                    this._RowBytesStart += Table.METADATA_TOTAL_LENGTH;
                    this._RowBytesStart 
                        += (this.RowNum - 1) * this._RowBytesLength;
                }
            }
        }

        
        
        
        /// <summary>
        /// Overrides the ToString method to return all fields in the Row.
        /// </summary>
        /// <returns>The Row, plus respective Field names and values.</returns>
        public override String ToString()
        {
            String tempResult = "";

            lock (this._Lock)
            {
                for (int i = 0; i < this._Fields.Length; i++)
                {
                    tempResult += "[" + this._Fields[i].FieldColumn.Name
                        + "]" + " = " + this._Fields[i].ToString() + "\t";
                }
            }
            return tempResult;
        }

        /// <summary>
        /// Method to read this Row from the file.
        /// </summary>
        public void ReadFromDisk()
        {
            lock (this._Lock)
            {
                this._MyFileStream.Seek(this._RowBytesStart, SeekOrigin.Begin);

                this.State = (RowStateFlag)Table.ReadByteFromDisk
                    (this._MyFileStream);

                int index = 0;
                foreach (Column tempColumn in this._ParentTable.Columns)
                {
                    this._Fields[index].Value = Table.ReadBytesFromDisk
                        (this._MyFileStream, tempColumn.Length);
                    index++;
                }
            }        
        }

        /// <summary>
        /// Method to write this Row to file.
        /// </summary>
        public void WriteToDisk()
        {
            using (_RowLock.AcquireWriter())
            {
                lock (this._Lock)
                {
                    this._MyFileStream.Seek
                        (this._RowBytesStart, SeekOrigin.Begin);

                    // When we write data to the disk, we'll only
                    // use .CLEAN, .EMPTY, and .TRASH state flags
                    RowStateFlag tempDiskRowState = RowStateFlag.CLEAN;
                    if (this.State == RowStateFlag.TRASH)
                        { tempDiskRowState = RowStateFlag.TRASH; }

                    Table.WriteByteToDisk(this._MyFileStream,
                        (Byte)tempDiskRowState);
                    

                    int index = 0;
                    foreach (Column tempColumn in this._ParentTable.Columns)
                    {
                        Table.WriteBytesToDisk
                            (this._MyFileStream, this._Fields[index]
                            .Value, tempColumn.Length);
                        index++;
                    }

                    
                    
                    if (State != RowStateFlag.TRASH)
                        { State = RowStateFlag.CLEAN; }
                }
            }
        }

        /// <summary>
        /// Method to print the Row to Console cia the TextWriter parameter.
        /// </summary>
        /// <param name="output">TextWriter parameter
        /// to serve as the output stream.</param>
        public void OutputTo(TextWriter output)
        {
            output.WriteLine(DateTime.Now + " > " + this);
            output.Flush();
        }
    }
}