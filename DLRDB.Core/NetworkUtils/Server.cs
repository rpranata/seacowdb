using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Text.RegularExpressions;

using DLRDB.Core.ConcurrencyUtils;
using DLRDB.Core.DataStructure;

namespace DLRDB.Core.NetworkUtils
{
    public class Server : ActiveObject
    {
        private readonly TcpListener _Listener;
        private readonly List<ActiveCommandListener> _Client;
        private readonly int _Port;
        private Table _Table;
       
        public Server(int port) : base()
        {
            //Init
            this._Port = port;
            this._Client = new List<ActiveCommandListener>();
            this._Table = new Table("Contact", "TestDB/test.dlr");

            //Networking - Setting up a server
            IPHostEntry IPs;
            IPs = Dns.GetHostEntry(Dns.GetHostName());
            String IPAddr = getIPV4(IPs);
            this._Listener = new TcpListener
                (IPAddress.Parse(IPAddr), this._Port);

            this._Listener.Start();
        }

        public override void DoWork()
        {
            Socket socket = this._Listener.AcceptSocket();
            ActiveCommandListener client = new ActiveCommandListener
                (socket, this._Table);
            client.Start();
            this._Client.Add(client);
            Trace.WriteLine("1 Client  connected");
        }

        private String getIPV4(IPHostEntry IPs)
        {
            String IP = "";
            Regex regex = new Regex(@"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9]"
                + @"[0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b");
            int last = IPs.AddressList.Length - 1;
            while (IP == "")
            {
                if ((regex.IsMatch(IPs.AddressList[last].ToString())) 
                    && (!IPs.AddressList[last].IsIPv6LinkLocal))
                    IP = IPs.AddressList[last].ToString();
                last--;
            }
            return IP;
        }
    }
}