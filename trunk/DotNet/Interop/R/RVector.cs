using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R.Core
{
    public class RVector
    {
        public readonly IList<string> ColNames = new List<string>();
        public readonly IList<string> RowNames = new List<string>();
        public object[,] Values;
        
        public int NumCols  { get { return this.Values == null ? -1 : this.Values.GetLength(1); } }
        public int NumRows  { get { return this.Values == null ? -1 : this.Values.GetLength(0); } }

        public RVector() { }

        public RVector(object[,] values)
        {
            this.Values = values;
        }

        public virtual void Validate()
        {
            if (this.Values == null)
                throw new ArgumentNullException("this.Values");

            if (this.ColNames.Count > 0 && this.ColNames.Count != this.NumCols)
                throw new ArgumentOutOfRangeException("this.ColNames.Count");

            if (this.RowNames.Count > 0 && this.RowNames.Count != this.NumRows)
                throw new ArgumentOutOfRangeException("this.RowNames.Count");
        }

        public string GetColName(int indx)
        {
            return (indx <= this.ColNames.Count ? this.ColNames[indx] : string.Empty);
        }

        public string GetRowName(int indx)
        {
            return (indx <= this.RowNames.Count ? this.RowNames[indx] : string.Empty);
        }

        internal void SetXVarColNames()
        {
            this.ColNames.Clear();
            for (int j = 0; j < this.NumCols; j++)
            {
                this.ColNames.Add("X" + j);
            }
        }

        internal void SetYVarColNames()
        {
            this.ColNames.Clear();
            this.ColNames.Add("Y");
        }
    }
}
