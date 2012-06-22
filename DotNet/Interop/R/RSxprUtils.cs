using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MDo.Interop.R.Core;

namespace MDo.Interop.R
{
    internal static class RSxprUtils
    {
        public static RVector RVectorFromStdRSxpr(IntPtr val)
        {
            RInterop.RSEXPREC ans = RInterop.RSEXPREC.FromPointer(val);
            IntPtr attribPtr = ans.Header.Attrib;
            int vLength = ans.Content.VLength;

            RVector vector = new RVector();
            if (attribPtr == RInterop.NullPtr)
            {
                vector.Values = new object[vLength, 1];
            }
            else
            {
                RInterop.RSEXPREC attrib = RInterop.NextNodeFromListSxprPtr(attribPtr);

                Action<IntPtr, int, IList<string>> getRowOrColNames = (IntPtr ptr, int numItems, IList<string> itemList) =>
                {
                    RInterop.RSEXPREC names = RInterop.RSEXPREC.FromPointer(ptr);
                    if (names.Header.SxpInfo.Type == RInterop.RSXPTYPE.NILSXP)
                        return;

                    {
                        const RInterop.RSXPTYPE expectedSxpType = RInterop.RSXPTYPE.STRSXP;
                        if (names.Header.SxpInfo.Type != expectedSxpType)
                            throw new RInteropException(string.Format(
                                "Invalid type for R name attribute node: expecting {0}, actual {1}.",
                                expectedSxpType,
                                names.Header.SxpInfo.Type));

                        int expectedVLength = numItems;
                        if (names.Content.VLength < expectedVLength)
                            throw new RInteropException(string.Format(
                                "Invalid length for R name attribute node: expecting {0}, actual {1}.",
                                expectedVLength,
                                names.Content.VLength));
                    }

                    for (int i = 0; i < numItems; i++)
                    {
                        string name;
                        unsafe { name = new string((sbyte*)RInterop.RSEXPREC.ValSxp_GetElement(RInterop.RSEXPREC.VecSxp_GetElement(ptr, i), 0, sizeof(sbyte))); }
                        itemList.Add(name);
                    }
                };

                if (attrib.Content.listsxp_cdrval == RInterop.NullPtr)
                {
                    vector.Values = new object[vLength, 1];
                    getRowOrColNames(attrib.Content.listsxp_carval, vLength, vector.RowNames);
                }
                else
                {
                    int numRows, numCols;
                    unsafe { numRows = *((int*)RInterop.RSEXPREC.ValSxp_GetElement(attrib.Content.listsxp_carval, 0, sizeof(int))); };
                    unsafe { numCols = *((int*)RInterop.RSEXPREC.ValSxp_GetElement(attrib.Content.listsxp_carval, 1, sizeof(int))); };

                    if (numRows * numCols != vLength)
                        throw new RInteropException(string.Format(
                            "Invalid number of rows, columns, and items: {0} * {1} != {2}",
                            numRows,
                            numCols,
                            vLength));

                    vector.Values = new object[numRows, numCols];

                    IntPtr rowAndColNamesPtr = RInterop.NextNodeFromListSxprPtr(attrib.Content.listsxp_cdrval).Content.listsxp_carval;
                    RInterop.RSEXPREC rowAndColNames = RInterop.RSEXPREC.FromPointer(rowAndColNamesPtr);
                    {
                        const RInterop.RSXPTYPE expectedSxpType = RInterop.RSXPTYPE.VECSXP;
                        if (rowAndColNames.Header.SxpInfo.Type != expectedSxpType)
                            throw new RInteropException(string.Format(
                                "Invalid type for R row/col-name container node: expecting {0}, actual {1}.",
                                expectedSxpType,
                                rowAndColNames.Header.SxpInfo.Type));

                        const int expectedVLength = 2;
                        if (rowAndColNames.Content.VLength < expectedVLength)
                            throw new RInteropException(string.Format(
                                "Invalid length for R row/col-name container node: expecting {0}, actual {1}.",
                                expectedVLength,
                                rowAndColNames.Content.VLength));
                    }
                    getRowOrColNames(RInterop.RSEXPREC.VecSxp_GetElement(rowAndColNamesPtr, 0), numRows, vector.RowNames);
                    getRowOrColNames(RInterop.RSEXPREC.VecSxp_GetElement(rowAndColNamesPtr, 1), numCols, vector.ColNames);
                }
            }
            return vector;
        }
    }
}
