using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MDo.Interop.R.Core
{
    public class DataFrame : RObject
    {
        #region Constructors

        private DataFrame(IntPtr ptr, string name = null) : base(ptr, name)
        {
        }

        #endregion Constructors


        #region RObject

        protected override void OnPtrSet()
        {
            // No-Ops
        }

        #endregion RObject


        public static DataFrame FromVectors(string name, params RVector[] vectors)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name");

            if (null == vectors || vectors.Length <= 0)
                throw new ArgumentOutOfRangeException("vector.Length");

            return new DataFrame(RInterop.Eval(ExpressionFromVectors(vectors), name), name);
        }

        public static string ExpressionFromVectors(params RVector[] vectors)
        {
            StringBuilder expr = new StringBuilder();
            
            /* data = data.frame(
             * V0 = c(0, 1, 2, 3),
             * V1 = c(4, 5, 6, 7),
             * V2 = c(8, 9, 10, 11))
             */

            expr.Append("data.frame(");

            int numVectors = 0;
            int numRows = -1;
            for (int n = 0; n < vectors.Length; n++)
            {
                RVector vector = vectors[n];
                vector.Validate();

                if (numRows < 0)
                    numRows = vector.NumRows;
                else if (vector.NumRows != numRows)
                    throw new ArgumentException("Vectors must have the same number of rows.");

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

            return expr.ToString();
        }

        public static string CsvFromVectors(params RVector[] vectors)
        {
            if (null == vectors || vectors.Length <= 0)
                throw new ArgumentOutOfRangeException("vector.Length");

            StringBuilder expr = new StringBuilder();

            // Header
            int numVectors = 0;
            int numRows = -1;
            for (int n = 0; n < vectors.Length; n++)
            {
                RVector vector = vectors[n];
                vector.Validate();

                if (numRows < 0)
                    numRows = vector.NumRows;
                else if (vector.NumRows != numRows)
                    throw new ArgumentException("Vectors must have the same number of rows.");

                for (int j = 0; j < vector.NumCols; j++)
                {
                    string colName = vector.GetColName(j);
                    expr.Append(string.IsNullOrWhiteSpace(colName) ? "V" + (numVectors++) : colName);

                    if (n < vectors.Length-1 || j < vector.NumCols-1)
                        expr.Append("\t");
                }
            }
            expr.Append(Environment.NewLine);

            // Data
            for (int i = 0; i < numRows; i++)
            {
                for (int n = 0; n < vectors.Length; n++)
                {
                    RVector vector = vectors[n];
                    for (int j = 0; j < vector.NumCols; j++)
                    {
                        Type type = vector.Values[i, j].GetType();
                        if (typeof(double) == type || typeof(int) == type  || typeof(long) == type  || typeof(short) == type ||
                            typeof(float) == type  || typeof(uint) == type || typeof(ulong) == type || typeof(ushort) == type)
                            expr.Append(vector.Values[i, j]);
                        else
                            expr.AppendFormat("\"{0}\"", vector.Values[i, j]);

                        if (n < vectors.Length-1 || j < vector.NumCols-1)
                            expr.Append("\t");
                    }
                }
                if (i < numRows-1)
                    expr.Append(Environment.NewLine);
            }

            return expr.ToString();
        }
    }
}
