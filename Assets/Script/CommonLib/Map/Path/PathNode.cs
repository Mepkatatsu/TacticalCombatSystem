using System;
using System.Collections.Generic;

namespace Script.CommonLib.Map.Path
{
    public class PathNode : IEquatable<PathNode>
    {
        public PathNode(GridPos gridPos)
        {
            GridPos = gridPos;
        }
        
        public PathNode(GridPos gridPos, PathNode parent)
        {
            GridPos = gridPos;
            Parent = parent;
        }
        
        public readonly GridPos GridPos;
        public PathNode Parent;

        public float CurrentCost;
        public float HeuristicCost;
        public float TotalCost;
        
        public bool Equals(PathNode other)
        {
            if (other == null)
                return false;
            
            return GridPos.x == other.GridPos.x && GridPos.y == other.GridPos.y;
        }

        public override bool Equals(object obj)
        {
            return obj is PathNode other && Equals(other);
        }

        public override int GetHashCode()
        {
            return GridPos.GetHashCode();
        }
    }
    
    public class PathNodeComparer : IComparer<PathNode>
    {
        public int Compare(PathNode node1, PathNode node2)
        {
            if (node1 == null || node2 == null)
                throw new ArgumentException("One or more PathNode are null.");
            
            var totalCostCompare = node1.TotalCost.CompareTo(node2.TotalCost);
            if (totalCostCompare != 0)
                return totalCostCompare;

            // TotalCost가 같은 경우 GridPos의 x, y 순서로 비교
            var xCompare = node1.GridPos.x.CompareTo(node2.GridPos.x);
            if (xCompare != 0)
                return xCompare;

            return node1.GridPos.y.CompareTo(node2.GridPos.y);
        }
    }
}
