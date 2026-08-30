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

            success &= Verify(TestArbitraryGoalUsesAuthoredWaypointDetour(), nameof(TestArbitraryGoalUsesAuthoredWaypointDetour));
            success &= Verify(TestExistingFindWaypointsResultIsPreserved(), nameof(TestExistingFindWaypointsResultIsPreserved));
            success &= Verify(TestFailedPathReturnsEmptyResult(), nameof(TestFailedPathReturnsEmptyResult));
            return success;
        }

        private static bool TestArbitraryGoalUsesAuthoredWaypointDetour()
        {
            var mapData = CreateMapData();
            mapData.obstacles.Add(CreateCenterObstacle());
            var pathFinder = new BattleMapPathFinder(mapData);
            var paths = new List<GridPos>();

            var found = pathFinder.TryFindWaypointsFromArbitraryPositions(
                new GridPos(-20, 0),
                new GridPos(20, 0),
                paths);

            return found &&
                   paths.Count > 2 &&
                   paths[0] == new GridPos(20, 0) &&
                   paths[paths.Count - 1] == new GridPos(-20, 0);
        }

        private static bool TestExistingFindWaypointsResultIsPreserved()
        {
            var mapData = CreateMapData();
            mapData.obstacles.Add(CreateCenterObstacle());
            var pathFinder = new BattleMapPathFinder(mapData);
            var before = new List<GridPos>();
            var after = new List<GridPos>();

            if (!pathFinder.TryFindWaypoints(new GridPos(-6, 0), new GridPos(6, 0), before))
                return false;

            var arbitraryPath = new List<GridPos>();
            pathFinder.TryFindWaypointsFromArbitraryPositions(
                new GridPos(-20, 0),
                new GridPos(20, 0),
                arbitraryPath);

            if (!pathFinder.TryFindWaypoints(new GridPos(-6, 0), new GridPos(6, 0), after))
                return false;

            if (before.Count != after.Count)
                return false;

            for (var i = 0; i < before.Count; i++)
            {
                if (before[i] != after[i])
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

        private static bool Verify(bool result, string testName)
        {
            if (!result)
                LogHelper.Error($"[{nameof(BattleMapPathFinderTest)}] {testName} failed.");

            return result;
        }
    }
}
