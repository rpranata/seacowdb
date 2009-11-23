using System;
using System.Collections.Generic;
using System.Text;

namespace DLRDB.Core.DataStructure
{
    /// <summary>
    /// Type of Field that deals with alpha-numeric values.
    /// </summary>
    public class StringField : DLRDB.Core.DataStructure.Field
    {
        /// <summary>
        /// Constructor: Sets the Name of this Field to indicate the
        /// Column Name, establishes this Field as an String Type.
        /// </summary>
        /// <param name="fieldName">String Name of this Field.</param>
        public StringField (Column theColumn) : base(theColumn) { }

        /// <summary>
        /// Overriden method to convert the Byte[] value to its native
        /// type of String. Note: value will be truncated or padded to
        /// the right when applicable to meet the Column length and
        /// fixed Column length critieria. Trailing white spaces will
        /// be removed prior to returning. ASCII Encoding is used for
        /// Byte conversion.
        /// </summary>
        /// <param name="value">Byte[] value to be converted to String
        /// value.</param>
        /// <returns>String value of the native value.</returns>
        public override Object BytesToNative(Byte[] value)
        {
            lock (base._Lock)
            { return ASCIIEncoding.ASCII.GetString(value).TrimEnd(); }
        }

        /// <summary>
        /// Overriden method to convert the String value to a Byte[].
        /// Note: value will be truncated or padded to the right when
        /// applicable to meet the Column length and fixed Column 
        /// length critieria. Trailing white spaces will be removed 
        /// prior to returning. ASCII Encoding is used for Byte 
        /// conversion.
        /// </summary>
        /// <param name="value">String value to be converted to Byte[].</param>
        /// <returns>Byte[] of the native String value.</returns>
        public override Byte[] NativeToBytes(Object value)
        {
            lock (base._Lock)
            {
                return ASCIIEncoding.Default.GetBytes(value.ToString()
                    .PadRight(this.FieldColumn.Length, ' '));
            }
        }
    }
}