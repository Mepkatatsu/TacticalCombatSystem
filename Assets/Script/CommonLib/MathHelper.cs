using System;

namespace Script.CommonLib
{
    public static class MathHelper
    {
        public static bool Approximately(float a, float b)
        {
            return Math.Abs(b - a) < Math.Max(1E-06f * Math.Max(Math.Abs(a), Math.Abs(b)), MathHelperInternal.Epsilon * 8f);
        }

        public static int IntSqrt(long n)
        {
            if (n <= 0)
                return 0;

            long x = (long)Math.Sqrt(n);
            while (x * x > n) --x;
            while ((x + 1) * (x + 1) <= n) ++x;

            return (int)x;
        }
    }
}
