using System;

namespace Script.CommonLib
{
    public static class MathHelper
    {
        public static bool Approximately(float a, float b)
        {
            return Math.Abs(b - a) < Math.Max(1E-06f * Math.Max(Math.Abs(a), Math.Abs(b)), MathHelperInternal.Epsilon * 8f);
        }
    }
}
