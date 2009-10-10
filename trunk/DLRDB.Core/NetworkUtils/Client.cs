using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace DLRDB.Core.NetworkUtils
{
    public class Client
    {
        private NetworkStream _Stream;
        private BinaryWriter _Writer;
        private BinaryReader _Reader;
        private TcpClient _TCPClient;
        private String _IPAddress;

        private const int PORT = 6806;

        public BinaryReader Reader
        {
            get { return this._Reader; }
        }

        public BinaryWriter Writer
        {
            get { return this._Writer; }
        }

        public Client(String serverIP)
        {
            this._IPAddress = serverIP;
            this._TCPClient = new TcpClient();

            //Connect to the server
            this._TCPClient.Connect(this._IPAddress, Client.PORT);
            this._Stream = this._TCPClient.GetStream();

            //Initialize the reader and writer
            _Writer = new BinaryWriter(this._Stream);
            _Reader = new BinaryReader(this._Stream);
        }

        public void Destruct()
        {
            this._Writer.Close();
            this._Reader.Close();
            this._Stream.Close();
            this._TCPClient.Close();
        }
    }
}
