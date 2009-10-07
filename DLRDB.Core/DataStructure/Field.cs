using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.DataStructure
{
    public abstract class Field
    {
        private Column _Column;
        private Row _ParentRow;

        private byte[] _Value;
        private byte[] _OriginalValue;

        /// <summary>
        /// Constructor. Name of Field, Type of Field(Int32 or String)
        /// </summary>
        /// <param name="name">Name of the Field</param>
        /// <param name="theColumn">The Column reference for this field</param>
        public Field(Column theColumn,Object value)
        {
            this._Column = theColumn;
            this._OriginalValue = NativeToBytes(value);
            this.Value = NativeToBytes(value);
        }

        public Field(Column theColumn)
        {
            this._Column = theColumn;
        }
        
        /// <summary>
        /// 
        /// </summary>
        public Column FieldColumn
        {
            get
            {
                return this._Column;
            }
        }
       
        
        /// <summary>
        /// Gets/Sets the parent Row of the Field. Used for backward referencing.
        /// </summary>
        public Row ParentRow
        {
            get {return this._ParentRow;}
            set {this._ParentRow = value;}
        }

        public Byte[] Value
        {
            get
            {
                return this._Value;
            }
            set
            {
                this._Value = value;
            }
        }

        public Byte[] OriginalValue
        {
            get
            {
                return this._OriginalValue;
            }
            set
            {
                this._OriginalValue = value;
            }
        }

        /// <summary>
        /// Overrides the ToString method to only return the Value of this Field.
        /// </summary>
        /// <returns>Value of this Field. IS already a String type, so no conversion is required.</returns>
        public String ToString()
        {
            return this.BytesToNative(this.Value).ToString();
        }

        public abstract Object BytesToNative(Byte[] value);
        public abstract Byte[] NativeToBytes(Object value);
        
    }
}
