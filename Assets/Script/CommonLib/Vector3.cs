using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Script.CommonLib
{
    [Serializable]
    public struct Vector3 : IEquatable<Vector3>, IFormattable
    {
        public const float kEpsilon = 1E-05f;
        public const float kEpsilonNormalSqrt = 1E-15f;
        /// <summary>
        ///   <para>X component of the vector.</para>
        /// </summary>
        public float x;
        /// <summary>
        ///   <para>Y component of the vector.</para>
        /// </summary>
        public float y;
        /// <summary>
        ///   <para>Z component of the vector.</para>
        /// </summary>
        public float z;
        private static readonly Vector3 zeroVector = new Vector3(0.0f, 0.0f, 0.0f);
        private static readonly Vector3 oneVector = new Vector3(1f, 1f, 1f);
        private static readonly Vector3 upVector = new Vector3(0.0f, 1f, 0.0f);
        private static readonly Vector3 downVector = new Vector3(0.0f, -1f, 0.0f);
        private static readonly Vector3 leftVector = new Vector3(-1f, 0.0f, 0.0f);
        private static readonly Vector3 rightVector = new Vector3(1f, 0.0f, 0.0f);
        private static readonly Vector3 forwardVector = new Vector3(0.0f, 0.0f, 1f);
        private static readonly Vector3 backVector = new Vector3(0.0f, 0.0f, -1f);
        private static readonly Vector3 positiveInfinityVector = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        private static readonly Vector3 negativeInfinityVector = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 a, Vector3 b)
        {
            return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.x, -a.y, -a.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 a, float d) => new Vector3(a.x * d, a.y * d, a.z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(float d, Vector3 a) => new Vector3(a.x * d, a.y * d, a.z * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 a, float d) => new Vector3(a.x / d, a.y / d, a.z / d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3 lhs, Vector3 rhs)
        {
            float num1 = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            float num3 = lhs.z - rhs.z;
            return (double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3 < 9.999999439624929E-11;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3 lhs, Vector3 rhs) => !(lhs == rhs);
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other) => other is Vector3 other1 && Equals(other1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector3 other)
        {
            return MathHelper.Approximately(x, other.x) && MathHelper.Approximately(y, other.y) && MathHelper.Approximately(z, other.z);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => this.ToString((string) null, (IFormatProvider) null);

        /// <summary>
        ///   <para>Returns a formatted string for this vector.</para>
        /// </summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format) => this.ToString(format, (IFormatProvider) null);

        /// <summary>
        ///   <para>Returns a formatted string for this vector.</para>
        /// </summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = (IFormatProvider) CultureInfo.InvariantCulture.NumberFormat;
            return $"({(object)this.x.ToString(format, formatProvider)}, {(object)this.y.ToString(format, formatProvider)}, {(object)this.z.ToString(format, formatProvider)})";
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return this.x.GetHashCode() ^ this.y.GetHashCode() << 2 ^ this.z.GetHashCode() >> 2;
        }
    
        public Vector3 normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Normalize(this);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vector3 a, Vector3 b)
        {
            float num1 = a.x - b.x;
            float num2 = a.y - b.y;
            float num3 = a.z - b.z;
            return (float) Math.Sqrt((double) num1 * (double) num1 + (double) num2 * (double) num2 + (double) num3 * (double) num3);
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalize(Vector3 value)
        {
            float num = Vector3.Magnitude(value);
            return (double) num > 9.999999747378752E-06 ? value / num : Vector3.zero;
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Magnitude(Vector3 vector)
        {
            return (float) Math.Sqrt((double) vector.x * (double) vector.x + (double) vector.y * (double) vector.y + (double) vector.z * (double) vector.z);
        }
    
        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, 0, 0).</para>
        /// </summary>
        public static Vector3 zero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.zeroVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(1, 1, 1).</para>
        /// </summary>
        public static Vector3 one
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.oneVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, 0, 1).</para>
        /// </summary>
        public static Vector3 forward
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.forwardVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, 0, -1).</para>
        /// </summary>
        public static Vector3 back
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.backVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, 1, 0).</para>
        /// </summary>
        public static Vector3 up
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.upVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(0, -1, 0).</para>
        /// </summary>
        public static Vector3 down
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.downVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(-1, 0, 0).</para>
        /// </summary>
        public static Vector3 left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.leftVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(1, 0, 0).</para>
        /// </summary>
        public static Vector3 right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.rightVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity).</para>
        /// </summary>
        public static Vector3 positiveInfinity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.positiveInfinityVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity).</para>
        /// </summary>
        public static Vector3 negativeInfinity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => Vector3.negativeInfinityVector;
        }
    }
}
