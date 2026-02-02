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

        public void RefreshNeighborNodes()
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

            if (currentNode == null)
                return;
            
            resultWaypoints.Add(currentNode.GridPos);
            while (currentNode.Parent != null)
            {
                currentNode = currentNode.Parent;
                resultWaypoints.Add(currentNode.GridPos);
            }
            
            resultWaypoints.Reverse(); // start -> goal 순으로 재정렬
        }

        private static float GetHeuristicCost(GridPos gridPos1, GridPos gridPos2)
        {
            return Mathf.Abs(gridPos1.X - gridPos2.X) + Mathf.Abs(gridPos1.Y - gridPos2.Y);
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
