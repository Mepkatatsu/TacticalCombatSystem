using System;
using System.Collections.Generic;
using System.Numerics;

namespace Script.CommonLib.Map
{
    internal sealed class BattleMapPathSmoother
    {
        private const int DirectionScale = 1000;
        private const int TransitionDistance = 3000;

        private readonly BattleMapData _battleMapData;
        private readonly BattleMapPathFinder _battleMapPathFinder;

        public BattleMapPathSmoother(BattleMapData battleMapData, BattleMapPathFinder battleMapPathFinder)
        {
            _battleMapData = battleMapData;
            _battleMapPathFinder = battleMapPathFinder;
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

            for (var distance = twoStageMaximumDistance; distance >= PositionConverter.FixedPosMultiplier;
                 distance -= PositionConverter.FixedPosMultiplier)
            {
                var firstPosition = new FixedPos(start.X + firstBlendX * distance / firstBlendLength, start.Y,
                    start.Z + firstBlendZ * distance / firstBlendLength);
                var secondPosition = new FixedPos(firstPosition.X + secondBlendX * distance / secondBlendLength,
                    start.Y, firstPosition.Z + secondBlendZ * distance / secondBlendLength);
                var firstGridPosition = firstPosition.ToGridPos();
                var secondGridPosition = secondPosition.ToGridPos();
                if (!IsTransitionPositionValid(startGridPosition, firstGridPosition, nextGridPosition) ||
                    !IsTransitionPositionValid(firstGridPosition, secondGridPosition, nextGridPosition) ||
                    secondGridPosition == startGridPosition ||
                    !_battleMapPathFinder.IsStraightPathReachable(secondGridPosition, nextGridPosition))
                {
                    continue;
                }

                if (!HasImprovedMaximumTurn(start, incomingDelta, nextPosition,
                        firstGridPosition.ToFixedPos(), secondGridPosition.ToFixedPos()))
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

        private bool TryCreateCornerTransition(GridPos previous, GridPos corner, GridPos next,
            out GridPos entry, out GridPos exit)
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

            var maximumDistance = Math.Min(TransitionDistance, Math.Min(incomingDistance, outgoingDistance) / 3);

            for (var distance = maximumDistance; distance >= PositionConverter.FixedPosMultiplier;
                 distance -= PositionConverter.FixedPosMultiplier)
            {
                var entryPosition = new FixedPos(cornerPosition.X - incoming.X * distance / incomingDistance,
                    cornerPosition.Y, cornerPosition.Z - incoming.Z * distance / incomingDistance);
                var exitPosition = new FixedPos(cornerPosition.X + outgoing.X * distance / outgoingDistance,
                    cornerPosition.Y, cornerPosition.Z + outgoing.Z * distance / outgoingDistance);
                var candidateEntry = entryPosition.ToGridPos();
                var candidateExit = exitPosition.ToGridPos();
                if (candidateEntry == previous || candidateEntry == corner || candidateEntry == next ||
                    candidateExit == previous || candidateExit == corner || candidateExit == next ||
                    candidateEntry == candidateExit)
                {
                    continue;
                }

                var chord = candidateExit.ToFixedPos() - candidateEntry.ToFixedPos();
                if (IsZero(chord) ||
                    !IsAngleLessThan(incoming, chord, incoming, outgoing) ||
                    !IsAngleLessThan(chord, outgoing, incoming, outgoing) ||
                    !_battleMapPathFinder.IsStraightPathReachable(previous, candidateEntry) ||
                    !_battleMapPathFinder.IsStraightPathReachable(candidateEntry, candidateExit) ||
                    !_battleMapPathFinder.IsStraightPathReachable(candidateExit, next))
                {
                    continue;
                }

                entry = candidateEntry;
                exit = candidateExit;
                return true;
            }

            return false;
        }

        private static bool HasImprovedMaximumTurn(FixedPos start, FixedPos incomingDirection, FixedPos originalNext,
            FixedPos firstTransition, FixedPos secondTransition)
        {
            var originalOutgoing = originalNext - start;
            var firstSegment = firstTransition - start;
            var secondSegment = secondTransition - firstTransition;
            var finalSegment = originalNext - secondTransition;
            if (IsZero(incomingDirection) || IsZero(originalOutgoing) || IsZero(firstSegment) ||
                IsZero(secondSegment) || IsZero(finalSegment))
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
            FixedPos firstLeft, FixedPos firstRight, FixedPos secondLeft, FixedPos secondRight)
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

            return firstDot.Sign > 0 ? firstComparison > secondComparison : firstComparison < secondComparison;
        }

        private static BigInteger GetDot(FixedPos first, FixedPos second) =>
            (BigInteger)first.X * second.X + (BigInteger)first.Z * second.Z;

        private static BigInteger GetLengthSquared(FixedPos value) =>
            (BigInteger)value.X * value.X + (BigInteger)value.Z * value.Z;

        private static bool IsZero(FixedPos value) => value.X == 0 && value.Z == 0;

        private bool IsTransitionPositionValid(GridPos start, GridPos transition, GridPos next)
        {
            return transition != start && transition != next &&
                   transition.x >= _battleMapData.minGridPos.x && transition.x <= _battleMapData.maxGridPos.x &&
                   transition.y >= _battleMapData.minGridPos.y && transition.y <= _battleMapData.maxGridPos.y &&
                   _battleMapPathFinder.IsStraightPathReachable(start, transition);
        }
    }
}
