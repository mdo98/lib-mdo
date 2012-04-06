using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

using MDo.Interop.R.Test;

namespace MDo.Interop.R.Models.Test
{
    public static class ModelsTestUtils
    {
        public const string Namespace = "Models";

        public static int Parse_LinearModelData(Stream input, out double[,] x, out double[,] y)
        {
            x = null; y = null;
            int numCols = 0, numRows = 0, numRowsProcessed = 0;
            using (TextReader textReader = new StreamReader(input))
            {
                string line = null;
                bool hasNumColsAndRows = false;
                while ((line = textReader.ReadLine()) != null)
                {
                    if (line.StartsWith("#"))
                        continue;

                    string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (!hasNumColsAndRows)
                    {
                        if (parts.Length < 2)
                            TestUtils.ThrowInvalidDataStream();
                        int numDims = int.Parse(parts[0].Trim());
                        switch (parts[1].Trim().ToLowerInvariant())
                        {
                            case "columns":
                                numCols = numDims;
                                break;

                            case "rows":
                                numRows = numDims;
                                break;

                            default:
                                TestUtils.ThrowInvalidDataStream();
                                break;
                        }
                        if (numCols > 0 && numRows > 0)
                        {
                            hasNumColsAndRows = true;
                            for (int j = 0; j < numCols; j++)
                                textReader.ReadLine();
                            x = new double[numRows, numCols-2];
                            y = new double[numRows, 1];
                        }
                    }
                    else
                    {
                        if (parts.Length != numCols)
                            TestUtils.ThrowInvalidDataStream();
                        if (numRowsProcessed >= numRows)
                            TestUtils.ThrowInvalidDataStream();

                        for (int j = 1; j < numCols-1; j++)
                            x[numRowsProcessed,j-1] = double.Parse(parts[j].Trim());
                        y[numRowsProcessed,0] = double.Parse(parts[numCols-1].Trim());
                        numRowsProcessed++;
                    }
                }
            }
            if (numRowsProcessed != numRows)
                TestUtils.ThrowInvalidDataStream();
            return numRows;
        }
    }
}
