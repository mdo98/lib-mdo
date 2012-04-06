using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R.Core
{
    public class DataFrame
    {
        #region Constructors

        private DataFrame(string name, IntPtr ptr)
        {
            this.Name = name;
            this.DataPtr = ptr;
        }

        #endregion Constructors


        #region Properties

        public string Name      { get; private set; }
        public IntPtr DataPtr   { get; private set; }

        #endregion Properties


        public static DataFrame FromVectors(string name, params RVector[] vectors)
        {
            if (null == vectors || vectors.Length <= 0)
                throw new ArgumentOutOfRangeException("vector.Length");

            StringBuilder expr = new StringBuilder();
            
            /* data = data.frame(
             * V0 = c(0, 1, 2, 3),
             * V1 = c(4, 5, 6, 7),
             * V2 = c(8, 9, 10, 11))
             */

            expr.Append("data.frame(");

            int numVectors = 0;
            for (int n = 0; n < vectors.Length; n++)
            {
                RVector vector = vectors[n];
                vector.Validate();

                for (int j = 0; j < vector.NumCols; j++)
                {
                    string colName = vector.GetColName(j);
                    expr.AppendFormat("{0} = c(", string.IsNullOrWhiteSpace(colName) ? "V" + (numVectors++) : colName);
                    for (int i = 0; i < vector.NumRows; i++)
                    {
                        Type type = vector.Values[i, j].GetType();
                        if (typeof(double) == type || typeof(int)  == type || typeof(long)  == type || typeof(short)  == type ||
                            typeof(float)  == type || typeof(uint) == type || typeof(ulong) == type || typeof(ushort) == type)
                            expr.Append(vector.Values[i, j]);
                        else
                            expr.AppendFormat("\"{0}\"", vector.Values[i, j]);
                        if (i < vector.NumRows-1)
                            expr.Append(", ");
                        else
                            expr.Append(")");
                    }

                    if (n < vectors.Length-1 || j < vector.NumCols-1)
                        expr.Append(", ");
                    else
                        expr.Append(")");
                }
            }

            return new DataFrame(name, RInterop.SetVariable(name, expr.ToString()));
        }
    }
}
