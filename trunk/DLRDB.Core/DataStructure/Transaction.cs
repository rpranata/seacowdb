using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.ConcurrencyUtils;

namespace DLRDB.Core.DataStructure
{
    public delegate void DataUpdater();

    public abstract class Transaction
    {
        private List<DataUpdater> _RunOnCommit = new List<DataUpdater>();
        private List<DataUpdater> _RunOnRollback = new List<DataUpdater>();

        public void AddCommitAction(DataUpdater updater) { _RunOnCommit.Add(updater); }
        public void AddRollbackAction(DataUpdater updater) { _RunOnRollback.Add(updater); }

        // Because we will only have a single row being locked  for READ at a time
        private ILock _ILockForRead;

        private List<ILock> _ListILockForWrite;

        public Transaction()
        {
            this._ListILockForWrite = new List<ILock>();
        }

        public virtual void StartReadTable(Table theTable)
        {
            // do nothing
        }

        public virtual void EndReadTable(Table theTable)
        {
            // do nothing
        }

        public virtual void StartReadRow(Row theRow)
        {
            this._ILockForRead = theRow.RowFileLock.AcquireReader();
        }

        public virtual void EndReadRow(Row theRow)
        {
            this._ILockForRead.Dispose();
        }

        public virtual void StartWriteRow(Row theRow)
        {
            _ListILockForWrite.Add(theRow.RowFileLock.AcquireWriter());
        }

        public virtual void EndWriteRow(Row theRow)
        {
            // do nothing
        }

        public void Commit()
        {
            // Release all locks

            foreach (DataUpdater u in _RunOnCommit) u();

            foreach (ILock theLock in this._ListILockForWrite)
            {
                theLock.Dispose();
            }
        }

        public void Rollback()
        {
            foreach (DataUpdater u in _RunOnRollback) u();
            // Release all locks

            foreach (ILock theLock in this._ListILockForWrite)
            {
                theLock.Dispose();
            }
        }

        public virtual void StartWriteTable(Table theTable)
        {
            _ListILockForWrite.Add(theTable._TableLock.AcquireWriter());
        }

        public virtual void EndWriteTable(Table theTable)
        {
            // do nothing
        }
    }

    public class ReadCommittedTransaction : Transaction
    {
    }

    public class ReadUncommittedTransaction : Transaction
    {
        public override void StartReadRow(Row theRow)
        {
            //do nothing
        }

        public override void EndReadRow(Row theRow)
        {
            //do nothing
        }
    }
}