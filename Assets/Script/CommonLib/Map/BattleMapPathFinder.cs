using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Script.CommonLib.Map.Path;
using UnityEngine;

namespace Script.CommonLib.Map
{
    public class BattleMapPathFinder
    {
        private const int DirectionScale = 1000;
        private const int TransitionDistance = 3000;

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

        public void SmoothPathTransition(FixedPos start, FixedDir incomingDirection, List<GridPos> waypoints)
        {
            var startGridPosition = start.ToGridPos();
            while (waypoints.Count > 0 && waypoints[waypoints.Count - 1] == startGridPosition)
                waypoints.RemoveAt(waypoints.Count - 1);

            SmoothPathCorners(startGridPosition, waypoints);
            SmoothInitialPathTransition(start, incomingDirection, waypoints);
        }

        private void SmoothInitialPathTransition(FixedPos start, FixedDir incomingDirection, List<GridPos> waypoints)
        {
            if (waypoints.Count == 0)
                return;

            var startGridPosition = start.ToGridPos();
            var nextGridPosition = waypoints[waypoints.Count - 1];
            var nextPosition = nextGridPosition.ToFixedPos();
            var incomingDelta = incomingDirection.targetFixedPos - incomingDirection.currentFixedPos;
            var outgoingDelta = nextPosition - start;
            var incomingDistance = incomingDirection.currentFixedPos.GetDistance(incomingDirection.targetFixedPos);
            var outgoingDistance = start.GetDistance(nextPosition);
            if (incomingDistance == 0 || outgoingDistance < PositionConverter.FixedPosMultiplier * 2)
                return;

            var incomingX = incomingDelta.X * DirectionScale / incomingDistance;
            var incomingZ = incomingDelta.Z * DirectionScale / incomingDistance;
            var outgoingX = outgoingDelta.X * DirectionScale / outgoingDistance;
            var outgoingZ = outgoingDelta.Z * DirectionScale / outgoingDistance;
            var dot = incomingX * outgoingX + incomingZ * outgoingZ;
            var cross = Math.Abs(incomingX * outgoingZ - incomingZ * outgoingX);

            // 거의 직선인 경로에는 불필요한 waypoint를 추가하지 않는다.
            if (dot > 0 && cross * 6 < dot)
                return;

            // 120도 이상의 U-turn은 짧은 waypoint로 완화하면 되돌아가는 loop가 생길 수 있다.
            if (dot <= -(long)DirectionScale * DirectionScale / 2)
                return;

            // 기존 진행 방향에서 새 경로 방향으로 두 번에 나눠 완만하게 전환한다.
            var firstBlendX = incomingX * 2 + outgoingX;
            var firstBlendZ = incomingZ * 2 + outgoingZ;
            var secondBlendX = incomingX + outgoingX * 2;
            var secondBlendZ = incomingZ + outgoingZ * 2;
            var firstBlendLength = MathHelper.IntSqrt(firstBlendX * firstBlendX + firstBlendZ * firstBlendZ);
            var secondBlendLength = MathHelper.IntSqrt(secondBlendX * secondBlendX + secondBlendZ * secondBlendZ);
            var twoStageMaximumDistance = Math.Min(TransitionDistance, outgoingDistance / 3);

            for (var distance = twoStageMaximumDistance;
                 distance >= PositionConverter.FixedPosMultiplier;
                 distance -= PositionConverter.FixedPosMultiplier)
            {
                var firstPosition = new FixedPos(
                    start.X + firstBlendX * distance / firstBlendLength,
                    start.Y,
                    start.Z + firstBlendZ * distance / firstBlendLength);
                var secondPosition = new FixedPos(
                    firstPosition.X + secondBlendX * distance / secondBlendLength,
                    start.Y,
                    firstPosition.Z + secondBlendZ * distance / secondBlendLength);
                var firstGridPosition = firstPosition.ToGridPos();
                var secondGridPosition = secondPosition.ToGridPos();
                if (!IsTransitionPositionValid(startGridPosition, firstGridPosition, nextGridPosition) ||
                    !IsTransitionPositionValid(firstGridPosition, secondGridPosition, nextGridPosition) ||
                    secondGridPosition == startGridPosition ||
                    !IsStraightPathReachable(secondGridPosition, nextGridPosition))
                {
                    continue;
                }

                if (!HasImprovedMaximumTurn(
                        start,
                        incomingDelta,
                        nextPosition,
                        firstGridPosition.ToFixedPos(),
                        secondGridPosition.ToFixedPos()))
                {
                    continue;
                }

                waypoints.Add(secondGridPosition);
                waypoints.Add(firstGridPosition);
                return;
            }
        }

        private void SmoothPathCorners(GridPos start, List<GridPos> waypoints)
        {
            if (waypoints.Count < 2)
                return;

            var forwardPath = new List<GridPos> { start };
            for (var i = waypoints.Count - 1; i >= 0; i--)
                forwardPath.Add(waypoints[i]);
            var smoothedPath = new List<GridPos> { forwardPath[0] };

            for (var i = 1; i < forwardPath.Count - 1; i++)
            {
                var previous = smoothedPath[smoothedPath.Count - 1];
                var corner = forwardPath[i];
                var next = forwardPath[i + 1];

                if (TryCreateCornerTransition(previous, corner, next, out var entry, out var exit))
                {
                    smoothedPath.Add(entry);
                    smoothedPath.Add(exit);
                }
                else
                {
                    smoothedPath.Add(corner);
                }
            }

            smoothedPath.Add(forwardPath[forwardPath.Count - 1]);
            smoothedPath.RemoveAt(0);
            smoothedPath.Reverse();
            waypoints.Clear();
            waypoints.AddRange(smoothedPath);
        }

        private bool TryCreateCornerTransition(
            GridPos previous,
            GridPos corner,
            GridPos next,
            out GridPos entry,
            out GridPos exit)
        {
            entry = default;
            exit = default;

            var previousPosition = previous.ToFixedPos();
            var cornerPosition = corner.ToFixedPos();
            var nextPosition = next.ToFixedPos();
            var incoming = cornerPosition - previousPosition;
            var outgoing = nextPosition - cornerPosition;
            var incomingDistance = previousPosition.GetDistance(cornerPosition);
            var outgoingDistance = cornerPosition.GetDistance(nextPosition);
            if (incomingDistance < PositionConverter.FixedPosMultiplier * 2 ||
                outgoingDistance < PositionConverter.FixedPosMultiplier * 2 ||
                IsZero(incoming) ||
                IsZero(outgoing))
            {
                return false;
            }

            var incomingX = incoming.X * DirectionScale / incomingDistance;
            var incomingZ = incoming.Z * DirectionScale / incomingDistance;
            var outgoingX = outgoing.X * DirectionScale / outgoingDistance;
            var outgoingZ = outgoing.Z * DirectionScale / outgoingDistance;
            var dot = incomingX * outgoingX + incomingZ * outgoingZ;
            var cross = Math.Abs(incomingX * outgoingZ - incomingZ * outgoingX);
            if (dot > 0 && cross * 6 < dot)
                return false;

            if (dot <= -(long)DirectionScale * DirectionScale / 2)
                return false;

            var maximumDistance = Math.Min(
                TransitionDistance,
                Math.Min(incomingDistance, outgoingDistance) / 3);

            for (var distance = maximumDistance;
                 distance >= PositionConverter.FixedPosMultiplier;
                 distance -= PositionConverter.FixedPosMultiplier)
            {
                var entryPosition = new FixedPos(
                    cornerPosition.X - incoming.X * distance / incomingDistance,
                    cornerPosition.Y,
                    cornerPosition.Z - incoming.Z * distance / incomingDistance);
                var exitPosition = new FixedPos(
                    cornerPosition.X + outgoing.X * distance / outgoingDistance,
                    cornerPosition.Y,
                    cornerPosition.Z + outgoing.Z * distance / outgoingDistance);
                var candidateEntry = entryPosition.ToGridPos();
                var candidateExit = exitPosition.ToGridPos();
                if (candidateEntry == previous ||
                    candidateEntry == corner ||
                    candidateEntry == next ||
                    candidateExit == previous ||
                    candidateExit == corner ||
                    candidateExit == next ||
                    candidateEntry == candidateExit)
                {
                    continue;
                }

                var chord = candidateExit.ToFixedPos() - candidateEntry.ToFixedPos();
                if (IsZero(chord) ||
                    !IsAngleLessThan(incoming, chord, incoming, outgoing) ||
                    !IsAngleLessThan(chord, outgoing, incoming, outgoing) ||
                    !IsStraightPathReachable(previous, candidateEntry) ||
                    !IsStraightPathReachable(candidateEntry, candidateExit) ||
                    !IsStraightPathReachable(candidateExit, next))
                {
                    continue;
                }

                entry = candidateEntry;
                exit = candidateExit;
                return true;
            }

            return false;
        }

        private static bool HasImprovedMaximumTurn(
            FixedPos start,
            FixedPos incomingDirection,
            FixedPos originalNext,
            FixedPos firstTransition,
            FixedPos secondTransition)
        {
            var originalOutgoing = originalNext - start;
            var firstSegment = firstTransition - start;
            var secondSegment = secondTransition - firstTransition;
            var finalSegment = originalNext - secondTransition;
            if (IsZero(incomingDirection) ||
                IsZero(originalOutgoing) ||
                IsZero(firstSegment) ||
                IsZero(secondSegment) ||
                IsZero(finalSegment))
            {
                return false;
            }

            var maximumLeft = incomingDirection;
            var maximumRight = firstSegment;
            if (IsAngleLessThan(maximumLeft, maximumRight, firstSegment, secondSegment))
            {
                maximumLeft = firstSegment;
                maximumRight = secondSegment;
            }

            if (IsAngleLessThan(maximumLeft, maximumRight, secondSegment, finalSegment))
            {
                maximumLeft = secondSegment;
                maximumRight = finalSegment;
            }

            return IsAngleLessThan(maximumLeft, maximumRight, incomingDirection, originalOutgoing);
        }

        internal static bool IsAngleLessThan(
            FixedPos firstLeft,
            FixedPos firstRight,
            FixedPos secondLeft,
            FixedPos secondRight)
        {
            var firstDot = GetDot(firstLeft, firstRight);
            var secondDot = GetDot(secondLeft, secondRight);
            if (firstDot.Sign != secondDot.Sign)
                return firstDot.Sign > secondDot.Sign;

            if (firstDot.IsZero)
                return false;

            var firstSquaredCosineNumerator = firstDot * firstDot;
            var secondSquaredCosineNumerator = secondDot * secondDot;
            var firstLengthProduct = GetLengthSquared(firstLeft) * GetLengthSquared(firstRight);
            var secondLengthProduct = GetLengthSquared(secondLeft) * GetLengthSquared(secondRight);
            var firstComparison = firstSquaredCosineNumerator * secondLengthProduct;
            var secondComparison = secondSquaredCosineNumerator * firstLengthProduct;

            return firstDot.Sign > 0
                ? firstComparison > secondComparison
                : firstComparison < secondComparison;
        }

        private static BigInteger GetDot(FixedPos first, FixedPos second) =>
            (BigInteger)first.X * second.X + (BigInteger)first.Z * second.Z;

        private static BigInteger GetLengthSquared(FixedPos value) =>
            (BigInteger)value.X * value.X + (BigInteger)value.Z * value.Z;

        private static bool IsZero(FixedPos value) => value.X == 0 && value.Z == 0;

        private bool IsTransitionPositionValid(GridPos start, GridPos transition, GridPos next)
        {
            return transition != start &&
                   transition != next &&
                   transition.x >= _battleMapData.minGridPos.x &&
                   transition.x <= _battleMapData.maxGridPos.x &&
                   transition.y >= _battleMapData.minGridPos.y &&
                   transition.y <= _battleMapData.maxGridPos.y &&
                   IsStraightPathReachable(start, transition);
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
