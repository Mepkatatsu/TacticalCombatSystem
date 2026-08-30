using System;
using System.Collections.Generic;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TacticalPositioningTestData;

namespace Script.CommonLib.Tests
{
    public sealed class BattleMapPathSmootherTest : ITest
    {
        public bool Test()
        {
            var success = true;

            success &= Verify<BattleMapPathSmootherTest>(
                TestInitialTransitionIsDeterministicReachableAndSmoother(),
                nameof(TestInitialTransitionIsDeterministicReachableAndSmoother));
            success &= Verify<BattleMapPathSmootherTest>(
                TestInternalCornerIsSmoothedSafely(),
                nameof(TestInternalCornerIsSmoothedSafely));
            success &= Verify<BattleMapPathSmootherTest>(
                TestUnsafeTransitionsPreserveOriginalSafePath(),
                nameof(TestUnsafeTransitionsPreserveOriginalSafePath));
            return success;
        }

        private static bool TestInitialTransitionIsDeterministicReachableAndSmoother()
        {
            var mapData = CreateMapData();
            var pathFinder = new BattleMapPathFinder(mapData);
            var pathSmoother = new BattleMapPathSmoother(mapData, pathFinder);
            var start = new GridPos(0, 0).ToFixedPos();
            var incomingDirection = new FixedDir(new GridPos(-1, 0).ToFixedPos(), start);
            var originalOutgoing = new GridPos(10, 10).ToFixedPos() - start;
            var firstPath = new List<GridPos> { new(10, 10), new(0, 0) };
            var secondPath = new List<GridPos> { new(10, 10), new(0, 0) };

            pathSmoother.SmoothPathTransition(start, incomingDirection, firstPath);
            pathSmoother.SmoothPathTransition(start, incomingDirection, secondPath);

            return HaveSamePath(firstPath, secondPath) &&
                   HasReachableSegments(pathFinder, start.ToGridPos(), firstPath) &&
                   HasAllTurnsSmallerThan(
                       start,
                       incomingDirection.targetFixedPos - incomingDirection.currentFixedPos,
                       firstPath,
                       incomingDirection.targetFixedPos - incomingDirection.currentFixedPos,
                       originalOutgoing);
        }

        private static bool TestInternalCornerIsSmoothedSafely()
        {
            var mapData = CreateMapData();
            var pathFinder = new BattleMapPathFinder(mapData);
            var pathSmoother = new BattleMapPathSmoother(mapData, pathFinder);
            var start = new GridPos(0, 0).ToFixedPos();
            var incomingDirection = new FixedDir(new GridPos(-1, 0).ToFixedPos(), start);
            var path = new List<GridPos> { new(10, 10), new(10, 0), new(0, 0) };
            var originalIncoming = new FixedPos(10, 0, 0);
            var originalOutgoing = new FixedPos(0, 0, 10);

            pathSmoother.SmoothPathTransition(start, incomingDirection, path);

            return HasReachableSegments(pathFinder, start.ToGridPos(), path) &&
                   HasAllTurnsSmallerThan(
                       start,
                       incomingDirection.targetFixedPos - incomingDirection.currentFixedPos,
                       path,
                       originalIncoming,
                       originalOutgoing);
        }

        private static bool TestUnsafeTransitionsPreserveOriginalSafePath()
        {
            return BlockedBlendPreservesOriginalSafePath() &&
                   UTurnPreservesOriginalSafePath();
        }

        private static bool BlockedBlendPreservesOriginalSafePath()
        {
            var mapData = CreateMapData();
            mapData.obstacles.Add(new ObstacleData
            {
                blockedPoints = new List<GridPos> { new(2, 2), new(1, 1) },
                waypoints = new List<GridPos>(),
            });

            return PreservesOriginalSafePath(mapData, new GridPos(0, 10));
        }

        private static bool UTurnPreservesOriginalSafePath()
        {
            return PreservesOriginalSafePath(CreateMapData(), new GridPos(-10, 1));
        }

        private static bool PreservesOriginalSafePath(BattleMapData mapData, GridPos destination)
        {
            var pathFinder = new BattleMapPathFinder(mapData);
            var pathSmoother = new BattleMapPathSmoother(mapData, pathFinder);
            var start = new GridPos(0, 0).ToFixedPos();
            var path = new List<GridPos> { destination, start.ToGridPos() };

            pathSmoother.SmoothPathTransition(
                start,
                new FixedDir(new GridPos(-1, 0).ToFixedPos(), start),
                path);

            return path.Count == 1 &&
                   path[0] == destination &&
                   HasReachableSegments(pathFinder, start.ToGridPos(), path);
        }

        private static bool HaveSamePath(List<GridPos> first, List<GridPos> second)
        {
            if (first.Count != second.Count)
                return false;

            for (var i = 0; i < first.Count; i++)
            {
                if (first[i] != second[i])
                    return false;
            }

            return true;
        }

        private static bool HasReachableSegments(
            BattleMapPathFinder pathFinder,
            GridPos start,
            List<GridPos> path)
        {
            var current = start;
            for (var i = path.Count - 1; i >= 0; i--)
            {
                if (!pathFinder.IsStraightPathReachable(current, path[i]))
                    return false;

                current = path[i];
            }

            return true;
        }

        private static bool HasAllTurnsSmallerThan(
            FixedPos start,
            FixedPos incomingDirection,
            List<GridPos> path,
            FixedPos originalIncoming,
            FixedPos originalOutgoing)
        {
            var current = start;
            var previousDirection = incomingDirection;

            for (var i = path.Count - 1; i >= 0; i--)
            {
                var next = path[i].ToFixedPos();
                var direction = next - current;
                if (!IsAngleLessThan(
                        previousDirection,
                        direction,
                        originalIncoming,
                        originalOutgoing))
                {
                    return false;
                }

                previousDirection = direction;
                current = next;
            }

            return true;
        }

        private static bool IsAngleLessThan(
            FixedPos firstIncoming,
            FixedPos firstOutgoing,
            FixedPos secondIncoming,
            FixedPos secondOutgoing)
        {
            var firstLengthProduct = SquaredLength(firstIncoming) * SquaredLength(firstOutgoing);
            var secondLengthProduct = SquaredLength(secondIncoming) * SquaredLength(secondOutgoing);
            if (firstLengthProduct <= 0 || secondLengthProduct <= 0)
                return false;

            var firstCosine = Dot(firstIncoming, firstOutgoing) / Math.Sqrt(firstLengthProduct);
            var secondCosine = Dot(secondIncoming, secondOutgoing) / Math.Sqrt(secondLengthProduct);
            return firstCosine > secondCosine;
        }

        private static double Dot(FixedPos first, FixedPos second)
        {
            return (double)first.X * second.X + (double)first.Z * second.Z;
        }

        private static double SquaredLength(FixedPos value)
        {
            return (double)value.X * value.X + (double)value.Z * value.Z;
        }

    }
}
