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


        public static DataFrame FromVectors(string name, int vectorLength, Func<int, Vector> getVector)
        {
            if (vectorLength <= 0)
                throw new ArgumentOutOfRangeException("vectorLength");

            if (null == getVector)
                throw new ArgumentNullException("getVector");

            StringBuilder expr = new StringBuilder();
            
            /* data = data.frame(
             * V0 = c(0, 1, 2, 3),
             * V1 = c(4, 5, 6, 7),
             * V2 = c(8, 9, 10, 11))
             */

            expr.Append("data.frame(");

            int numVectors = 0;
            Vector vector = getVector(numVectors);
            while (vector != null)
            {
                if (vector.Values == null)
                    throw new ArgumentNullException("vector.Values");

                if (vector.Values.Length != vectorLength)
                    throw new ArgumentOutOfRangeException("vector.Values.Length");

                expr.AppendFormat("{0} = c(", string.IsNullOrWhiteSpace(vector.Name) ? "V" + numVectors : vector.Name);
                for (int i = 0; i < vectorLength; i++)
                {
                    expr.Append(vector.Values[i]);
                    if (i < vectorLength-1)
                        expr.Append(", ");
                    else
                        expr.Append(")");
                }

                vector = getVector(++numVectors);
                if (vector != null)
                    expr.Append(", ");
                else
                    expr.Append(")");
            }

            return new DataFrame(name, RInterop.SetVariable(name, expr.ToString()));
        }
    }
}
