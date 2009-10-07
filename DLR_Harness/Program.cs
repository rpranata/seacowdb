using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DLRDB.Core.DataStructure;

namespace DLR_Harness
{
    class Program
    {
        static void Main(string[] args)
        {
            Table myTable = new Table("Contact", "TestDB/test.dlr");

            Row[] mySelectRow = null;
            
            // Selecting all in the disk
            // ===================================
            Console.WriteLine();
            mySelectRow = myTable.Select(200,450);
            foreach (Row tempRow in mySelectRow)
            {
                Console.WriteLine(tempRow.ToString());
            }


            // Insert 1 row to the disk
            // ===========================
            // Fields[0] is an ID, thus, we don't need to specify this

            Console.WriteLine();
            Row myNewRow = null;

            //myNewRow = myTable.NewRow();                        
            //myNewRow.Fields[1].Value = myNewRow.Fields[1].NativeToBytes("NewName");
            //myNewRow.Fields[2].Value = myNewRow.Fields[2].NativeToBytes(99);
            //myTable.InsertRow(myNewRow);

            for (int i = 1; i <= 10000000; i++ )
            {
                myNewRow = myTable.NewRow();
                myNewRow.Fields[1].Value = myNewRow.Fields[1].NativeToBytes("NewName" + i);
                myNewRow.Fields[2].Value = myNewRow.Fields[2].NativeToBytes(10 + (i % 10));

                myTable.InsertRow(myNewRow);
            }

            // Selecting all in the disk
            // ===================================

            Console.WriteLine();
            mySelectRow = myTable.SelectAll();
            foreach (Row tempRow in mySelectRow)
            {
                Console.WriteLine(tempRow.ToString());
            }

            Console.ReadLine();

        }
    }
}
