namespace Script.CommonLib
{
    public struct MathHelperInternal
    {
        public static volatile float FloatMinNormal = 1.1754944E-38f;
        public static volatile float FloatMinDenormal = float.Epsilon;
        public static readonly bool IsFlushToZeroEnabled = (double)FloatMinDenormal == 0.0;
    
        public static readonly float Epsilon = IsFlushToZeroEnabled ? FloatMinNormal : FloatMinDenormal;
    }
}
