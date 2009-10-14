using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DLRDB.Core.NetworkUtils;
using System.IO;

namespace ServerHarness
{
    class Program
    {
        private static Server _Server;

        static void Main(string[] args)
        {
            generateSampleDatabase();

            _Server = new Server();
            Console.WriteLine("Seacow Server Database Started");
            Console.WriteLine("Please keep this window open");
            _Server.Start();
        }


        #region Database Generator

        private const int METADATA_TRADEMARK_LENGTH = 6;
        private const int METADATA_MAJOR_VERSION_LENGTH = 1;
        private const int METADATA_MINOR_VERSION_LENGTH = 1;
        private const int METADATA_DETAIL_VERSION_LENGTH = 1;
        private const int METADATA_NUM_ROWS_LENGTH = 4;
        private const int METADATA_NUM_USED_PHYSICAL_ROWS_LENGTH = 4;
        private const int METADATA_NUM_AVAILABLE_PHYSICAL_ROWS_LENGTH = 4;
        private const int METADATA_NEXT_PK_LENGTH = 4;

        public const int METADATA_TOTAL_LENGTH =
            METADATA_TRADEMARK_LENGTH + METADATA_MAJOR_VERSION_LENGTH + METADATA_MINOR_VERSION_LENGTH +
            METADATA_DETAIL_VERSION_LENGTH + METADATA_NUM_ROWS_LENGTH + METADATA_NEXT_PK_LENGTH +
            METADATA_NUM_USED_PHYSICAL_ROWS_LENGTH + METADATA_NUM_AVAILABLE_PHYSICAL_ROWS_LENGTH;

        static void generateSampleDatabase()
        {
            System.IO.Directory.CreateDirectory("TestDB");
            FileStream BinaryFile = new FileStream("TestDB/test.dlr", FileMode.Create, FileAccess.ReadWrite);
            BinaryReader Reader = new BinaryReader(BinaryFile);
            BinaryWriter Writer = new BinaryWriter(BinaryFile);


            // 1 row of data => 
            // ================
            // [Flag  ][ID    ][Name   ][Age   ] 
            // [1 byte][4 byte][20 byte][4 byte] => 29 byte

            int ByteCount_FLAG = 1; // 1:active 0:deleted
            int ByteCount_ID = 4;
            int ByteCount_NAME = 20;
            int ByteCount_AGE = 4;

            int ByteCount_TOTAL = ByteCount_FLAG + ByteCount_ID + ByteCount_NAME + ByteCount_AGE;

            int numDataRow = 1000000;


            Random myRandomGenerator = new Random();

            // Write metadata
            // ==============
            String trademark = "SEACOW"; // 6 bytes
            Byte majorVersion = 1; // 1 bytes
            Byte minorVersion = 0; // 1 bytes
            Byte detailVersion = 0; // 1 bytes

            Int32 numOfRows = 100000; // 4 bytes
            Int32 nextPK = numOfRows + 1; // 4 bytes
            Int32 numOfUsedPhysicalRows = 100000; // 4 bytes
            Int32 numOfAvailablePhysicalRows = 1000000; // 4 bytes

            // Prepare the data to be written to the file
            // ===========================================
            BinaryFile.SetLength(METADATA_TOTAL_LENGTH + (numOfUsedPhysicalRows + numOfAvailablePhysicalRows) * ByteCount_TOTAL);

            Writer.Write(ASCIIEncoding.Default.GetBytes(trademark));
            Writer.Write(majorVersion);
            Writer.Write(minorVersion);
            Writer.Write(detailVersion);

            Writer.Write(System.BitConverter.GetBytes(numOfRows));
            Writer.Write(System.BitConverter.GetBytes(nextPK));
            Writer.Write(System.BitConverter.GetBytes(numOfUsedPhysicalRows));
            Writer.Write(System.BitConverter.GetBytes(numOfAvailablePhysicalRows));

            // Write 5 lines of data
            // ==============

            Int32 tempAge = 20;

            for (Int32 i = 0; i < numDataRow; i++)
            {
                // Write the Flag
                Byte tempFlag = 1;
                Writer.Write(tempFlag);

                // Write the ID
                Int32 tempID = i + 1;
                Writer.Write(System.BitConverter.GetBytes(tempID));

                // Write the Name
                String tempName = ("Name" + (i + 1).ToString()).PadRight(20, ' ').Substring(0, 20);
                Writer.Write(ASCIIEncoding.Default.GetBytes(tempName));

                // Write the Age
                tempAge++;
                if (tempAge > 60)
                { tempAge = 18; }
                
                Writer.Write(System.BitConverter.GetBytes(tempAge));

            }


            BinaryFile.Close();


        }

        #endregion
    }
}
