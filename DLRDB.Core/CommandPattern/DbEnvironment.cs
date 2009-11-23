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
        
        /// <summary>
        /// Constructor: Establishes the output stream for system feedback.
        /// </summary>
        /// <param name="writer">The current associated output stream.</param>
        public DbEnvironment(StreamWriter writer) { this._Writer = writer; }

        /// <summary>
        /// Accessor: returns the output stream. Non mutable.
        /// </summary>
        public StreamWriter Writer { get { return this._Writer; } }
    }
}