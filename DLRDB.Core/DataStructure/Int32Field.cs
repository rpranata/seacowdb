using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.DataStructure
{
    /// <summary>
    /// Type of Field that deals with numeric values.
    /// </summary>
    public class Int32Field : Field
    { 
        /// <summary>
        /// Constructor. Sets the Name of this Field to indicate the Column Name, establishes this Field as an Int32 Type.
        /// </summary>
        /// <param name="fieldName">String Name of this Field.</param>
        public Int32Field (Column theColumn)
            : base(theColumn)
        {
        }

        public override Object BytesToNative(Byte[] value)
        {
            return BitConverter.ToInt32(value,0);
        }

        public override Byte[] NativeToBytes(Object value)
        {
            return BitConverter.GetBytes(Convert.ToInt32(value));
        }

      
    }
}
