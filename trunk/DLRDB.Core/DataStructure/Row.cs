using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using DLRDB.Core.ConcurrencyUtils;

namespace DLRDB.Core.DataStructure
{
    /// <summary>
    /// State of the Row. 
    /// EMPTY(0) indicates an empty or potential Row.
    /// CLEAN(1) indicates CRUD can be performed to the Row. 
    /// DIRTY(2) indicates updates have been peformed on the Row, and
    /// that the changes have not been persisted to file.
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
        private int _RowNum;
        private Table _ParentTable;
        private Field[] _Fields;
        private RowStateFlag _State;
        
        private int _RowBytesLength = 0;
        private int _RowBytesStart = 0;

        private FileStream _MyFileStream;
        public const int ROWSTATE_LENGTH = 1;  
        private readonly ReadWriteLock _RowFileLock;
        private readonly ReadWriteLock _RowMemoryLock;

        /// <summary>
        /// Used for constructing the table to read data from the disk
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="rowNum"></param>
        /// <param name="myFileStream"></param>
        public Row(Table parent, int rowNum, FileStream myFileStream) : this (parent,myFileStream)
        {
            this.RowNum = rowNum;
        }

        public Row(Table parent, FileStream myFileStream)
        {
            this.ParentTable = parent;
            this._MyFileStream = myFileStream;
            this._RowFileLock = new ReadWriteLock();
            this._RowMemoryLock = new ReadWriteLock();
            //TODO: Create empty row and return            
        }

        private void constructFields()
        {
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

        }

        /// <summary>
        /// This will be called ONLY when we set the RowNum
        /// </summary>
        private void CalculateRowBytesStart()
        {
            _RowBytesLength = 0;
            _RowBytesLength += ROWSTATE_LENGTH;
            foreach (Column tempColumn in this._ParentTable.Columns)
            {
                // Calculate the total length for this row
                _RowBytesLength += tempColumn.Length;
            }

            this._RowBytesStart += Table.METADATA_TOTAL_LENGTH;
            this._RowBytesStart += (this.RowNum - 1) * this._RowBytesLength;
        }
               
        /// <summary>
        /// Get/Set Parent Table, used for backward referencing.
        /// </summary>
        public Table ParentTable
        {
            get
            { return this._ParentTable; }
            set
            { 
                this._ParentTable = value;
                constructFields();
            }
        }

        /// <summary>
        /// Gets Field based on Field Name, returns relevant Field Object.
        /// </summary>
        /// <param name="fieldName">Field Name to seek by.</param>
        /// <returns>Relevant Field based on search criteria.</returns>
        /*public Field GetField(String fieldName)
        {
            Field tempField = null;
            this._DictFields.TryGetValue(fieldName, out tempField);
            return tempField;
        }*/

        //public Field GetField(int fieldIndex)
        //{
        //    return this._ListFields[fieldIndex];
        //}

        /// <summary>
        /// Gets/Sets the State of the Row. DEFAULT(0) indicates CRUD can be performed. DELETED indicates Row is flagged for deletion, therefore becomes unaccessible.
        /// </summary>
        public RowStateFlag State
        {
            get 
            {
                try
                {
                    //this._RowFileLock.ReaderLock();
                    return this._State;
                }
                finally
                {
                    //this._RowFileLock.Release();
                }
            }
            set 
            {
                try
                {
                    //this._RowFileLock.WriterLock();
                    if (value == RowStateFlag.EMPTY)
                    {
                        Console.WriteLine("Rownum " + this._RowNum + " is Set into EMPTY");
                    }
                    this._State = value;
                }
                finally
                {
                    //this._RowFileLock.WriterLock();
                }
            }
        }

        public int RowNum
        {
            get { return this._RowNum; }
            set 
            {
                this._RowNum = value;
                CalculateRowBytesStart();
            }
        }

        public FileStream MyFileStream
        {
            get { return this._MyFileStream; }
            set { this._MyFileStream = value; }
        }

        /// <summary>
        /// Overrides the ToString method to return all fields in the Row.
        /// </summary>
        /// <returns>The Row, plus respective Field names and values.</returns>
        public override String ToString()
        {
            String tempResult = "";

            for (int i = 0; i < this._Fields.Length; i++)
            {
                tempResult += "[" + this._Fields[i].FieldColumn.Name 
                    + "]" + " = " + this._Fields[i].ToString() + "\t";  
            }

            return tempResult;
        }

        public void ReadFromDisk()
        {
            //this._RowFileLock.ReaderLock();

            this._MyFileStream.Seek(this._RowBytesStart, SeekOrigin.Begin);
            
            this.State = (RowStateFlag) Table.ReadByteFromDisk
                (this._MyFileStream);
            
            int index = 0;
            foreach (Column tempColumn in this._ParentTable.Columns)
            {
                this._Fields[index].Value = Table.ReadBytesFromDisk
                    (this._MyFileStream,tempColumn.Length);
                index++;
            }

            //this._RowFileLock.Release();
          
        }

        public void WriteToDisk()
        {
            //this._RowFileLock.WriterLock();

            this._MyFileStream.Seek(this._RowBytesStart, SeekOrigin.Begin);

            // when we put data into the disk, we'll only use .CLEAN, .EMPTY, and .TRASH flag
            RowStateFlag tempDiskRowState = tempDiskRowState = RowStateFlag.CLEAN;
            if (this.State == RowStateFlag.TRASH)
            { tempDiskRowState  = RowStateFlag.TRASH; }

            Table.WriteByteToDisk(this._MyFileStream,(Byte) tempDiskRowState);

            int index = 0;
            foreach (Column tempColumn in this._ParentTable.Columns)
            {
                Table.WriteBytesToDisk(this._MyFileStream, this._Fields[index].Value, tempColumn.Length);
                index++;
            }
            //this._RowFileLock.ReleaseWriterLock();

        }

        public Field[] Fields
        {
            get
            {
                try
                {
                    //this._RowFileLock.ReaderLock();
                    return this._Fields;
                }
                finally
                {
                    //this._RowFileLock.Release();
                }
            }
            set
            {
                try
                {
                    //this._RowFileLock.WriterLock();
                    this._Fields = value;
                }
                finally
                {
                    //this._RowFileLock.ReleaseWriterLock();
                }
            }
        }

        public ReadWriteLock RowMemoryLock
        { get { return this._RowMemoryLock; } }
    }
}
