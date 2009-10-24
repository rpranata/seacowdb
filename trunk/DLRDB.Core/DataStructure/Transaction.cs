using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.ConcurrencyUtils;

namespace DLRDB.Core.DataStructure
{
    public class Transaction
    {
        // Because we will only have a single row being locked  for READ at a time
        private ILock _ILockForRead;

        private List<ILock> _ListILockForWrite;

        public Transaction()
        {
            this._ListILockForWrite = new List<ILock>();
        }

        public void StartReadTable(Table theTable)
        {
            // do nothing
        }

        public void EndReadTable(Table theTable)
        {
            // do nothing
        }

        public void StartReadRow(Row theRow)
        {
            this._ILockForRead = theRow.RowFileLock.AcquireReader();
        }

        public void EndReadRow(Row theRow)
        {
            this._ILockForRead.Dispose();
        }

        public void StartWriteRow(Row theRow)
        {
            _ListILockForWrite.Add(theRow.RowFileLock.AcquireWriter());
        }

        public void EndWriteRow(Row theRow)
        {
            // do nothing
        }

        public void Commit()
        {
            // Release all locks

            foreach (ILock theLock in this._ListILockForWrite)
            {
                theLock.Dispose();
            }
        }

        public void Rollback()
        {
            // Release all locks

            foreach (ILock theLock in this._ListILockForWrite)
            {
                theLock.Dispose();
            }
        }

        public void StartRowInsertOnTable(Table theTable)
        {
            // do nothing
        }

        public void EndRowInsertOnTable(Table theTable)
        {
            // do nothing
        }




    }
}