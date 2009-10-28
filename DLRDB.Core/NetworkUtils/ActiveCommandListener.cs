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
using DLRDB.Core.NewCommandPattern;

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
            
            this._Writer.WriteLine("SERVER>>> Connection Successful");
            this._Writer.WriteLine("Welcome to the Seacow Database ");
            this._Writer.WriteLine("===============================");
            this._Writer.WriteLine("");
            this._Writer.Flush();
		}

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
                        this._Command = this._Command.Remove(this._Command.Length - 1);
                        this._Command = this._Command.Trim().ToLower().Replace("  ", " ");                     
                       
                        //QUESTION : ROllBACK?? i'm afraid the rollback will use the existing transaction..
                        //Checking the existence of SELECT,INSERT,UPDATE,DELETE
                        foreach (Command cmd in _Commands)
                        {
                            if (cmd.RunFor(this._Command))
                            {
                                commandKnown = true;
                                bool createTransaction = myEnv.CurrentTransaction == null;
                                if (createTransaction)
                                {
                                    myEnv.CurrentTransaction = myEnv.CreateTransactionForIsolationLevel();
                                }

                                cmd.Run(this._Command, _Table, myEnv);

                                if (((createTransaction) && (!this._Command.Equals("begin transaction"))) 
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
                            this._Writer.WriteLine("SERVER>>> UNKNOWN COMMAND");
                            this._Writer.Flush();
                            Trace.WriteLine(this._Socket.RemoteEndPoint + " send this command " + this._Command);
                        }

                        #region old
                        /*
                        String commandType = "";
                        String[] commands = this._Command.Split(' ');
                        if (commands.Length >= 0)
                            commandType = commands[0];
                         */

                        /*switch (commandType.ToLower())
                        {
                            case "begintransaction":
                                {
                                    this._TheTransaction = myEnv.CreateTransactionForIsolationLevel();
                                    break;
                                }

                            case "commit":
                                {
                                    this._TheTransaction.Commit();
                                    break;
                                }
                            case "rollback":
                                {
                                    this._TheTransaction.Rollback();
                                    break;
                                }

                            case "insertall":
                                {
                                    String response = "";
                                    Row myNewRow = null;
                                    int numOfRowsToInsert = 100;

                                    DateTime start = DateTime.Now;

                                    for (int i = 1; i <= numOfRowsToInsert; i++)
                                    {
                                        myNewRow = this._Table.NewRow();
                                        myNewRow.Fields[1].Value = myNewRow.Fields[1].NativeToBytes("NewName");
                                        myNewRow.Fields[2].Value = myNewRow.Fields[2].NativeToBytes(10 + (i % 10));

                                        this._Table.InsertRow(myNewRow, this._TheTransaction);

                                        this._Writer.Write(".");

                                        if (i % 80 - (3 + ("" + numOfRowsToInsert).Length)  == 0)
                                        {
                                            this._Writer.WriteLine(" (" + i + ")");
                                        }
                                        this._Writer.Flush();
                                        //response += Environment.NewLine + "[" + i + "] row(s) inserted.";
                                    }


                                    DateTime end = DateTime.Now;

                                    TimeSpan theSpan = end - start;

                                    this._Writer.WriteLine(DateTime.Now + ">" + "insert takes " + theSpan.TotalMilliseconds + " ms");

                                    this._Writer.WriteLine(DateTime.Now + ">" + Environment.NewLine + response);
                                    this._Writer.Flush();
                                    break;
                                }
                            case "update":
                                {
                                     //Supported format : 
                                     //-> UPDATE 1-2,dany,22
                                     //-> UPDATE *,hoon,22
                                     //-> UPDATE *,rendy,

                                    int updatedRows = 0;

                                    // Split by comma
                                    // ==================
                                    String[] arrSplitByComma = Regex.Split(commands[1], ",");

                                    // Allocate for (length - 1), because one of the element of arrSplitByComma will be the "range" 
                                    // - which is about to be ignored
                                    // Moreover, we add the number of element by 1, to allocate for the auto generated ID
                                    Object[] arrUpdatedValue = new Object[(arrSplitByComma.Length - 1) + 1];
                                    for (int i = 1; i < arrSplitByComma.Length; i++)
                                    {
                                        arrUpdatedValue[i] = arrSplitByComma[i];
                                    }

                                    if (arrSplitByComma[0] == "*")
                                    {
                                        updatedRows = this._Table.UpdateAll(this._TheTransaction, arrUpdatedValue);
                                        
                                    }
                                    else
                                    {
                                        String[] arrRange = Regex.Split(arrSplitByComma[0], "-");

                                        Int32 startIndex = Convert.ToInt32(arrRange[0]);
                                        Int32 endIndex = Convert.ToInt32(arrRange[1]);

                                        updatedRows = this._Table.Update(startIndex, endIndex,this._TheTransaction, arrUpdatedValue);
                                        
                                    }

                                    this._Writer.WriteLine(DateTime.Now + ">" + "Finish updating " + updatedRows + " row(s)");
                                    this._Writer.Flush();
                                    break;


                                    // ==

                                    String response = "";

                                    int updateLowRange = 1;
                                    int updateHighRange = 4;
                                    Object[] arrUpdatedValues = new Object[3];
                                    arrUpdatedValues[0] = null; // for ID
                                    arrUpdatedValues[1] = "UpdatedName";
                                    arrUpdatedValues[2] = 999;

                                    int numOfUpdatedRows = this._Table.Update(updateLowRange, updateHighRange, this._TheTransaction,arrUpdatedValues);

                                    response += Environment.NewLine + "[" + numOfUpdatedRows + "] row(s) updated.";

                                    this._Writer.WriteLine(DateTime.Now + ">" + Environment.NewLine + response);
                                    this._Writer.Flush();
                                    break;
                                }
                            case "delete":
                                {
                                    // Deleting the data
                                    // ===================================

                                    int deletedRows = 0;

                                    if (commands[1] == "*")
                                    {
                                        deletedRows = this._Table.DeleteAll(this._TheTransaction, this._Writer);
                                    }
                                    else
                                    {
                                        String[] arrSplitExpression = Regex.Split(commands[1], "-");

                                        Int32 startIndex = Convert.ToInt32(arrSplitExpression[0]);
                                        Int32 endIndex = Convert.ToInt32(arrSplitExpression[1]);

                                        deletedRows = this._Table.Delete(startIndex, endIndex, this._TheTransaction, this._Writer);
                                    }

                                    this._Writer.WriteLine(DateTime.Now + ">" + "Finish deleting " + deletedRows + " row(s)");
                                    this._Writer.Flush();
                                    break;

                                    //String response = "";

                                    //int deleteLowRange = 2;
                                    //int deleteHighRange = 3;

                                    //int numOfDeletedRows = this._Table.Delete(deleteLowRange, deleteHighRange);

                                    //response += Environment.NewLine + "[" + numOfDeletedRows + "] row(s) deleted";

                                    //this._Writer.WriteLine(DateTime.Now + ">" + Environment.NewLine + response);
                                    //this._Writer.Flush();
                                    //break;
                                }
                            case "select":
                                {
                                    // this._Writer.WriteLine("SELECT COMMAND");
                                    if (commands[1] == "*")
                                    {
                                        this._Table.SelectAll(this._Writer, this._TheTransaction);
                                    }
                                    else
                                    {
                                        String[] arrSplitExpression = Regex.Split(commands[1], "-");

                                        Int32 startIndex = Convert.ToInt32(arrSplitExpression[0]);
                                        Int32 endIndex = Convert.ToInt32(arrSplitExpression[1]);

                                        this._Table.Select(startIndex, endIndex, this._TheTransaction, this._Writer);
                                    }

                                    this._Writer.WriteLine(DateTime.Now + ">" + "Finish select");
                                    this._Writer.Flush();
                                    break;

                                    //this._Table.FetchRows(9999990,9999992,this._Writer);
                                    //String response = "";
                                    //int numOfSelectedRows = 0;
                                    //foreach (Row tempRow in arrSelectRow)
                                    //{
                                    //    if (tempRow != null)
                                    //    {
                                    //        numOfSelectedRows++;
                                    //        response += Environment.NewLine + tempRow.ToString();
                                    //    }
                                    //}

                                    // response += Environment.NewLine + "[" + numOfSelectedRows + "] row(s) selected.";

                                }
                            case "exit":
                                {
                                    this._Writer.WriteLine("Thanks for using seacow");
                                    this._Writer.WriteLine("Bye...");
                                    this._Writer.Flush();
                                    this._Socket.Disconnect(true);
                                    break;
                                }
                            default:
                                {
                                    this._Writer.WriteLine("UNKNOWN COMMAND");
                                    this._Writer.Flush();
                                    Trace.WriteLine(this._Socket.RemoteEndPoint + " Send this command " + this._Command);
                                    break;
                                }
                        }*/
                        #endregion
                    }
                    else
                    {
                        this._Writer.WriteLine("INVALID COMMAND - Please put the semicolon to run a command");
                        this._Writer.WriteLine("");
                        this._Writer.Flush();
                    }
                    //----------------
                }
                catch (Exception e)
                {
                    if (this._Socket.Connected)
                    {
                        this._Writer.WriteLine("======================================");
                        this._Writer.Write("ERROR : ");
                        this._Writer.WriteLine(e.Message);
                        this._Writer.Flush();
                    }
                    Console.WriteLine("Error has occured when trying to listen" +
                        "\nto the command sent by client\n" + e.Message);
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
