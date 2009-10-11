using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

using DLRDB.Core.ConcurrencyUtils;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.NetworkUtils
{
    class ActiveCommandListener : ActiveObject
    {
        private Table _Table;
        private Socket _Socket;
        private BinaryReader _Reader;
        private BinaryWriter _Writer;
        private String _Command;
        private NetworkStream _NetworkStream;

        public ActiveCommandListener(Socket newSocket, Table table)
            : base()
		{
            this._Table = table;
			this._Socket = newSocket;
			this._NetworkStream =  new NetworkStream(this._Socket);
            this._Reader = new BinaryReader(this._NetworkStream);
            this._Writer = new BinaryWriter(this._NetworkStream);
            
            this._Writer.Write("SERVER>>> Connection Successful\n");
		}

        public override void DoSomething()
        {
            while (this._Socket.Connected)
            {
                try
                {
                    this._Command = this._Reader.ReadString();
                    //Proccess command
                    this._Command = this._Command.Trim();
                    string commandType = this._Command.Substring(0, 6);
                    switch (commandType.ToLower())
                    {
                        case "insert":
                            {
                                String response = "";
                                Row myNewRow = null;
                                int numOfRowsToInsert = 1;
                                for (int i = 1; i <= numOfRowsToInsert; i++)
                                {
                                    myNewRow = this._Table.NewRow();
                                    myNewRow.Fields[1].Value = myNewRow.Fields[1].NativeToBytes("NewName" + i);
                                    myNewRow.Fields[2].Value = myNewRow.Fields[2].NativeToBytes(10 + (i % 10));

                                    this._Table.InsertRow(myNewRow);
                                    response += Environment.NewLine + "[1] row(s) inserted.";
                                }

                                this._Writer.Write(DateTime.Now + ">" + Environment.NewLine + response);

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

                                this._Writer.Write(DateTime.Now + ">" + Environment.NewLine + response);

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

                                this._Writer.Write(DateTime.Now + ">" + Environment.NewLine + response);

                                break;
                            }
                        case "select":
                            {
                                // this._Writer.Write("SELECT COMMAND");
                             
                                Row[] arrSelectRow = null;
                                arrSelectRow  = this._Table.SelectAll();

                                String response = "";
                                int numOfSelectedRows = 0;

                                foreach (Row tempRow in arrSelectRow)
                                {
                                    if (tempRow != null)
                                    {
                                        numOfSelectedRows++;
                                        response += Environment.NewLine + tempRow.ToString();
                                    }
                                }

                                response += Environment.NewLine + "[" + numOfSelectedRows + "] row(s) selected.";
                                this._Writer.Write(DateTime.Now + ">" + Environment.NewLine+ response);
                                break;
                            }
                       default:
                            {
                                this._Writer.Write("UNKNOWN COMMAND");
                                break;
                            }
                    }
                    //----------------
                }
                catch (Exception e)
                {
                    if (this._Socket.Connected)
                        this._Writer.Write(e.Message);
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
            base._Thread.Abort();
        }
    }
}
