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

        public void AddCommitAction(DataUpdater updater) 
            { _RunOnCommit.Add(updater); }
        public void AddRollbackAction(DataUpdater updater) 
            { _RunOnRollback.Add(updater); }

        // Because we will only have a single row being locked  
        // for READ at a time
        protected List<ILock> _ListILockForRead;
        protected List<ILock> _ListILockForWrite;

        public Transaction()
        {
            this._ListILockForRead = new List<ILock>();
            this._ListILockForWrite = new List<ILock>();
        }

        public virtual void StartReadTable(Table theTable) {/*do nothing*/}
        public virtual void EndReadTable(Table theTable) {/*do nothing*/}

        public virtual void StartReadRow(Row theRow)
            { this._ListILockForRead.Add(theRow.RowLock.AcquireReader()); }

        /// <summary>
        /// This will only be executed under read committed isolation level
        /// thus, in theory, there will only be one read lock 
        /// stored in the list
        /// </summary>
        /// <param name="theRow"></param>
        public virtual void EndReadRow(Row theRow)
            { this._ListILockForRead.Last().Dispose(); }

        public virtual void StartWriteRow(Row theRow)
            { _ListILockForWrite.Add(theRow.RowLock.AcquireWriter()); }

        public virtual void EndWriteRow(Row theRow) {/*do nothing*/}

        public virtual void Commit()
        {
            foreach (DataUpdater u in _RunOnCommit) u();
            // Release all locks
            foreach (ILock theLock in this._ListILockForWrite)
                { theLock.Dispose(); }
        }

        public virtual void Rollback()
        {
            foreach (DataUpdater u in _RunOnRollback) u();
            // Release all locks
            foreach (ILock theLock in this._ListILockForWrite)
                { theLock.Dispose(); }
        }

        public virtual void StartWriteTable(Table theTable)
            { _ListILockForWrite.Add(theTable._TableLock.AcquireWriter()); }

        public virtual void EndWriteTable(Table theTable) {/*do nothing*/}
    }

    
    
    
    public class ReadCommittedTransaction : Transaction
    {
        public override String ToString()
            { return "Read Committed"; }
    }

    public class ReadUncommittedTransaction : Transaction
    {
        public override void StartReadRow(Row theRow) {/*do nothing*/}
        public override void EndReadRow(Row theRow) {/*do nothing*/}

        public override String ToString()
            { return "Read Uncommitted"; }
    }
    
    public class RepeatableReadTransaction : ReadCommittedTransaction
    {
        public override void EndReadRow(Row theRow) {/*do nothing*/}

        public override void Commit()
        {
            base.Commit();

            // Release all read locks, because repeatable read may have
            // more than 1 read lock at a time
            foreach (ILock theLock in base._ListILockForRead)
                { theLock.Dispose(); }
        }

        public override void Rollback()
        {
            base.Rollback();

            // Release all read locks, because repeatable read may have
            // more than 1 read lock at a time
            foreach (ILock theLock in base._ListILockForRead)
                { theLock.Dispose(); }
        }

        public override String ToString()
            { return "Repeatable Read"; }
    }

    public class SerializableTransaction : RepeatableReadTransaction
    {           
        public override void StartReadTable(Table theTable)
        {
            base._ListILockForRead.Add(theTable._TableLock.AcquireReader());
        }

        public override String ToString()
            { return "Serializable"; }
    }
}