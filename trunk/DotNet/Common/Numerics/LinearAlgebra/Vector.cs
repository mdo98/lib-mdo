using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace MDo.Common.Numerics.LinearAlgebra
{
    public class Vector<TNumeric>
        where TNumeric : struct, IEquatable<TNumeric>, IComparable<TNumeric>
    {
#if X86
        private readonly Func<uint, uint, IntPtr> GslVecAlloc;
        private readonly Func<IntPtr, uint, uint, TNumeric> GslVecGetValue;
        private readonly Action<IntPtr, uint, uint, TNumeric> GslVecSetValue;
#else
        private readonly Func<ulong, IntPtr> GslVecAlloc;
        private readonly Func<IntPtr, ulong, TNumeric> GslVecGetValue;
        private readonly Action<IntPtr, ulong, TNumeric> GslVecSetValue;
#endif
        private readonly Action<IntPtr> GslVecSetZero;
        private readonly Action<IntPtr, TNumeric> GslVecSetAll;
        private IntPtr _V;

        public Vector(uint len, TNumeric[] data = null)
        {
            if (typeof(double) == typeof(TNumeric))
            {
                GslVecAlloc = gsl_vector_alloc;
                GslVecSetValue = gsl_vector_set;
                GslVecGetValue = gsl_vector_get;
                GslVecSetZero = gsl_vector_set_zero;
                GslVecSetAll = gsl_vector_set_all;
            }
            else if (typeof(int) == typeof(TNumeric))
            {
                GslVecAlloc = gsl_vector_int_alloc;
                GslVecSetValue = gsl_vector_int_set;
                GslVecGetValue = gsl_vector_int_get;
                GslVecSetZero = gsl_vector_int_set_zero;
                GslVecSetAll = gsl_vector_int_set_all;
            }
            else if (typeof(long) == typeof(TNumeric))
            {
                GslVecAlloc = gsl_vector_long_alloc;
                GslVecSetValue = gsl_vector_long_set;
                GslVecGetValue = gsl_vector_long_get;
                GslVecSetZero = gsl_vector_long_set_zero;
                GslVecSetAll = gsl_vector_long_set_all;
            }

            this.Length = len;

            if (data != null)
            {
                if (data.Length != len)
                    throw new ArgumentException("data");
            }

            _V = GslVecAlloc(len);
            if (data != null)
            {
                for (uint i = 0; i < len; i++)
                {
                    GslVecSetValue(_V, i, data[i]);
                }
            }
        }

        ~Vector()
        {
            gsl_vector_free(_V);
        }


        #region Fields & Properties

        private uint _len;

        public uint Length
        {
            get
            {
                return _len;
            }
            private set
            {
                if (value <= 0U)
                    throw new ArgumentOutOfRangeException("Length");
                _len = value;
            }
        }

        #endregion Fields & Properties


        #region Special Vectors

        public static Vector<TNumeric> Zero(uint len)
        {
            Vector<TNumeric> v = new Vector<TNumeric>(len);
            v.GslVecSetZero(v._V);
            return v;
        }

        public static Vector<TNumeric> All(uint len, TNumeric val)
        {
            Vector<TNumeric> v = new Vector<TNumeric>(len);
            v.GslVecSetAll(v._V, val);
            return v;
        }

        #endregion Special Vectors


        #region Imports

        #region Memory Management

#if X86
        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_vector_alloc(uint n);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_vector_int_alloc(uint n);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_vector_long_alloc(uint n);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_set(IntPtr m, uint i, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_int_set(IntPtr m, uint i, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_long_set(IntPtr m, uint i, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_vector_get(IntPtr m, uint i);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_vector_int_get(IntPtr m, uint i);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_vector_long_get(IntPtr m, uint i);
#else
        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_vector_alloc(ulong n);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_vector_int_alloc(ulong n);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gsl_vector_long_alloc(ulong n);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_set(IntPtr m, ulong i, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_int_set(IntPtr m, ulong i, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_long_set(IntPtr m, ulong i, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_vector_get(IntPtr m, ulong i);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_vector_int_get(IntPtr m, ulong i);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern TNumeric gsl_vector_long_get(IntPtr m, ulong i);
#endif

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_set_zero(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_int_set_zero(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_long_set_zero(IntPtr m);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_set_all(IntPtr m, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_int_set_all(IntPtr m, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_long_set_all(IntPtr m, TNumeric x);

        [DllImport(Gsl.GSL_PATH, CallingConvention = CallingConvention.Cdecl)]
        private static extern void gsl_vector_free(IntPtr m);

        #endregion Memory Management



        #endregion Imports
    }
}
