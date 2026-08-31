using System.Collections.Generic;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TestResultVerifier;

namespace Script.CommonLib.Tests
{
    public sealed class BattleMapPathFinderTest : ITest
    {
        public bool Test()
        {
            var success = true;

            success &= Verify<BattleMapPathFinderTest>(TestArbitraryGoalDetourPreservesAuthoredQueryResult(),
                nameof(TestArbitraryGoalDetourPreservesAuthoredQueryResult));
            success &= Verify<BattleMapPathFinderTest>(TestFailedPathReturnsEmptyResult(),
                nameof(TestFailedPathReturnsEmptyResult));
            return success;
        }

        private static bool TestArbitraryGoalDetourPreservesAuthoredQueryResult()
        {
            var mapData = CreatePathfindingMapData();
            mapData.obstacles.Add(CreateCenterObstacle());
            var pathFinder = new BattleMapPathFinder(mapData);
            var authoredPathBefore = new List<GridPos>();
            var arbitraryPath = new List<GridPos>();
            var authoredPathAfter = new List<GridPos>();

            if (!pathFinder.TryFindWaypoints(new GridPos(-6, 0), new GridPos(6, 0), authoredPathBefore))
                return false;

            if (!pathFinder.TryFindWaypointsFromArbitraryPositions(
                    new GridPos(-20, 0), new GridPos(20, 0), arbitraryPath))
                return false;

            if (!pathFinder.TryFindWaypoints(new GridPos(-6, 0), new GridPos(6, 0), authoredPathAfter))
                return false;

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
            var mapData = CreatePathfindingMapData();
            var blockedPoints = new List<GridPos>();
            for (var y = mapData.minGridPos.y; y <= mapData.maxGridPos.y; y++)
                blockedPoints.Add(new GridPos(0, y));

            mapData.obstacles.Add(new ObstacleData { blockedPoints = blockedPoints, waypoints = new List<GridPos>() });

            var pathFinder = new BattleMapPathFinder(mapData);
            var result = new List<GridPos> { new(999, 999) };
            var found = pathFinder.TryFindWaypoints(new GridPos(-6, 0), new GridPos(6, 0), result);
            return !found && result.Count == 0;
        }

        private static BattleMapData CreatePathfindingMapData()
        {
            return new BattleMapData
            {
                minGridPos = new GridPos(-30, -15),
                maxGridPos = new GridPos(30, 15),
                battlePositions = new List<BattlePositionData>
                {
                    new()
                    {
                        name = "LeftWaypoint",
                        gridPos = new GridPos(-6, 0),
                        positionType = BattlePositionData.PositionType.Waypoint,
                    },
                    new()
                    {
                        name = "RightWaypoint",
                        gridPos = new GridPos(6, 0),
                        positionType = BattlePositionData.PositionType.Waypoint,
                    },
                },
            };
        }

        private static ObstacleData CreateCenterObstacle()
        {
            return new ObstacleData
            {
                blockedPoints = new List<GridPos> { new(0, -1), new(0, 0), new(0, 1) },
                waypoints = new List<GridPos> { new(-6, -3), new(6, -3), new(-6, 3), new(6, 3) },
            };
        }
    }
}
