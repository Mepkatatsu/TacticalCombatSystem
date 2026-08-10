using System.Collections.Generic;
using System.Linq;
using Script.CommonLib.Map.Path;
using UnityEngine;

namespace Script.CommonLib.Map
{
    public class BattleMapPathFinder
    {
        public BattleMapPathFinder(BattleMapData battleMapData)
        {
            _battleMapData = battleMapData;
            RefreshNeighborNodes();
        }
        
        private readonly BattleMapData _battleMapData;
        private readonly BresenhamSuperCoverNodeVisitor _visitor = new();

        private List<GridPos> Waypoints => _battleMapData.Waypoints;
        private HashSet<GridPos> BlockedPoints => _battleMapData.BlockedPoints;
        
        private readonly Dictionary<GridPos, List<GridPos>> _fullNeighborGridPosDic = new();
        private readonly SortedSet<PathNode> _openSet = new(new PathNodeComparer());
        private readonly HashSet<PathNode> _closedSet = new();
        private readonly Dictionary<GridPos, PathNode> _nodeMap = new();

        private void RefreshNeighborNodes()
        {
            RefreshFullNeighborNodes();
        }

        private void RefreshFullNeighborNodes()
        {
            _fullNeighborGridPosDic.Clear();

            for (var i = 0; i < Waypoints.Count; i++)
            {
                var startNode = Waypoints[i];

                for (var j = 0; j < Waypoints.Count; j++)
                {
                    if (i == j)
                        continue;
                    
                    var endNode = Waypoints[j];
                    var visible = _visitor.VisitPath(startNode, endNode, (x, y) =>
                    {
                        var gridPos = new GridPos(x, y);
                        return BlockedPoints.Contains(gridPos);
                    });

                    if (visible)
                        AddNeighborNode(startNode, endNode);
                }
            }
        }

        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
        {
            _openSet.Clear();
            _closedSet.Clear();
            _nodeMap.Clear();
            
            // TODO: 풀링으로 메모리 할당 적게 수정하면 좋을 듯 Ex)PathNode.Create(gridPos)
            var startNode = new PathNode(start);
            
            _nodeMap.Add(start, startNode);
            _openSet.Add(startNode);

            PathNode currentNode = null;
            
            while (_openSet.Count > 0)
            {
                currentNode = _openSet.First();
                _openSet.Remove(currentNode);

                if (currentNode.GridPos == goal)
                {
                    break;
                }

                _closedSet.Add(currentNode);

                if (!_fullNeighborGridPosDic.TryGetValue(currentNode.GridPos, out var neighborGridPosList))
                    continue;
                
                foreach (var gridPos in neighborGridPosList)
                {
                    if (!_nodeMap.TryGetValue(gridPos, out var neighborNode))
                    {
                        neighborNode = new PathNode(gridPos, currentNode);
                        _nodeMap.Add(gridPos, neighborNode);
                    }
                    
                    if (_closedSet.Contains(neighborNode))
                        continue;

                    var newCost = currentNode.CurrentCost + currentNode.GridPos.GetDistance(neighborNode.GridPos);
                    
                    if (newCost < neighborNode.CurrentCost || !_openSet.Contains(neighborNode))
                    {
                        if (_openSet.Contains(neighborNode))
                            _openSet.Remove(neighborNode);
                        
                        neighborNode.CurrentCost = newCost;
                        neighborNode.HeuristicCost = GetHeuristicCost(neighborNode.GridPos, goal);
                        neighborNode.TotalCost = neighborNode.CurrentCost + neighborNode.HeuristicCost;
                        neighborNode.Parent = currentNode;

                        _openSet.Add(neighborNode);
                    }
                }
            }

            resultWaypoints.Clear();
            
            while (currentNode != null)
            {
                resultWaypoints.Add(currentNode.GridPos);
                currentNode = currentNode.Parent;
            }
        }

        private static long GetHeuristicCost(GridPos gridPos1, GridPos gridPos2)
        {
            return Mathf.Abs(gridPos1.x - gridPos2.x) + Mathf.Abs(gridPos1.y - gridPos2.y);
        }

        public bool IsStraightPathReachable(GridPos start, GridPos goal)
        {
            if (BlockedPoints.Contains(start) || BlockedPoints.Contains(goal))
                return false;

            return _visitor.VisitPath(start, goal, (x, y) => BlockedPoints.Contains(new GridPos(x, y)));
        }

        public bool TryFindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
        {
            resultWaypoints.Clear();

            if (BlockedPoints.Contains(start) || BlockedPoints.Contains(goal))
                return false;

            if (IsStraightPathReachable(start, goal))
            {
                resultWaypoints.Add(goal);
                resultWaypoints.Add(start);
                return true;
            }

            var openSet = new SortedSet<PathNode>(new PathNodeComparer());
            var closedSet = new HashSet<PathNode>();
            var nodeMap = new Dictionary<GridPos, PathNode>();
            var startNode = new PathNode(start);

            nodeMap.Add(start, startNode);
            openSet.Add(startNode);

            PathNode goalNode = null;

            while (openSet.Count > 0)
            {
                var currentNode = openSet.First();
                openSet.Remove(currentNode);

                if (currentNode.GridPos == goal)
                {
                    goalNode = currentNode;
                    break;
                }

                closedSet.Add(currentNode);
                var neighbors = GetTransientNeighbors(currentNode.GridPos, start, goal);

                for (var i = 0; i < neighbors.Count; i++)
                {
                    var gridPos = neighbors[i];
                    if (!nodeMap.TryGetValue(gridPos, out var neighborNode))
                    {
                        neighborNode = new PathNode(gridPos, currentNode);
                        nodeMap.Add(gridPos, neighborNode);
                    }

                    if (closedSet.Contains(neighborNode))
                        continue;

                    var newCost = currentNode.CurrentCost + currentNode.GridPos.GetDistance(gridPos);
                    if (newCost >= neighborNode.CurrentCost && openSet.Contains(neighborNode))
                        continue;

                    if (openSet.Contains(neighborNode))
                        openSet.Remove(neighborNode);

                    neighborNode.CurrentCost = newCost;
                    neighborNode.HeuristicCost = GetHeuristicCost(gridPos, goal);
                    neighborNode.TotalCost = neighborNode.CurrentCost + neighborNode.HeuristicCost;
                    neighborNode.Parent = currentNode;
                    openSet.Add(neighborNode);
                }
            }

            if (goalNode == null)
                return false;

            while (goalNode != null)
            {
                resultWaypoints.Add(goalNode.GridPos);
                goalNode = goalNode.Parent;
            }

            return true;
        }

        private List<GridPos> GetTransientNeighbors(GridPos current, GridPos start, GridPos goal)
        {
            var neighbors = new List<GridPos>();

            if (_fullNeighborGridPosDic.TryGetValue(current, out var authoredNeighbors))
                neighbors.AddRange(authoredNeighbors);

            if (current == start)
            {
                for (var i = 0; i < Waypoints.Count; i++)
                {
                    var waypoint = Waypoints[i];
                    if (waypoint != start && IsStraightPathReachable(start, waypoint) && !neighbors.Contains(waypoint))
                        neighbors.Add(waypoint);
                }
            }

            if (current != goal && IsStraightPathReachable(current, goal) && !neighbors.Contains(goal))
                neighbors.Add(goal);

            neighbors.Sort(CompareGridPos);
            return neighbors;
        }

        private static int CompareGridPos(GridPos first, GridPos second)
        {
            var xComparison = first.x.CompareTo(second.x);
            return xComparison != 0 ? xComparison : first.y.CompareTo(second.y);
        }

        private void AddNeighborNode(GridPos key, GridPos value)
        {
            if (!_fullNeighborGridPosDic.TryGetValue(key, out var list))
            {
                list = new List<GridPos>();
                _fullNeighborGridPosDic.Add(key, list);
            }
            
            list.Add(value);
        }

        public void Refresh()
        {
            Waypoints.Clear();
            BlockedPoints.Clear();
            RefreshNeighborNodes();
        }

        public void FindStraightPathWithoutBlock(GridPos startNode, GridPos endNode, List<GridPos> result)
        {
            _visitor.VisitPath(startNode, endNode, (x, y) =>
            {
                result.Add(new GridPos(x, y));
                return false;
            });
        }
    }
}
