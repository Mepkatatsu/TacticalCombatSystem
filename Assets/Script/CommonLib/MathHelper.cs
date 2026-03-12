using System;

namespace Script.CommonLib
{
    public static class MathHelper
    {
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
