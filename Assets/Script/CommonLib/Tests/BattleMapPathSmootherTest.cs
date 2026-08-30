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

            success &= Verify(TestSmoothPathTransitionSplitsCornerDeterministically(), nameof(TestSmoothPathTransitionSplitsCornerDeterministically));
            success &= Verify(TestSmoothPathTransitionKeepsOriginalPathWhenBlendIsBlocked(), nameof(TestSmoothPathTransitionKeepsOriginalPathWhenBlendIsBlocked));
            success &= Verify(TestSmoothPathTransitionKeepsUTurns(), nameof(TestSmoothPathTransitionKeepsUTurns));
            success &= Verify(TestSmoothPathTransitionReducesInternalCorner(), nameof(TestSmoothPathTransitionReducesInternalCorner));
            success &= Verify(TestIntegerAngleOrderingBoundaries(), nameof(TestIntegerAngleOrderingBoundaries));
            return success;
        }

        private static bool TestSmoothPathTransitionSplitsCornerDeterministically()
        {
            var mapData = CreateMapData();
            var pathFinder = new BattleMapPathFinder(mapData);
            var pathSmoother = new BattleMapPathSmoother(mapData, pathFinder);
            var start = new GridPos(0, 0).ToFixedPos();
            var incomingDirection = new FixedDir(new GridPos(-1, 0).ToFixedPos(), start);
            var firstPath = new List<GridPos> { new(10, 10), new(0, 0) };
            var secondPath = new List<GridPos> { new(10, 10), new(0, 0) };

            pathSmoother.SmoothPathTransition(start, incomingDirection, firstPath);
            pathSmoother.SmoothPathTransition(start, incomingDirection, secondPath);

            return firstPath.Count >= 3 &&
                   HaveSamePath(firstPath, secondPath) &&
                   HasReachableSegments(pathFinder, firstPath) &&
                   HasAllTurnsSmallerThanOriginal(incomingDirection, firstPath, new GridPos(10, 10));
        }

        private static bool TestSmoothPathTransitionKeepsOriginalPathWhenBlendIsBlocked()
        {
            var mapData = CreateMapData();
            mapData.obstacles.Add(new ObstacleData
            {
                blockedPoints = new List<GridPos> { new(2, 2), new(1, 1) },
                waypoints = new List<GridPos>(),
            });
            var pathFinder = new BattleMapPathFinder(mapData);
            var pathSmoother = new BattleMapPathSmoother(mapData, pathFinder);
            var start = new GridPos(0, 0).ToFixedPos();
            var path = new List<GridPos> { new(0, 10), new(0, 0) };

            pathSmoother.SmoothPathTransition(
                start,
                new FixedDir(new GridPos(-1, 0).ToFixedPos(), start),
                path);

            return path.Count == 1 && path[0] == new GridPos(0, 10);
        }

        private static bool TestSmoothPathTransitionKeepsUTurns()
        {
            var mapData = CreateMapData();
            var pathSmoother = new BattleMapPathSmoother(mapData, new BattleMapPathFinder(mapData));
            var start = new GridPos(0, 0).ToFixedPos();
            var incomingDirection = new FixedDir(new GridPos(-1, 0).ToFixedPos(), start);
            var destinations = new[] { new GridPos(-10, 0), new GridPos(-10, 1) };

            for (var i = 0; i < destinations.Length; i++)
            {
                var path = new List<GridPos> { destinations[i], new(0, 0) };
                pathSmoother.SmoothPathTransition(start, incomingDirection, path);
                if (path.Count != 1 || path[0] != destinations[i])
                    return false;
            }

            return true;
        }

        private static bool TestSmoothPathTransitionReducesInternalCorner()
        {
            var mapData = CreateMapData();
            var pathFinder = new BattleMapPathFinder(mapData);
            var pathSmoother = new BattleMapPathSmoother(mapData, pathFinder);
            var start = new GridPos(0, 0).ToFixedPos();
            var path = new List<GridPos> { new(10, 10), new(10, 0), new(0, 0) };
            var originalIncoming = new FixedPos(10, 0, 0);
            var originalOutgoing = new FixedPos(0, 0, 10);

            pathSmoother.SmoothPathTransition(
                start,
                new FixedDir(new GridPos(-1, 0).ToFixedPos(), start),
                path);

            if (path.Count < 3 || !HasReachableSegments(pathFinder, path))
                return false;

            var previousDirection = path[path.Count - 2].ToFixedPos() - path[path.Count - 1].ToFixedPos();
            for (var i = path.Count - 2; i > 0; i--)
            {
                var direction = path[i - 1].ToFixedPos() - path[i].ToFixedPos();
                if (!BattleMapPathSmoother.IsAngleLessThan(
                        previousDirection,
                        direction,
                        originalIncoming,
                        originalOutgoing))
                {
                    return false;
                }

                previousDirection = direction;
            }

            return true;
        }

        private static bool TestIntegerAngleOrderingBoundaries()
        {
            var forward = new FixedPos(1, 0, 0);
            var shallow = new FixedPos(2, 0, 1);
            var diagonal = new FixedPos(1, 0, 1);
            var perpendicular = new FixedPos(0, 0, 1);
            var obtuse = new FixedPos(-1, 0, 1);
            var deeperObtuse = new FixedPos(-2, 0, 1);

            return BattleMapPathSmoother.IsAngleLessThan(forward, shallow, forward, diagonal) &&
                   !BattleMapPathSmoother.IsAngleLessThan(forward, diagonal, forward, shallow) &&
                   !BattleMapPathSmoother.IsAngleLessThan(forward, diagonal, forward, diagonal * 2) &&
                   BattleMapPathSmoother.IsAngleLessThan(forward, perpendicular, forward, obtuse) &&
                   BattleMapPathSmoother.IsAngleLessThan(forward, obtuse, forward, deeperObtuse) &&
                   !BattleMapPathSmoother.IsAngleLessThan(forward, deeperObtuse, forward, obtuse);
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

        private static bool HasReachableSegments(BattleMapPathFinder pathFinder, List<GridPos> path)
        {
            for (var i = path.Count - 1; i > 0; i--)
            {
                if (!pathFinder.IsStraightPathReachable(path[i], path[i - 1]))
                    return false;
            }

            return true;
        }

        private static bool HasAllTurnsSmallerThanOriginal(
            FixedDir incomingDirection,
            List<GridPos> path,
            GridPos originalNext)
        {
            var previousDirection = incomingDirection.targetFixedPos - incomingDirection.currentFixedPos;
            var originalDirection = originalNext.ToFixedPos() - path[path.Count - 1].ToFixedPos();

            for (var i = path.Count - 1; i > 0; i--)
            {
                var current = path[i].ToFixedPos();
                var next = path[i - 1].ToFixedPos();
                var direction = next - current;
                if (!BattleMapPathSmoother.IsAngleLessThan(
                        previousDirection,
                        direction,
                        incomingDirection.targetFixedPos - incomingDirection.currentFixedPos,
                        originalDirection))
                {
                    return false;
                }

                previousDirection = direction;
            }

            return true;
        }

        private static bool Verify(bool result, string testName)
        {
            if (!result)
                LogHelper.Error($"[{nameof(BattleMapPathSmootherTest)}] {testName} failed.");

            return result;
        }
    }
}
