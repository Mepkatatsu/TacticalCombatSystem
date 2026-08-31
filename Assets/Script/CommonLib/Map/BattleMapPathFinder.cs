using System.Collections.Generic;
using System.Linq;
using Script.CommonLib.Map.Path;
using UnityEngine;

namespace Script.CommonLib.Map
{
    public class BattleMapPathFinder
    {
        private readonly BattleMapData _battleMapData;
        private readonly BresenhamSuperCoverNodeVisitor _visitor = new();
        private readonly Dictionary<GridPos, List<GridPos>> _authoredNeighborGridPosDic = new();
        private readonly Dictionary<GridPos, List<GridPos>> _queryNeighborGridPosDic = new();
        private readonly SortedSet<PathNode> _openSet = new(new PathNodeComparer());
        private readonly HashSet<PathNode> _closedSet = new();
        private readonly Dictionary<GridPos, PathNode> _nodeMap = new();

        public BattleMapPathFinder(BattleMapData battleMapData)
        {
            _battleMapData = battleMapData;
            RefreshAuthoredNeighborNodes();
        }

        private List<GridPos> Waypoints => _battleMapData.Waypoints;
        private HashSet<GridPos> BlockedPoints => _battleMapData.BlockedPoints;

        public bool TryFindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
        {
            return TryFindWaypointsCore(start, goal, _authoredNeighborGridPosDic, resultWaypoints);
        }

        public bool TryFindWaypointsBetweenAnyPositions(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
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

            RefreshQueryNeighborNodes(start, goal);
            return TryFindWaypointsCore(start, goal, _queryNeighborGridPosDic, resultWaypoints);
        }

        public bool IsStraightPathReachable(GridPos start, GridPos goal)
        {
            if (BlockedPoints.Contains(start) || BlockedPoints.Contains(goal))
                return false;

            return _visitor.VisitPath(start, goal, (x, y) => BlockedPoints.Contains(new GridPos(x, y)));
        }

        private bool TryFindWaypointsCore(
            GridPos start,
            GridPos goal,
            Dictionary<GridPos, List<GridPos>> neighborGridPositionMap,
            List<GridPos> resultWaypoints)
        {
            _openSet.Clear();
            _closedSet.Clear();
            _nodeMap.Clear();
            resultWaypoints.Clear();

            if (BlockedPoints.Contains(start) || BlockedPoints.Contains(goal))
                return false;

            // TODO: 풀링으로 메모리 할당 적게 수정하면 좋을 듯 Ex)PathNode.Create(gridPos)
            var startNode = new PathNode(start);
            _nodeMap.Add(start, startNode);
            _openSet.Add(startNode);

            PathNode goalNode = null;

            while (_openSet.Count > 0)
            {
                var currentNode = _openSet.First();
                _openSet.Remove(currentNode);

                if (currentNode.GridPos == goal)
                {
                    goalNode = currentNode;
                    break;
                }

                _closedSet.Add(currentNode);

                if (neighborGridPositionMap.TryGetValue(currentNode.GridPos, out var neighbors))
                    UpdateNeighborNodes(currentNode, neighbors, goal);
            }

            if (goalNode == null)
                return false;

            var resultNode = goalNode;
            while (resultNode != null)
            {
                resultWaypoints.Add(resultNode.GridPos);
                resultNode = resultNode.Parent;
            }

            return true;
        }

        private void UpdateNeighborNodes(PathNode currentNode, List<GridPos> neighbors, GridPos goal)
        {
            for (var i = 0; i < neighbors.Count; i++)
            {
                var gridPos = neighbors[i];
                if (!_nodeMap.TryGetValue(gridPos, out var neighborNode))
                {
                    neighborNode = new PathNode(gridPos, currentNode);
                    _nodeMap.Add(gridPos, neighborNode);
                }

                if (_closedSet.Contains(neighborNode))
                    continue;

                var newCost = currentNode.CurrentCost + currentNode.GridPos.GetDistance(gridPos);
                if (newCost >= neighborNode.CurrentCost && _openSet.Contains(neighborNode))
                    continue;

                if (_openSet.Contains(neighborNode))
                    _openSet.Remove(neighborNode);

                neighborNode.CurrentCost = newCost;
                neighborNode.HeuristicCost = GetHeuristicCost(gridPos, goal);
                neighborNode.TotalCost = neighborNode.CurrentCost + neighborNode.HeuristicCost;
                neighborNode.Parent = currentNode;
                _openSet.Add(neighborNode);
            }
        }

        private void RefreshAuthoredNeighborNodes()
        {
            _authoredNeighborGridPosDic.Clear();

            for (var i = 0; i < Waypoints.Count; i++)
            {
                var startNode = Waypoints[i];

                for (var j = 0; j < Waypoints.Count; j++)
                {
                    if (i == j)
                        continue;

                    var endNode = Waypoints[j];
                    if (IsStraightPathReachable(startNode, endNode))
                        AddAuthoredNeighborNode(startNode, endNode);
                }
            }
        }

        private void RefreshQueryNeighborNodes(GridPos start, GridPos goal)
        {
            _queryNeighborGridPosDic.Clear();

            for (var i = 0; i < Waypoints.Count; i++)
            {
                var current = Waypoints[i];
                var neighbors = GetOrCreateQueryNeighbors(current);

                if (_authoredNeighborGridPosDic.TryGetValue(current, out var authoredNeighbors))
                    neighbors.AddRange(authoredNeighbors);

                if (current != goal && IsStraightPathReachable(current, goal))
                    AddUnique(neighbors, goal);

                neighbors.Sort(CompareGridPos);
            }

            var startNeighbors = GetOrCreateQueryNeighbors(start);
            for (var i = 0; i < Waypoints.Count; i++)
            {
                var waypoint = Waypoints[i];
                if (waypoint != start && IsStraightPathReachable(start, waypoint))
                    AddUnique(startNeighbors, waypoint);
            }

            startNeighbors.Sort(CompareGridPos);
        }

        private List<GridPos> GetOrCreateQueryNeighbors(GridPos gridPosition)
        {
            if (_queryNeighborGridPosDic.TryGetValue(gridPosition, out var neighbors))
                return neighbors;

            neighbors = new List<GridPos>();
            _queryNeighborGridPosDic.Add(gridPosition, neighbors);
            return neighbors;
        }

        private static void AddUnique(List<GridPos> neighbors, GridPos neighbor)
        {
            if (!neighbors.Contains(neighbor))
                neighbors.Add(neighbor);
        }

        private static long GetHeuristicCost(GridPos first, GridPos second)
        {
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y);
        }

        private static int CompareGridPos(GridPos first, GridPos second)
        {
            var xComparison = first.x.CompareTo(second.x);
            return xComparison != 0 ? xComparison : first.y.CompareTo(second.y);
        }

        private void AddAuthoredNeighborNode(GridPos key, GridPos value)
        {
            if (!_authoredNeighborGridPosDic.TryGetValue(key, out var list))
            {
                list = new List<GridPos>();
                _authoredNeighborGridPosDic.Add(key, list);
            }

            list.Add(value);
        }

        public void Refresh()
        {
            Waypoints.Clear();
            BlockedPoints.Clear();
            RefreshAuthoredNeighborNodes();
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
