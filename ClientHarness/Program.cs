using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DLRDB.Core.NetworkUtils;

namespace ClientHarness
{
    class Program
    {
        private static Client _Client;

        static void Main(string[] args)
        {
            Program _Program = new Program();

            //_Client = new Client("192.168.0.2");
            _Client = new Client("127.0.0.1");
            Console.WriteLine(_Client.Reader.ReadString());
            String input = GetInput();

            while(!(input.Equals("exit")))
            {
                try
                {
                    _Client.Writer.Write(input);
                    Console.WriteLine(_Client.Reader.ReadString());
                    input = GetInput();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\r\n ERROR Occured \r\n " + ex.Message);
                }
            }

            _Client.Destruct();
            Environment.Exit(0);
        }

        private static String GetInput()
        {
            Console.Write("SEACOW > ");
            String input = Console.ReadLine();
            while(!(input.Contains(";")))
            {
                Console.Write("> ");
                input = input + Console.ReadLine();
            }
            return input.Trim().ToLower().Remove(input.Length-1);
        }
    }
}
