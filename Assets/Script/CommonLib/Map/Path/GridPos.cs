using System;

namespace Script.CommonLib
{
    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        public int x;
        public int y;

        private string _toString;

        public GridPos(Vector3 pos)
        {
            x = (int)Math.Round(pos.x);
            y = (int)Math.Round(pos.z);

            _toString = string.Empty;
        }

        public GridPos(int x, int y)
        {
            this.x = x;
            this.y = y;

            _toString = string.Empty;
        }

        public readonly float GetDistance(GridPos pos)
        {
            float dx = x - pos.x;
            float dy = y - pos.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_toString))
            {
                _toString = $"({x}, {y})";
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
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is GridPos other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(x, y);
        }
    }
}
