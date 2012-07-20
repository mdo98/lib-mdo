using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Numerics.LinearAlgebra
{
    public class Matrix<TNumeric>
        where TNumeric : struct, IEquatable<TNumeric>, IComparable<TNumeric>
    {
#if X86
        private readonly Func<uint, uint, IntPtr> GslMatAlloc;
        private readonly Func<IntPtr, uint, uint, TNumeric> GslMatGetValue;
        private readonly Action<IntPtr, uint, uint, TNumeric> GslMatSetValue;
#else
        private readonly Func<ulong, ulong, IntPtr> GslMatAlloc;
        private readonly Func<IntPtr, ulong, ulong, TNumeric> GslMatGetValue;
        private readonly Action<IntPtr, ulong, ulong, TNumeric> GslMatSetValue;
#endif
        private readonly Action<IntPtr> GslMatSetZero;
        private readonly Action<IntPtr> GslMatSetIdentity;
        private readonly Action<IntPtr, TNumeric> GslMatSetAll;
        private IntPtr _M;

        public Matrix(uint numRows, uint numCols, TNumeric[,] data = null)
        {
            if (typeof(double) == typeof(TNumeric))
            {
                GslMatAlloc = gsl_matrix_alloc;
                GslMatSetValue = gsl_matrix_set;
                GslMatGetValue = gsl_matrix_get;
                GslMatSetZero = gsl_matrix_set_zero;
                GslMatSetIdentity = gsl_matrix_set_identity;
                GslMatSetAll = gsl_matrix_set_all;
            }
            else if (typeof(int) == typeof(TNumeric))
            {
                GslMatAlloc = gsl_matrix_int_alloc;
                GslMatSetValue = gsl_matrix_int_set;
                GslMatGetValue = gsl_matrix_int_get;
                GslMatSetZero = gsl_matrix_int_set_zero;
                GslMatSetIdentity = gsl_matrix_int_set_identity;
                GslMatSetAll = gsl_matrix_int_set_all;
            }
            else if (typeof(long) == typeof(TNumeric))
            {
                GslMatAlloc = gsl_matrix_long_alloc;
                GslMatSetValue = gsl_matrix_long_set;
                GslMatGetValue = gsl_matrix_long_get;
                GslMatSetZero = gsl_matrix_long_set_zero;
                GslMatSetIdentity = gsl_matrix_long_set_identity;
                GslMatSetAll = gsl_matrix_long_set_all;
            }

            this.NumRows = numRows;
            this.NumCols = numCols;

            if (data != null)
            {
                if (data.GetLength(0) != numRows || data.GetLength(1) != numCols)
                    throw new ArgumentException("data");
            }

            _M = GslMatAlloc(numRows, numCols);
            if (data != null)
            {
                for (uint i = 0; i < numRows; i++)
                {
                    for (uint j = 0; j < numCols; j++)
                    {
                        GslMatSetValue(_M, i, j, data[i, j]);
                    }
                }
            }
        }

        ~Matrix()
        {
            gsl_matrix_free(_M);
        }


        #region Fields & Properties

        private uint _numRows, _numCols;

        public uint NumRows
        {
            get
            {
                return _numRows;
            }
            private set
            {
                if (value <= 0U)
                    throw new ArgumentOutOfRangeException("NumRows");
                _numRows = value;
            }
        }

        public uint NumCols
        {
            get
            {
                return _numCols;
            }
            private set
            {
                if (value <= 0U)
                    throw new ArgumentOutOfRangeException("NumCols");
                _numCols = value;
            }
        }

        #endregion Fields & Properties


        #region Special Matrices

        public static Matrix<TNumeric> Zero(uint numRows, uint numCols)
        {
            Matrix<TNumeric> m = new Matrix<TNumeric>(numRows, numCols);
            m.GslMatSetZero(m._M);
            return m;
        }

        public static Matrix<TNumeric> Identity(uint size)
        {
            Matrix<TNumeric> m = new Matrix<TNumeric>(size, size);
            m.GslMatSetIdentity(m._M);
            return m;
        }

        public static Matrix<TNumeric> All(uint numRows, uint numCols, TNumeric val)
        {
            Matrix<TNumeric> m = new Matrix<TNumeric>(numRows, numCols);
            m.GslMatSetAll(m._M, val);
            return m;
        }

        #endregion Special Matrices


        #region Operations

        public static Matrix<TNumeric> operator * (Matrix<TNumeric> A, Matrix<TNumeric> B)
        {
            return Multiply(A, MatrixVariant.None, B, MatrixVariant.None);
        }

        public static Matrix<TNumeric> Multiply(Matrix<TNumeric> A, MatrixVariant A_Variant, Matrix<TNumeric> B, MatrixVariant B_Variant)
        {
            uint M, N, KA, KB;
            switch (A_Variant)
            {
                case MatrixVariant.None:
                    M = A.NumRows;
                    KA = A.NumCols;
                    break;

                case MatrixVariant.Transpose:
                    M = A.NumCols;
                    KA = A.NumRows;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("A_Variant");
            }
            switch (B_Variant)
            {
                case MatrixVariant.None:
                    N = B.NumCols;
                    KB = B.NumRows;
                    break;

                case MatrixVariant.Transpose:
                    N = B.NumRows;
                    KB = B.NumCols;
                    break;

                default:
                    throw new ArgumentOutOfRangeException("A_Variant");
            }
            if (KA != KB)
                throw new InvalidOperationException("MATRIX_SIZE_MISMATCH");
            Matrix<TNumeric> P = new Matrix<TNumeric>(M, N);
            Gsl.Invoke(() => gsl_linalg_matmult_mod(A._M, (int)A_Variant, B._M, (int)B_Variant, P._M));
            return P;
        }

        #endregion Operations


        #region Imports

        public enum MatrixVariant
        {
            None = 0,
            Transpose = 1,
        }

        #region Memory Management

#if X86
        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_matrix_alloc(uint n1, uint n2);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_matrix_int_alloc(uint n1, uint n2);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_matrix_long_alloc(uint n1, uint n2);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_set(IntPtr m, uint i, uint j, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_int_set(IntPtr m, uint i, uint j, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_long_set(IntPtr m, uint i, uint j, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_matrix_get(IntPtr m, uint i, uint j);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_matrix_int_get(IntPtr m, uint i, uint j);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_matrix_long_get(IntPtr m, uint i, uint j);
#else
        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_matrix_alloc(ulong n1, ulong n2);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_matrix_int_alloc(ulong n1, ulong n2);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_matrix_long_alloc(ulong n1, ulong n2);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_set(IntPtr m, ulong i, ulong j, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_int_set(IntPtr m, ulong i, ulong j, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_long_set(IntPtr m, ulong i, ulong j, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_matrix_get(IntPtr m, ulong i, ulong j);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_matrix_int_get(IntPtr m, ulong i, ulong j);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_matrix_long_get(IntPtr m, ulong i, ulong j);
#endif

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_set_zero(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_int_set_zero(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_long_set_zero(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_set_identity(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_int_set_identity(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_long_set_identity(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_set_all(IntPtr m, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_int_set_all(IntPtr m, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_long_set_all(IntPtr m, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_matrix_free(IntPtr m);

        #endregion Memory Management

        #region Operations

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern int gsl_linalg_matmult_mod(IntPtr A, int modA, IntPtr B, int modB, IntPtr P);

        #endregion Operations

        #endregion Imports
    }
}
