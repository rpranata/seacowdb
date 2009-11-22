using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.DataStructure
{
    public class Column
    {
        private System.Type _NativeType;
        private String _Name;
        private Int32 _Length;
        
        /// <summary>
        /// Constructor. Name of Field, Type of Field(Int32 or String)
        /// </summary>
        /// <param name="name">Name of the Field</param>
        /// <param name="nativeFieldType">Type of the Field(Int32, String)</param>
        public Column(String name, System.Type nativeFieldType, Int32 length)
        {
            this._Name = name;
            this._NativeType = nativeFieldType;
            this._Length = length;
        }

        /// <summary>
        /// Gets the name of the Field. String value.
        /// </summary>
        public String Name
        { get { return this._Name; } }
    
        /// <summary>
        /// Gets FieldType(Int32, String) for validation.
        /// </summary>
        public System.Type NativeType
        { get { return this._NativeType; } }

        /// <summary>
        /// 
        /// </summary>
        public Int32 Length
        { get { return this._Length; } }
    }
}
