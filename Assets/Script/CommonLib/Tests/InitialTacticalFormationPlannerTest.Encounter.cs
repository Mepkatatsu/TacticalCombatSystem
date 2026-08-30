using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    public partial class InitialTacticalFormationPlannerTest
    {
        private static bool TestInitDoesNotApplyFormation()
        {
            var simulator = CreateSimulator();
            simulator.Init();

            return !simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   HasAuthoredDestinations(simulator.GetAliveEntities());
        }

        private static bool TestEncounterDetectionMarginBoundary()
        {
            var mapData = CreateMapData();
            mapData.entities[0].attackRange = 3000;
            mapData.entities[3].attackRange = 5000;
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            entities[0].SetPos(new FixedPos(0, 0, 0));
            entities[3].SetPos(new FixedPos(20000, 0, 0));

            var blueEntities = new List<Entity> { entities[2], entities[0], entities[1] };
            var redEntities = new List<Entity> { entities[5], entities[4], entities[3] };
            var startsAtBoundary = InitialEncounterDetector.HasEncounter(blueEntities, redEntities) &&
                                   InitialEncounterDetector.HasEncounter(redEntities, blueEntities);
            entities[3].SetPos(new FixedPos(20001, 0, 0));
            return startsAtBoundary && !InitialEncounterDetector.HasEncounter(blueEntities, redEntities);
        }

        private static bool TestLongRangeBacklineDoesNotStartEncounterEarly()
        {
            var mapData = CreateMapData();
            mapData.entities[0].attackRange = 3000;
            mapData.entities[1].attackRange = 20000;
            mapData.entities[3].attackRange = 3000;
            mapData.entities[4].attackRange = 20000;
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            entities[0].SetPos(new FixedPos(0, 0, 0));
            entities[1].SetPos(new FixedPos(0, 0, 0));
            entities[3].SetPos(new FixedPos(20000, 0, 0));
            entities[4].SetPos(new FixedPos(20000, 0, 0));

            return !InitialEncounterDetector.HasEncounter(
                new List<Entity> { entities[0], entities[1], entities[2] },
                new List<Entity> { entities[3], entities[4], entities[5] });
        }

        private static bool TestFirstEncounterAppliesFormation()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   !HasAuthoredDestinations(simulator.GetAliveEntities());
        }

        private static bool TestFormationIsNotReappliedAfterFirstEncounter()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var entities = GetEntities(simulator.GetAliveEntities());
            Entity plannedEntity = null;
            for (var i = 0; i < entities.Count; i++)
            {
                if (!entities[i].ShouldPrioritizeMovement)
                    continue;

                plannedEntity = entities[i];
                break;
            }

            if (plannedEntity == null)
                return false;

            // 첫 배치 결과를 일부러 덮어쓴 뒤 같은 교전 조건에서 다시 갱신한다.
            // 초기 배치가 재실행된다면 이 목적지가 전술 목적지로 다시 바뀐다.
            var overrideDestination = plannedEntity.GetPos();
            plannedEntity.SetDestination(overrideDestination);
            simulator.Update(0);

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   plannedEntity.GetDestinationForTest() == overrideDestination;
        }

        private static bool TestUnequalRangePredictionMatchesFixedTickReference()
        {
            var mapData = CreateMapData();
            mapData.entities = new List<EntityData>
            {
                CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 10000),
                CreateEntityData(TeamFlag.Red, "RedStart1", "RedEnd1", 3000),
            };
            mapData.battlePositions[0].gridPos = new GridPos(-10, 0);
            mapData.battlePositions[3].gridPos = new GridPos(10, 0);

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueStart = entities[0].GetPos();
            var redStart = entities[1].GetPos();
            var predictor = new FrontlineEncounterPredictor(mapData);

            if (!predictor.TryPredict(
                    entities[0],
                    entities[1],
                    out var bluePosition,
                    out var redPosition))
            {
                return false;
            }

            var blueMoveDistance = blueStart.GetDistance(bluePosition);
            var redMoveDistance = redStart.GetDistance(redPosition);
            // Blue는 10m 탐지 tick에서 한 번 더 0.25m 이동해 -4.75m, Red는 이후 3m 탐지 tick에서 -2m에 멈춘다.
            var expectedBlueStopGridPos = new GridPos(-5, 0);
            var expectedRedStopGridPos = new GridPos(-2, 0);
            return bluePosition.ToGridPos() == expectedBlueStopGridPos &&
                   redPosition.ToGridPos() == expectedRedStopGridPos &&
                   blueMoveDistance < redMoveDistance &&
                   bluePosition.GetDistance(redPosition) <= entities[1].AttackRange;
        }

        private static bool TestPredictionFailureKeepsAuthoredDestinations()
        {
            var mapData = CreateMapData();
            for (var i = 0; i < mapData.entities.Count; i++)
            {
                mapData.entities[i].moveSpeed = 0;
            }

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            entities[0].SetPos(new GridPos(-3, 0));
            entities[3].SetPos(new GridPos(3, 0));
            simulator.Update(50);

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   HasAuthoredDestinations(simulator.GetAliveEntities());
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
    }
}
