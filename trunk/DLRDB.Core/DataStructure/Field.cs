using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DLRDB.Core.DataStructure
{
    public abstract class Field
    {
        private readonly Column _Column;
        private readonly Row _ParentRow;
        protected readonly Object _Lock = new Object();

        private byte[] _Value;
        private byte[] _OriginalValue;

        /// <summary>
        /// Constructor. Name of Field, Type of Field(Int32 or String)
        /// </summary>
        /// <param name="name">Name of the Field</param>
        /// <param name="theColumn">The Column reference for this field</param>
        public Field(Column theColumn, Object value)
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
        public Column FieldColumn { get { return this._Column; } }
        
        /// <summary>
        /// Gets/Sets the parent Row of the Field. Used for backward referencing.
        /// </summary>
        public Row ParentRow { get { return this._ParentRow; } }

        public Byte[] Value
        {
            get
            {
                lock (this._Lock) { return this._Value; }
            }
            set
            {
                lock (this._Lock) { this._Value = value; }
            }
        }

        public Byte[] OriginalValue
        {
            get
            {
                lock (this._Lock) { return this._OriginalValue; }
            }
            set
            {
                lock (this._Lock){ this._OriginalValue = value; }
            }
        }

        /// <summary>
        /// Overrides the ToString method to only return the Value of this Field.
        /// </summary>
        /// <returns>
        /// Value of this Field. IS already a String type, 
        /// so no conversion is required.
        /// </returns>
        public override String ToString()
        {
            return this.BytesToNative(this.Value).ToString();
        }

        public abstract Object BytesToNative(Byte[] value);
        public abstract Byte[] NativeToBytes(Object value);
    }
}