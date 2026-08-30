using System.Collections.Generic;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TacticalPositioningTestData;

namespace Script.CommonLib.Tests
{
    public sealed class BattleMapPathFinderTest : ITest
    {
        public bool Test()
        {
            var success = true;

            success &= Verify<BattleMapPathFinderTest>(
                TestArbitraryGoalDetourPreservesAuthoredQueryResult(),
                nameof(TestArbitraryGoalDetourPreservesAuthoredQueryResult));
            success &= Verify<BattleMapPathFinderTest>(
                TestFailedPathReturnsEmptyResult(),
                nameof(TestFailedPathReturnsEmptyResult));
            return success;
        }

        private static bool TestArbitraryGoalDetourPreservesAuthoredQueryResult()
        {
            var mapData = CreateMapData();
            mapData.obstacles.Add(CreateCenterObstacle());
            var pathFinder = new BattleMapPathFinder(mapData);
            var authoredPathBefore = new List<GridPos>();
            var arbitraryPath = new List<GridPos>();
            var authoredPathAfter = new List<GridPos>();

            if (!pathFinder.TryFindWaypoints(
                    new GridPos(-6, 0),
                    new GridPos(6, 0),
                    authoredPathBefore))
            {
                return false;
            }

            if (!pathFinder.TryFindWaypointsFromArbitraryPositions(
                    new GridPos(-20, 0),
                    new GridPos(20, 0),
                    arbitraryPath))
            {
                return false;
            }

            if (!pathFinder.TryFindWaypoints(
                    new GridPos(-6, 0),
                    new GridPos(6, 0),
                    authoredPathAfter))
            {
                return false;
            }

            if (arbitraryPath.Count <= 2 ||
                arbitraryPath[0] != new GridPos(20, 0) ||
                arbitraryPath[arbitraryPath.Count - 1] != new GridPos(-20, 0) ||
                authoredPathBefore.Count != authoredPathAfter.Count)
            {
                return false;
            }

            for (var i = 0; i < authoredPathBefore.Count; i++)
            {
                if (authoredPathBefore[i] != authoredPathAfter[i])
                    return false;
            }

            return true;
        }

        private static bool TestFailedPathReturnsEmptyResult()
        {
            var mapData = CreateMapData();
            var blockedPoints = new List<GridPos>();
            for (var y = mapData.minGridPos.y; y <= mapData.maxGridPos.y; y++)
                blockedPoints.Add(new GridPos(0, y));

            mapData.obstacles.Add(new ObstacleData
            {
                blockedPoints = blockedPoints,
                waypoints = new List<GridPos>(),
            });

            var pathFinder = new BattleMapPathFinder(mapData);
            var result = new List<GridPos> { new(999, 999) };
            var found = pathFinder.TryFindWaypoints(new GridPos(-6, 0), new GridPos(6, 0), result);
            return !found && result.Count == 0;
        }

    }
}
