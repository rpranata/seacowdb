using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

using DLRDB.Core.ConcurrencyUtils;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.NetworkUtils
{
    public class Server : ActiveObject
    {
        private TcpListener _Listener;
        private readonly Queue<ActiveCommandListener> _Client;
        private Table _Table;

        public Server() : base()
        {
            //TODO: CONSTRUCT THIS IN THE BETTER WAY!!
            /*IPHostEntry myIP;
            myIP = Dns.GetHostEntry(Dns.GetHostName());
            Console.WriteLine(myIP.AddressList[0]);
            myListener = new TcpListener(new System.Net.IPEndPoint(myIP.AddressList[0],6806));*/
            byte[] myByte = new byte[4];
            myByte[0] = 127;
            myByte[1] = 0;
            myByte[2] = 0;
            myByte[3] = 1;
            IPEndPoint ip = new IPEndPoint(new IPAddress(myByte), 6806);
            _Listener = new TcpListener(ip);
            
            this._Table = new Table("Contact", "TestDB/test.dlr");

            //-END TODO---------------------------------

            _Client = new Queue<ActiveCommandListener>();
        }

        public override void DoSomething()
        {
            this. _Listener.Start();
            Socket socket = this._Listener.AcceptSocket();
            ActiveCommandListener client = new ActiveCommandListener(socket,this._Table);
            this._Client.Enqueue(client);
            client.Start();
        }



        
    }
}
