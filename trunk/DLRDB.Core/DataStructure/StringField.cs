using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.DataStructure
{
    /// <summary>
    /// Type of Field that deals with alpha-numeric values.
    /// </summary>
    public class StringField : DLRDB.Core.DataStructure.Field
    {
        /// <summary>
        /// Constructor. Sets the Name of this Field to indicate the Column Name, establishes this Field as an String Type.
        /// </summary>
        /// <param name="fieldName">String Name of this Field.</param>
        public StringField (Column theColumn)
            : base(theColumn)
        {
        }


        /// <summary>
        /// Notes :we do the Right-Padding to the string to fit the specified length
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Object BytesToNative(Byte[] value)
        {
            return ASCIIEncoding.ASCII.GetString(value).TrimEnd();
        }

        /// <summary>
        /// Notes :we do the Right-Padding to the string to fit the specified length
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Byte[] NativeToBytes(Object value)
        {
            return ASCIIEncoding.Default.GetBytes(value.ToString().PadRight(this.FieldColumn.Length,' '));
        }


    }
}
