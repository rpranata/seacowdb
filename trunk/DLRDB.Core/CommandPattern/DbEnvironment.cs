using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;
using System.IO;


namespace DLRDB.Core.CommandPattern
{
    public delegate Transaction TransactionCreater();

    public class DbEnvironment
    {
        public TransactionCreater CreateTransactionForIsolationLevel;
        public Transaction CurrentTransaction;
        private readonly StreamWriter _Writer;

        public DbEnvironment(StreamWriter writer)
        {
            this._Writer = writer;
        }

        public StreamWriter Writer
        {
            //TODO : need any lock?
            get { return this._Writer; }
        }
    }
}
