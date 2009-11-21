using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace SeacowTestHarness
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //if(args[0] != null)
            //{
                TcpClient client = new TcpClient();//"136.186.197.41", 6806);
                client.Connect("localhost", 6806);//"136.186.197.41", 6806);//
                //NetworkStream streamA = client.GetStream();
                NetworkStream myStream = client.GetStream();
                //myStream.Write(


                StreamWriter writer = new StreamWriter(myStream);
                StreamReader reader = new StreamReader(myStream);

                /*Console.WriteLine(reader.ReadLine());
                writer.Write(@"insert dany, 20;");
                writer.Flush();*/
                Console.WriteLine(reader.ReadLine());
                Console.WriteLine(reader.ReadLine());
                Console.WriteLine(reader.ReadLine());

                while (true)
                { 
                    writer.WriteLine(Console.ReadLine());
                    writer.Flush();
                    
                    Console.WriteLine(reader.ReadLine().ToString());
                    if (Console.ReadLine() == "exit")
                    { break; }
                }

                //Socket socketA = new Socket(client.GetStream().);
                //socketA.Connect();
                // socketA.Send(ASCIIEncoding.ASCII.GetBytes(@"insert dany,20;"));
                //Console.WriteLine("Socket A: " + ASCIIEncoding.ASCII.ToString(socketA.Receive()));

                /*Socket socketB = new Socket(client);
                socketB.Connect();

                Socket socketC = new Socket(client);
                socketC.Connect();

                Socket socketD = new Socket(client);
                socketD.Connect();*/

            //}
        }
    }
}
