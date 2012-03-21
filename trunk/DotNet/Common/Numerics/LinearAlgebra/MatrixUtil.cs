using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MDo.Common.Numerics.LinearAlgebra
{
    public static class MatrixUtil
    {
        public static void Power(double[,] M, int eM, out double[,] MP, out int eMP, int power, int eNorm, double eNormFactor)
        {
            int mLength = M.GetLength(0);
            if (mLength != M.GetLength(1))
                throw new InvalidOperationException("Cannot raise a non-square matrix to a power.");

            if (power < 0)
                throw new ArgumentOutOfRangeException("exponent");
            
            if (power == 1)
            {
                MP = new double[mLength, mLength];
                for (int i = 0; i < mLength; i++)
                {
                    for (int j = 0; j < mLength; j++)
                    {
                        MP[i, j] = M[i, j];
                    }
                }
                eMP = eM;
                return;
            }

            Power(M, eM, out MP, out eMP, power >> 1, eNorm, eNormFactor);

            double[,] MP_Square = Multiply(MP, MP);
            eMP *= 2;

            if ((power & 1) == 0)
            {
                MP = MP_Square;
            }
            else
            {
                MP = Multiply(M, MP_Square);
                eMP += eM;
            }

            if (MP[mLength / 2, mLength / 2] > (1.0 / eNormFactor))
            {
                var MP_tmp = MP;
                Parallel.For(0, mLength, (int i) =>
                {
                    for (int j = 0; j < mLength; j++)
                    {
                        MP_tmp[i, j] = MP_tmp[i, j] * eNormFactor;
                    }
                });
                eMP += eNorm;
            }
        }

        public static double[,] Multiply(double[,] M1, double[,] M2)
        {
            int mLength = M1.GetLength(1);
            if (mLength != M2.GetLength(0))
                throw new InvalidOperationException("M1 and M2 have incompatible sizes and cannot be multiplied.");

            int m1Length = M1.GetLength(0),
                m2Length = M2.GetLength(1);
            double[,] MP = new double[m1Length, m2Length];

            Parallel.For(0, m1Length, (int i) =>
            {
                for (int j = 0; j < m2Length; j++)
                {
                    double s = 0.0;
                    for (int k = 0; k < mLength; k++)
                    {
                        s += (M1[i, k] * M2[k, j]);
                    }
                    MP[i, j] = s;
                }
            });
            return MP;
        }
    }
}
