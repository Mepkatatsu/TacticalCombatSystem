using System.Collections.Generic;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TacticalPositioningTestData;

namespace Script.CommonLib.Tests
{
    public sealed class InitialTacticalFormationPlannerTest : ITest
    {
        public bool Test()
        {
            var success = true;

            success &= Verify(TestPartialCandidateFailureKeepsWholeTeamDestinations(), nameof(TestPartialCandidateFailureKeepsWholeTeamDestinations));
            success &= Verify(TestFourEntityCandidateFailureKeepsWholeTeamDestinations(), nameof(TestFourEntityCandidateFailureKeepsWholeTeamDestinations));
            success &= Verify(TestPlacementOrderIsDeterministicWhenInputOrderChanges(), nameof(TestPlacementOrderIsDeterministicWhenInputOrderChanges));
            success &= Verify(TestOpenMapPlacementMatchesCurrentSafeAttackPolicy(), nameof(TestOpenMapPlacementMatchesCurrentSafeAttackPolicy));
            success &= Verify(TestOpenMapFourEntityPlacementMatchesCurrentSpacingPolicy(), nameof(TestOpenMapFourEntityPlacementMatchesCurrentSpacingPolicy));
            success &= Verify(TestPlacementPreservesCurrentLateralSide(), nameof(TestPlacementPreservesCurrentLateralSide));
            success &= Verify(TestPlacementPreservesRelativeLateralOrder(), nameof(TestPlacementPreservesRelativeLateralOrder));
            success &= Verify(TestRedPlacementPreservesCurrentLateralSide(), nameof(TestRedPlacementPreservesCurrentLateralSide));
            success &= Verify(TestDiagonalPlacementPreservesCurrentLateralSide(), nameof(TestDiagonalPlacementPreservesCurrentLateralSide));
            success &= Verify(TestBlueAndRedPlacementIsSymmetric(), nameof(TestBlueAndRedPlacementIsSymmetric));
            return success;
        }

        private static bool TestPartialCandidateFailureKeepsWholeTeamDestinations()
        {
            var mapData = CreateMapData();
            mapData.obstacles.Add(new ObstacleData
            {
                blockedPoints = new List<GridPos> { new(-6, 4) },
                waypoints = new List<GridPos>(),
            });
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueEntities = new List<Entity> { entities[0], entities[1], entities[2] };
            var redEntities = new List<Entity> { entities[3], entities[4], entities[5] };
            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));

            planner.TryApply(blueEntities, redEntities);

            return blueEntities[1].GetDestinationForTest().X == 20000 &&
                   blueEntities[2].GetDestinationForTest().X == 20000;
        }

        private static bool TestFourEntityCandidateFailureKeepsWholeTeamDestinations()
        {
            var mapData = CreateFourEntityTeamMapData();
            mapData.minGridPos = new GridPos(-30, -4);
            mapData.maxGridPos = new GridPos(30, 4);
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueEntities = new List<Entity> { entities[0], entities[1], entities[2], entities[3] };
            var redEntities = new List<Entity> { entities[4], entities[5], entities[6], entities[7] };
            var authoredDestinations = GetDestinationsById(simulator.GetAliveEntities());
            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));

            planner.TryApply(blueEntities, redEntities);

            return HaveSameDestinations(
                authoredDestinations,
                GetDestinationsById(simulator.GetAliveEntities()));
        }

        private static bool TestPlacementOrderIsDeterministicWhenInputOrderChanges()
        {
            var firstMapData = CreateMapData();
            var secondMapData = CreateMapData();
            var firstSimulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, firstMapData);
            var secondSimulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, secondMapData);
            firstSimulator.Init();
            secondSimulator.Init();

            var firstEntities = GetEntities(firstSimulator.GetAliveEntities());
            var secondEntities = GetEntities(secondSimulator.GetAliveEntities());
            var firstPlanner = new InitialTacticalFormationPlanner(firstMapData, new BattleMapPathFinder(firstMapData));
            var secondPlanner = new InitialTacticalFormationPlanner(secondMapData, new BattleMapPathFinder(secondMapData));

            if (!firstPlanner.TryApply(
                    new List<Entity> { firstEntities[0], firstEntities[1], firstEntities[2] },
                    new List<Entity> { firstEntities[3], firstEntities[4], firstEntities[5] }) ||
                !secondPlanner.TryApply(
                    new List<Entity> { secondEntities[2], secondEntities[0], secondEntities[1] },
                    new List<Entity> { secondEntities[5], secondEntities[3], secondEntities[4] }))
            {
                return false;
            }

            return HaveSameDestinations(
                GetDestinationsById(firstSimulator.GetAliveEntities()),
                GetDestinationsById(secondSimulator.GetAliveEntities()));
        }

        private static bool TestOpenMapPlacementMatchesCurrentSafeAttackPolicy()
        {
            var mapData = CreateMapData();
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var predictor = new FrontlineEncounterPredictor(mapData);

            if (!predictor.TryPredict(
                    entities[0],
                    entities[3],
                    out _,
                    out var redFrontlinePosition))
            {
                return false;
            }

            var blueFirstAuthoredDestination = entities[1].GetDestinationForTest();
            var blueSecondAuthoredDestination = entities[2].GetDestinationForTest();
            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));
            if (!planner.TryApply(
                    new List<Entity> { entities[0], entities[1], entities[2] },
                    new List<Entity> { entities[3], entities[4], entities[5] }))
            {
                return false;
            }

            var safeAttackRange = entities[1].AttackRange * 90 / 100;
            return entities[1].GetDestinationForTest() != blueFirstAuthoredDestination &&
                   entities[2].GetDestinationForTest() != blueSecondAuthoredDestination &&
                   entities[1].GetDestinationForTest().GetDistance(redFrontlinePosition) <= safeAttackRange &&
                   entities[2].GetDestinationForTest().GetDistance(redFrontlinePosition) <= safeAttackRange;
        }

        private static bool TestOpenMapFourEntityPlacementMatchesCurrentSpacingPolicy()
        {
            var mapData = CreateFourEntityTeamMapData();
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueEntities = new List<Entity> { entities[0], entities[1], entities[2], entities[3] };
            var redEntities = new List<Entity> { entities[4], entities[5], entities[6], entities[7] };
            var predictor = new FrontlineEncounterPredictor(mapData);
            if (!predictor.TryPredict(
                    blueEntities[0],
                    redEntities[0],
                    out var blueFrontlinePosition,
                    out var redFrontlinePosition))
            {
                return false;
            }

            var authoredDestinations = GetDestinationsById(simulator.GetAliveEntities());
            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));
            if (!planner.TryApply(blueEntities, redEntities))
                return false;

            for (var i = 1; i < 4; i++)
            {
                if (blueEntities[i].GetDestinationForTest() == authoredDestinations[blueEntities[i].Id] ||
                    redEntities[i].GetDestinationForTest() == authoredDestinations[redEntities[i].Id])
                {
                    return false;
                }
            }

            var bluePositions = new List<FixedPos> { blueFrontlinePosition };
            var redPositions = new List<FixedPos> { redFrontlinePosition };
            for (var i = 1; i < 4; i++)
            {
                bluePositions.Add(blueEntities[i].GetDestinationForTest());
                redPositions.Add(redEntities[i].GetDestinationForTest());
            }

            return HasMinimumSpacing(bluePositions, 5500) &&
                   HasMinimumSpacing(redPositions, 5500);
        }

        private static bool TestBlueAndRedPlacementIsSymmetric()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var entities = GetEntities(simulator.GetAliveEntities());

            var blueFirst = entities[1].GetDestinationForTest();
            var blueSecond = entities[2].GetDestinationForTest();
            var redFirst = entities[4].GetDestinationForTest();
            var redSecond = entities[5].GetDestinationForTest();

            return blueFirst.X == -redFirst.X &&
                   blueSecond.X == -redSecond.X &&
                   blueFirst.Z == redFirst.Z &&
                   blueSecond.Z == redSecond.Z;
        }

        private static bool TestPlacementPreservesCurrentLateralSide()
        {
            return TestPlacementPreservesCurrentLateralSide(TeamFlag.Blue);
        }

        private static bool TestPlacementPreservesRelativeLateralOrder()
        {
            var mapData = CreateMapData();
            mapData.battlePositions[1].gridPos = new GridPos(-6, -4);
            mapData.battlePositions[2].gridPos = new GridPos(-6, 0);
            mapData.battlePositions[4].gridPos = new GridPos(6, -4);
            mapData.battlePositions[5].gridPos = new GridPos(6, 0);
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));

            if (!planner.TryApply(
                    new List<Entity> { entities[0], entities[1], entities[2] },
                    new List<Entity> { entities[3], entities[4], entities[5] }))
            {
                return false;
            }

            return entities[1].GetDestinationForTest().Z < entities[2].GetDestinationForTest().Z &&
                   entities[4].GetDestinationForTest().Z < entities[5].GetDestinationForTest().Z;
        }

        private static bool TestRedPlacementPreservesCurrentLateralSide()
        {
            return TestPlacementPreservesCurrentLateralSide(TeamFlag.Red);
        }

        private static bool TestPlacementPreservesCurrentLateralSide(TeamFlag teamFlag)
        {
            var mapData = CreateMapData();
            var firstIndex = teamFlag == TeamFlag.Blue ? 1 : 4;
            var secondIndex = firstIndex + 1;
            var startX = teamFlag == TeamFlag.Blue ? -6 : 6;
            mapData.battlePositions[firstIndex].gridPos = new GridPos(startX, 4);
            mapData.battlePositions[secondIndex].gridPos = new GridPos(startX, -4);
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var upperStart = entities[firstIndex].GetPos();
            var lowerStart = entities[secondIndex].GetPos();
            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));

            if (!planner.TryApply(
                    new List<Entity> { entities[0], entities[1], entities[2] },
                    new List<Entity> { entities[3], entities[4], entities[5] }))
            {
                return false;
            }

            return upperStart.Z > 0 &&
                   lowerStart.Z < 0 &&
                   entities[firstIndex].GetDestinationForTest().Z > 0 &&
                   entities[secondIndex].GetDestinationForTest().Z < 0;
        }

        private static bool TestDiagonalPlacementPreservesCurrentLateralSide()
        {
            var mapData = CreateMapData();
            mapData.battlePositions[0].gridPos = new GridPos(-8, -2);
            mapData.battlePositions[1].gridPos = new GridPos(-10, 4);
            mapData.battlePositions[2].gridPos = new GridPos(-6, -8);
            mapData.battlePositions[3].gridPos = new GridPos(8, 2);
            mapData.battlePositions[4].gridPos = new GridPos(10, -4);
            mapData.battlePositions[5].gridPos = new GridPos(6, 8);
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var starts = new FixedPos[entities.Count];
            for (var i = 0; i < entities.Count; i++)
            {
                starts[i] = entities[i].GetPos();
            }

            var predictor = new FrontlineEncounterPredictor(mapData);
            if (!predictor.TryPredict(
                    entities[0],
                    entities[3],
                    out var blueFrontline,
                    out var redFrontline))
            {
                return false;
            }

            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));
            if (!planner.TryApply(
                    new List<Entity> { entities[0], entities[1], entities[2] },
                    new List<Entity> { entities[3], entities[4], entities[5] }))
            {
                return false;
            }

            return HasSameLateralSide(starts[1], entities[1].GetDestinationForTest(), blueFrontline, redFrontline) &&
                   HasSameLateralSide(starts[2], entities[2].GetDestinationForTest(), blueFrontline, redFrontline) &&
                   HasSameLateralSide(starts[4], entities[4].GetDestinationForTest(), redFrontline, blueFrontline) &&
                   HasSameLateralSide(starts[5], entities[5].GetDestinationForTest(), redFrontline, blueFrontline);
        }

        private static bool HasSameLateralSide(
            FixedPos start,
            FixedPos destination,
            FixedPos frontline,
            FixedPos opposingFrontline)
        {
            var axis = opposingFrontline - frontline;
            var startDelta = start - frontline;
            var destinationDelta = destination - frontline;
            var startProjection = -startDelta.X * axis.Z + startDelta.Z * axis.X;
            var destinationProjection = -destinationDelta.X * axis.Z + destinationDelta.Z * axis.X;
            return startProjection != 0 &&
                   destinationProjection != 0 &&
                   (startProjection > 0) == (destinationProjection > 0);
        }

        private static bool HasMinimumSpacing(List<FixedPos> positions, long minimumSpacing)
        {
            for (var i = 0; i < positions.Count; i++)
            {
                for (var j = i + 1; j < positions.Count; j++)
                {
                    if (positions[i].GetDistance(positions[j]) < minimumSpacing)
                        return false;
                }
            }

            return true;
        }

        private static bool Verify(bool result, string testName)
        {
            if (!result)
                LogHelper.Error($"[{nameof(InitialTacticalFormationPlannerTest)}] {testName} failed.");

            return result;
        }
    }
}
