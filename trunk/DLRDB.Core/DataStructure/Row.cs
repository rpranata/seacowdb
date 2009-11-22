﻿using System;
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
        // DANGEROUS FIELDS
        // ==================
        private int _RowNum;
        private RowStateFlag _State;
        private int _RowBytesLength = 0;
        private int _RowBytesStart = 0;

        private readonly Table _ParentTable;
        private readonly Field[] _Fields;
        private readonly FileStream _MyFileStream;
        private readonly Object _Lock = new Object();
        private readonly ReadWriteLock _RowFileLock;
        public const int ROWSTATE_LENGTH = 1;  
       
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
            this._ParentTable = parent;

            // Construct the fields
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
            this._RowFileLock = new ReadWriteLock();
            //TODO: Create empty row and return            
        }

        public ReadWriteLock RowFileLock { get { return this._RowFileLock; } }
        public Field[] Fields { get { return this._Fields; } }

        /// <summary>
        /// Gets/Sets the State of the Row. 
        /// DEFAULT(0) indicates CRUD can be performed. 
        /// DELETED indicates Row is flagged for deletion, 
        /// therefore becomes unaccessible.
        /// </summary>
        public RowStateFlag State
        {
            get
            { return this._State; }
            set 
            {
                lock (this._Lock){this._State = value;}
            }
        }

        public int RowNum
        {
            get {return this._RowNum;}
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
                    // ========================
                    this._RowBytesStart += Table.METADATA_TOTAL_LENGTH;
                    this._RowBytesStart += (this.RowNum - 1) * this._RowBytesLength;
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

        public void WriteToDisk()
        {
            using (_RowFileLock.AcquireWriter())
            {
                lock (this._Lock)
                {
                    this._MyFileStream.Seek(this._RowBytesStart, SeekOrigin.Begin);

                    // when we put data into the disk, we'll only use .CLEAN, .EMPTY, and .TRASH flag
                    RowStateFlag tempDiskRowState = RowStateFlag.CLEAN;
                    if (this.State == RowStateFlag.TRASH)
                    { tempDiskRowState = RowStateFlag.TRASH; }

                    Table.WriteByteToDisk(this._MyFileStream, (Byte)tempDiskRowState);
                    

                    int index = 0;
                    foreach (Column tempColumn in this._ParentTable.Columns)
                    {
                        Table.WriteBytesToDisk(this._MyFileStream, this._Fields[index].Value, tempColumn.Length);
                        index++;
                    }

                    if (State != RowStateFlag.TRASH)
                    {
                        State = RowStateFlag.CLEAN;
                    }
                }
            }
        }

        public void OutputTo(TextWriter output)
        {
            //TODO: read locks etc.
            output.WriteLine(DateTime.Now + " > " + this);
            output.Flush();
        }
    }
}