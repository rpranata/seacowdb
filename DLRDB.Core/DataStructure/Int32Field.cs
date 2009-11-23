using System;
using System.Collections.Generic;
using System.Text;

namespace DLRDB.Core.DataStructure
{
    /// <summary>
    /// Type of Field that deals with numeric values.
    /// </summary>
    public class Int32Field : Field
    { 
        /// <summary>
        /// Constructor: Sets the Name of this Field to 
        /// indicate the Column Name, establishes this 
        /// Field as an Int32 Type.
        /// </summary>
        /// <param name="fieldName">String Name of this Field.</param>
        public Int32Field (Column theColumn) : base(theColumn) { }

        /// <summary>
        /// Overriden method to convert the Byte[]
        /// value to its native type of Int32.
        /// </summary>
        /// <param name="value">Byte[] value to be 
        /// converted to Int32 value.</param>
        /// <returns>Int32 type Object value of the native value.</returns>
        public override Object BytesToNative(Byte[] value)
        {
            lock (base._Lock) 
                { return BitConverter.ToInt32(value, 0); }
        }

        /// <summary>
        /// Overriden method to convert the native 
        /// value into a Byte[].
        /// </summary>
        /// <param name="value">Int32 value to 
        /// be converted into a Byte[].</param>
        /// <returns>Byte[] value of the native Int32 value.</returns>
        public override Byte[] NativeToBytes(Object value)
        {
            lock (base._Lock)
                { return BitConverter.GetBytes(Convert.ToInt32(value)); }
        }
    }
}