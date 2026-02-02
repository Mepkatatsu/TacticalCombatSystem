using System;
using UnityEngine;

namespace Script.CommonLib
{
    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        public readonly int X;
        public readonly int Y;

        private string _toString;

        public GridPos(int x, int y)
        {
            X = x;
            Y = y;

            _toString = string.Empty;
        }

        public readonly float GetDistance(GridPos pos)
        {
            float dx = X - pos.X;
            float dy = Y - pos.Y;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_toString))
            {
                _toString = $"({X}, {Y})";
            }
            
            return _toString;
        }
        
        public static bool operator ==(GridPos gridPos1, GridPos gridPos2)
        {
            return gridPos1.Equals(gridPos2);
        }
        
        public static bool operator !=(GridPos gridPos1, GridPos gridPos2)
        {
            return !gridPos1.Equals(gridPos2);
        }

        public bool Equals(GridPos other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}
