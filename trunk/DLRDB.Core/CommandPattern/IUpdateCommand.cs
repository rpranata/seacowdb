using System;
namespace DLRDB.Core.CommandPattern
{
    interface IUpdateCommand
    {
        string ExecuteToFile();
        string ExecuteToMemory();
        void UnexecuteFromFile();
    }
}
