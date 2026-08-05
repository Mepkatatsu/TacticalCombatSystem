using System;

namespace Script.CommonLib
{
    [Serializable]
    public readonly struct FixedPos : IEquatable<FixedPos>
    {
        public readonly long X; // 고정 소수점 좌표. 1000 unit은 1 world unit에 해당한다.
        public readonly long Y; // 고정 소수점 좌표. 1000 unit은 1 world unit에 해당한다.
        public readonly long Z; // 고정 소수점 좌표. 1000 unit은 1 world unit에 해당한다.

        public FixedPos(long x, long y, long z)
        {
            X = x;
            Y = y;
            Z = z;
        }
        
        public long GetDistanceSq(FixedPos pos)
        {
            long dx = X - pos.X;
            long dy = Y - pos.Y;
            long dz = Z - pos.Z;

            return dx * dx + dy * dy + dz * dz;
        }
        
        public long GetDistance(FixedPos pos) => MathHelper.IntSqrt(GetDistanceSq(pos));
        
        public static FixedPos operator +(FixedPos a, FixedPos b) 
            => new FixedPos(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static FixedPos operator -(FixedPos a, FixedPos b) 
            => new FixedPos(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static FixedPos operator *(FixedPos fixedPos, int value) 
            => new FixedPos(fixedPos.X * value, fixedPos.Y * value, fixedPos.Z * value);
        public static FixedPos operator /(FixedPos fixedPos, int value) 
            => new FixedPos(fixedPos.X / value, fixedPos.Y / value, fixedPos.Z / value);
        
        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
        
        public static bool operator ==(FixedPos fixedPos1, FixedPos fixedPos2)
        {
            return fixedPos1.Equals(fixedPos2);
        }
        
        public static bool operator !=(FixedPos fixedPos1, FixedPos fixedPos2)
        {
            return !fixedPos1.Equals(fixedPos2);
        }

        public bool Equals(FixedPos other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            return obj is FixedPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }
    }
}
