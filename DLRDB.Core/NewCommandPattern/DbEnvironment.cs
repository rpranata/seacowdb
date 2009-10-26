using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;
using System.IO;


namespace DLRDB.Core.NewCommandPattern
{
    public delegate Transaction TransactionCreater();

    public class DbEnvironment
    {
        public TransactionCreater CreateTransactionForIsolationLevel;
        public Transaction CurrentTransaction;
        public StreamWriter Writer;
    }
}
