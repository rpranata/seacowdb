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

namespace DLRDB.Core.NetworkUtils
{
    class ActiveCommandListener : ActiveObject
    {
        private readonly Table _Table;
        private readonly Socket _Socket;
        private readonly StreamReader _Reader;
        private readonly StreamWriter _Writer;
        private readonly NetworkStream _NetworkStream;
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

        public override void DoSomething()
        {
            while (this._Socket.Connected)
            {
                try
                {
                    this._Command = this._Reader.ReadLine();
                    //Proccess command
                    this._Command = this._Command.Trim();

                    // Remove the semicolon
                    this._Command = this._Command.Remove(this._Command.Length - 1);
                    
                    // Checking the existence of SELECT,INSERT,UPDATE,DELETE
                    String commandType = "";
                    String[] commands = this._Command.Split(' ');
                    if (commands.Length >= 0)
                        commandType = commands[0];

                    switch (commandType.ToLower())
                    {
                        case "insert":
                            {
                                String response = "";
                                Row myNewRow = null;
                                int numOfRowsToInsert = 10000000;

                                DateTime start = DateTime.Now;

                                for (int i = 1; i <= numOfRowsToInsert; i++)
                                {
                                    myNewRow = this._Table.NewRow();
                                    myNewRow.Fields[1].Value = myNewRow.Fields[1].NativeToBytes("NewName");
                                    myNewRow.Fields[2].Value = myNewRow.Fields[2].NativeToBytes(10 + (i % 10));

                                    this._Table.InsertRow(myNewRow);
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
                                String response = "";

                                int updateLowRange = 1;
                                int updateHighRange = 4;
                                Object[] arrUpdatedValues = new Object[3];
                                arrUpdatedValues[0] = null; // for ID
                                arrUpdatedValues[1] = "UpdatedName";
                                arrUpdatedValues[2] = 999;

                                int numOfUpdatedRows = this._Table.Update(updateLowRange, updateHighRange, arrUpdatedValues);

                                response += Environment.NewLine + "[" + numOfUpdatedRows + "] row(s) updated.";

                                this._Writer.WriteLine(DateTime.Now + ">" + Environment.NewLine + response);
                                this._Writer.Flush();
                                break;
                            }
                        case "delete":
                            {
                                // Deleting the data
                                // ===================================

                                String response = "";
                                
                                int deleteLowRange = 2;
                                int deleteHighRange = 3;

                                int numOfDeletedRows = this._Table.Delete(deleteLowRange, deleteHighRange);

                                response += Environment.NewLine + "[" + numOfDeletedRows + "] row(s) deleted";

                                this._Writer.WriteLine(DateTime.Now + ">" + Environment.NewLine + response);
                                this._Writer.Flush();
                                break;
                            }
                        case "select":
                            {

                                // this._Writer.WriteLine("SELECT COMMAND");

                                if (commands[1] == "*")
                                {
                                    this._Table.SelectAll(this._Writer);
                                }
                                else
                                {
                                    String[] arrSplitExpression = Regex.Split(commands[1], "-");

                                    Int32 startIndex = Convert.ToInt32(arrSplitExpression[0]) - 1;
                                    Int32 endIndex = Convert.ToInt32(arrSplitExpression[1]) - 1;

                                    this._Table.Select(startIndex, endIndex, this._Writer);
                                }

                                this._Writer.WriteLine(DateTime.Now + ">" + "Finish select");
                                this._Writer.Flush();
                                break;

                                //this._Table.Select(9999990,9999992,this._Writer);
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

        private void parseSelect (String command)
        {
          


            
        }


    }
}
