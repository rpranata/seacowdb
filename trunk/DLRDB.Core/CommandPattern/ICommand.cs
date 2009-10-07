using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.CommandPattern
{
    public interface ICommand
    {
        String ExecuteToMemory();

        String ExecuteToFile();

        void UnexecuteFromFile();

    }
}
