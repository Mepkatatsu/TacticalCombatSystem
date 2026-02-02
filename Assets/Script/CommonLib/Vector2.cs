using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Script.CommonLib
{
    [Serializable]
    public struct Vector2 : IEquatable<Vector2>, IFormattable
    {
        public float x;

        /// <summary>
        ///   <para>Y component of the vector.</para>
        /// </summary>
        public float y;

        public Vector2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        private static readonly Vector2 zeroVector = new Vector2(0.0f, 0.0f);
        private static readonly Vector2 oneVector = new Vector2(1f, 1f);
        private static readonly Vector2 upVector = new Vector2(0.0f, 1f);
        private static readonly Vector2 downVector = new Vector2(0.0f, -1f);
        private static readonly Vector2 leftVector = new Vector2(-1f, 0.0f);
        private static readonly Vector2 rightVector = new Vector2(1f, 0.0f);

        private static readonly Vector2 positiveInfinityVector =
            new Vector2(float.PositiveInfinity, float.PositiveInfinity);

        private static readonly Vector2 negativeInfinityVector =
            new Vector2(float.NegativeInfinity, float.NegativeInfinity);

        public const float kEpsilon = 1E-05f;
        public const float kEpsilonNormalSqrt = 1E-15f;

        public float this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch (index)
                {
                    case 0:
                        return this.x;
                    case 1:
                        return this.y;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2 index!");
                }
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                switch (index)
                {
                    case 0:
                        this.x = value;
                        break;
                    case 1:
                        this.y = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector2 index!");
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 a, Vector2 b) => new Vector2(a.x + b.x, a.y + b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 a, Vector2 b) => new Vector2(a.x * b.x, a.y * b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 a, Vector2 b) => new Vector2(a.x / b.x, a.y / b.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 a) => new Vector2(-a.x, -a.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 a, float d) => new Vector2(a.x * d, a.y * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(float d, Vector2 a) => new Vector2(a.x * d, a.y * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 a, float d) => new Vector2(a.x / d, a.y / d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2 lhs, Vector2 rhs)
        {
            float num1 = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            return (double) num1 * (double) num1 + (double) num2 * (double) num2 < 9.999999439624929E-11;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2 lhs, Vector2 rhs) => !(lhs == rhs);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3(Vector2 v) => new Vector3(v.x, v.y, 0.0f);

        /// <summary>
        ///   <para>Makes this vector have a magnitude of 1.</para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            float magnitude = this.magnitude;
            if ((double)magnitude > 9.999999747378752E-06)
                this = this / magnitude;
            else
                this = Vector2.zero;
        }

        [JsonIgnore]
        public Vector2 normalized
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Vector2 normalized = new Vector2(this.x, this.y);
                normalized.Normalize();
                return normalized;
            }
        }

        public float magnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return (float)Math.Sqrt((double)this.x * (double)this.x + (double)this.y * (double)this.y); }
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector2(0, 0).</para>
        /// </summary>
        public static Vector2 zero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector2.zeroVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector2(1, 1).</para>
        /// </summary>
        public static Vector2 one
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector2.oneVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector2(0, 1).</para>
        /// </summary>
        public static Vector2 up
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector2.upVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector2(0, -1).</para>
        /// </summary>
        public static Vector2 down
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector2.downVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector2(-1, 0).</para>
        /// </summary>
        public static Vector2 left
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector2.leftVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector2(1, 0).</para>
        /// </summary>
        public static Vector2 right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector2.rightVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector2(float.PositiveInfinity, float.PositiveInfinity).</para>
        /// </summary>
        public static Vector2 positiveInfinity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector2.positiveInfinityVector;
        }

        /// <summary>
        ///   <para>Shorthand for writing Vector2(float.NegativeInfinity, float.NegativeInfinity).</para>
        /// </summary>
        public static Vector2 negativeInfinity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Vector2.negativeInfinityVector;
        }
        
        /// <summary>
        ///   <para>Returns true if the given vector is exactly equal to this vector.</para>
        /// </summary>
        /// <param name="other"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object other) => other is Vector2 other1 && this.Equals(other1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode() << 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2 other)
        {
            return MathHelper.Approximately(x, other.x) && MathHelper.Approximately(y, other.y);
        }
        
        /// <summary>
        ///   <para>Returns a formatted string for this vector.</para>
        /// </summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => this.ToString(null, null);

        /// <summary>
        ///   <para>Returns a formatted string for this vector.</para>
        /// </summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="formatProvider">An object that specifies culture-specific formatting.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format) => this.ToString(format, null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F2";
            if (formatProvider == null)
                formatProvider = CultureInfo.InvariantCulture.NumberFormat;
            return $"({(object)x.ToString(format, formatProvider)}, {(object)y.ToString(format, formatProvider)})";
        }
    }
}