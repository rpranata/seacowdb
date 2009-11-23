using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Diagnostics;

using DLRDB.Core.ConcurrencyUtils;
using DLRDB.Core.DataStructure;
using System.Text.RegularExpressions;
using DLRDB.Core.CommandPattern;
using DLRDB.Core.Exceptions;

namespace DLRDB.Core.NetworkUtils
{
    class ActiveCommandListener : ActiveObject
    {
        private static readonly TransactionCreater _DefaultIsolation;
        private static readonly List<Command> _Commands = new List<Command>();

        static ActiveCommandListener()
        {
            SetIsolationLevelCommand cmd = new SetIsolationLevelCommand();
            _Commands.Add(cmd);
            _DefaultIsolation = cmd.CreateReadCommitted;
            //adding supported command
            _Commands.Add(new BeginTransactionCommand());
            _Commands.Add(new CommitCommand());
            _Commands.Add(new RollbackCommand());
            _Commands.Add(new SelectCommand());
            _Commands.Add(new InsertAllCommand());
            _Commands.Add(new InsertCommand());
            _Commands.Add(new UpdateCommand());
            _Commands.Add(new DeleteCommand());
        }

        private readonly Table _Table;
        private readonly Socket _Socket;
        private readonly StreamReader _Reader;
        private readonly StreamWriter _Writer;
        private readonly NetworkStream _NetworkStream;
        //private Transaction _TheTransaction;
        private String _Command;
       
        public ActiveCommandListener(Socket newSocket, Table table)
            : base()
		{
            this._Table = table;
			this._Socket = newSocket;
			this._NetworkStream =  new NetworkStream(this._Socket);
            this._Reader = new StreamReader(this._NetworkStream);
            this._Writer = new StreamWriter(this._NetworkStream);
            
            this._Writer.WriteLine("SERVER >> Connection Successful");
            this._Writer.WriteLine("Welcome to the Seacow Database ");
            this._Writer.WriteLine("===============================");
            this._Writer.WriteLine("");
            this._Writer.Flush();
		}

        public override void Run()
            { DoWork(); }

        public override void DoWork()
        {
            DbEnvironment myEnv = new DbEnvironment(this._Writer);
            myEnv.CreateTransactionForIsolationLevel = _DefaultIsolation;
            bool commandKnown;
   
            
            
            
            while (this._Socket.Connected)
            {
                try
                {
                    this._Command = this._Reader.ReadLine();
                    this._Command = this._Command.Trim();
                    commandKnown = false;
                    //Proccess command
                    if (this._Command.EndsWith(";"))
                    {
                        // Remove the semicolon and clean up the command string
                        this._Command = this._Command.Remove
                            (this._Command.Length - 1);
                        this._Command = this._Command.Trim()
                            .ToLower().Replace("  ", " ");

                        #region Questions
                            //QUESTION : ROllBACK?? i'm afraid the rollback will use the existing transaction..
                        #endregion
                        //Checking the existence of SELECT,INSERT,UPDATE,DELETE
                        foreach (Command cmd in _Commands)
                        {
                            if (cmd.RunFor(this._Command))
                            {
                                commandKnown = true;
                                bool createTransaction = myEnv
                                    .CurrentTransaction == null;
                                if (createTransaction)
                                {
                                    myEnv.CurrentTransaction = myEnv
                                        .CreateTransactionForIsolationLevel();
                                }

                                try
                                    { cmd.Run(this._Command, _Table, myEnv); }
                                catch (DLRDBException ex)
                                {
                                    this._Writer.WriteLine(ex.Message);
                                    this._Writer.Flush();
                                }

                                if (((createTransaction) 
                                    && (!this._Command.Equals
                                    ("begin transaction")))
                                    || this._Command.Equals("commit"))
                                {
                                    myEnv.CurrentTransaction.Commit();
                                    myEnv.CurrentTransaction = null;
                                }
                                break;
                            }
                        }

                        if (!commandKnown)
                        {
                            this._Writer.WriteLine
                                ("SERVER >> UNKNOWN COMMAND");
                            this._Writer.Flush();
                            Trace.WriteLine(this._Socket.RemoteEndPoint 
                                + " send this command " + this._Command);
                        }
                    }
                    else
                    {
                        this._Writer.WriteLine("INVALID COMMAND - Please place"
                            +" a semicolon delimiter to run a command");
                        this._Writer.WriteLine("");
                        this._Writer.Flush();
                    }
                    //----------------
                }
                
                
                
                
                
                catch (Exception e)
                {
                    if (this._Socket.Connected)
                    {
                        this._Writer.WriteLine
                            ("======================================");
                        this._Writer.Write("Error : ");
                        this._Writer.WriteLine(e.Message);
                        this._Writer.Flush();
                    }
                    Console.WriteLine("Error has occured when trying to listen"
                        + "\nto the command sent by client\n" + e.Message);
                }
            }

            //delete all the resources
            this._Reader.Close();
            this._Writer.Close();
            this._NetworkStream.Close();
            this._Socket.Close();

            //kill the thread
            base._Thread.Interrupt();
        }
    }
}