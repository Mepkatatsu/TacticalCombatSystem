using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    public class InitialTacticalFormationPlannerTest : ITest
    {
        public bool Test()
        {
            var success = true;
            success &= Verify(TestInitDoesNotApplyFormation(), nameof(TestInitDoesNotApplyFormation));
            success &= Verify(TestFirstEncounterAppliesFormationOnce(), nameof(TestFirstEncounterAppliesFormationOnce));
            success &= Verify(TestPredictionKeepsAttackPriorityForUnequalRanges(), nameof(TestPredictionKeepsAttackPriorityForUnequalRanges));
            success &= Verify(TestPlannedEntityMovesEvenWhenEnemyIsInRange(), nameof(TestPlannedEntityMovesEvenWhenEnemyIsInRange));
            success &= Verify(TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken(), nameof(TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken));
            success &= Verify(TestImmobilePlannedEntityReleasesMovementPriority(), nameof(TestImmobilePlannedEntityReleasesMovementPriority));
            success &= Verify(TestSubStepTacticalMovementReleasesMovementPriority(), nameof(TestSubStepTacticalMovementReleasesMovementPriority));
            success &= Verify(TestExhaustedTacticalPathReleasesMovementPriority(), nameof(TestExhaustedTacticalPathReleasesMovementPriority));
            success &= Verify(TestUnplannedEntityKeepsAttackPriority(), nameof(TestUnplannedEntityKeepsAttackPriority));
            success &= Verify(TestRepeatedUpdateDoesNotReapplyFormation(), nameof(TestRepeatedUpdateDoesNotReapplyFormation));
            success &= Verify(TestPredictionFailureKeepsAuthoredDestinations(), nameof(TestPredictionFailureKeepsAuthoredDestinations));
            success &= Verify(TestArbitraryGoalUsesAuthoredWaypointDetour(), nameof(TestArbitraryGoalUsesAuthoredWaypointDetour));
            success &= Verify(TestPartialCandidateFailureKeepsWholeTeamDestinations(), nameof(TestPartialCandidateFailureKeepsWholeTeamDestinations));
            success &= Verify(TestDestinationsStayWithinSafeAttackRange(), nameof(TestDestinationsStayWithinSafeAttackRange));
            success &= Verify(TestBlueAndRedPlacementIsSymmetric(), nameof(TestBlueAndRedPlacementIsSymmetric));
            success &= Verify(TestPlacementOrderIsDeterministicWhenInputOrderChanges(), nameof(TestPlacementOrderIsDeterministicWhenInputOrderChanges));
            success &= Verify(TestDisabledMapKeepsAuthoredDestinations(), nameof(TestDisabledMapKeepsAuthoredDestinations));
            success &= Verify(TestExistingFindWaypointsResultIsPreserved(), nameof(TestExistingFindWaypointsResultIsPreserved));
            success &= Verify(TestTest001RuntimeSimulationAppliesFormation(), nameof(TestTest001RuntimeSimulationAppliesFormation));
            return success;
        }

        private static bool Verify(bool result, string testName)
        {
            if (!result)
                LogHelper.Error($"[InitialTacticalFormationPlannerTest] {testName} failed.");

            return result;
        }

        private static bool TestInitDoesNotApplyFormation()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();

            return !simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   HasAuthoredDestinations(simulator.GetAliveEntities());
        }

        private static bool TestFirstEncounterAppliesFormationOnce()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();
            simulator.Update(50);

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   !HasAuthoredDestinations(simulator.GetAliveEntities());
        }

        private static bool TestRepeatedUpdateDoesNotReapplyFormation()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();
            simulator.Update(50);
            var firstDestinations = GetDestinationsById(simulator.GetAliveEntities());

            for (var i = 0; i < 20; i++)
            {
                simulator.Update(50);
            }

            var laterDestinations = GetDestinationsById(simulator.GetAliveEntities());
            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   HaveSameDestinations(firstDestinations, laterDestinations);
        }

        private static bool TestPlannedEntityMovesEvenWhenEnemyIsInRange()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var initialBlueRangedPosition = entities[1].GetPos();
            var initialRedRangedPosition = entities[4].GetPos();

            simulator.Update(50);
            simulator.Update(50);

            return entities[1].GetPos() != initialBlueRangedPosition &&
                   entities[4].GetPos() != initialRedRangedPosition &&
                   entities[1].ShouldPrioritizeMovement &&
                   entities[4].ShouldPrioritizeMovement;
        }

        private static bool TestPredictionKeepsAttackPriorityForUnequalRanges()
        {
            var mapData = CreateMapData(false);
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

        private static bool TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();
            simulator.Update(50);
            simulator.Update(50);
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueRanged = entities[1];
            var destination = blueRanged.GetDestinationForTest();
            var currentTargetId = blueRanged.GetMainTargetIdForTest();
            if (!currentTargetId.HasValue)
                return false;

            Entity currentTarget = null;
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].Id == currentTargetId.Value)
                {
                    currentTarget = entities[i];
                    break;
                }
            }

            if (currentTarget == null)
                return false;

            currentTarget.Hit(currentTarget.MaxHp);
            blueRanged.Hit(1);

            for (var i = 0; i < 500 && blueRanged.ShouldPrioritizeMovement; i++)
            {
                simulator.Update(50);
            }

            return blueRanged.IsAlive() &&
                   !currentTarget.IsAlive() &&
                   !blueRanged.ShouldPrioritizeMovement &&
                   blueRanged.GetPos() == destination;
        }

        private static bool TestUnplannedEntityKeepsAttackPriority()
        {
            var simulator = CreateSimulator(false);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueRanged = entities[1];

            for (var i = 0; i < 30; i++)
            {
                simulator.Update(50);
            }

            return !blueRanged.ShouldPrioritizeMovement &&
                   blueRanged.CurrentStateType == Script.CommonLib.Battle.EntityStateType.Attack;
        }

        private static bool TestImmobilePlannedEntityReleasesMovementPriority()
        {
            var mapData = CreateMapData(true);
            mapData.entities[1].moveSpeed = 0;
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            simulator.Update(50);
            var blueRanged = (Entity)simulator.GetAliveEntities()[1];

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   !blueRanged.ShouldPrioritizeMovement;
        }

        private static bool TestSubStepTacticalMovementReleasesMovementPriority()
        {
            var mapData = CreateMapData(true);
            mapData.entities[1].moveSpeed = 1;
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            simulator.Update(50);
            var blueRanged = (Entity)simulator.GetAliveEntities()[1];

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   !blueRanged.ShouldPrioritizeMovement;
        }

        private static bool TestExhaustedTacticalPathReleasesMovementPriority()
        {
            var simulator = CreateSimulator(false);
            simulator.Init();
            var entity = (Entity)simulator.GetAliveEntities()[1];
            var currentPosition = entity.GetPos();
            var unreachableDestination = new FixedPos(
                currentPosition.X + 10000,
                currentPosition.Y,
                currentPosition.Z);
            entity.SetTacticalDestination(
                unreachableDestination,
                new List<GridPos> { currentPosition.ToGridPos() });

            simulator.Update(50);

            return entity.GetPos() == currentPosition &&
                   !entity.ShouldPrioritizeMovement;
        }

        private static bool TestPredictionFailureKeepsAuthoredDestinations()
        {
            var mapData = CreateMapData(true);
            for (var i = 0; i < mapData.entities.Count; i++)
            {
                mapData.entities[i].moveSpeed = 0;
            }

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            simulator.Update(50);

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   HasAuthoredDestinations(simulator.GetAliveEntities());
        }

        private static bool TestArbitraryGoalUsesAuthoredWaypointDetour()
        {
            var mapData = CreateMapData(false);
            mapData.obstacles.Add(CreateCenterObstacle());
            var pathFinder = new BattleMapPathFinder(mapData);
            var paths = new List<GridPos>();

            var found = pathFinder.TryFindWaypoints(new GridPos(-20, 0), new GridPos(20, 0), paths);

            return found &&
                   paths.Count > 2 &&
                   paths[0] == new GridPos(20, 0) &&
                   paths[paths.Count - 1] == new GridPos(-20, 0);
        }

        private static bool TestPartialCandidateFailureKeepsWholeTeamDestinations()
        {
            var mapData = CreateMapData(false);
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

        private static bool TestDestinationsStayWithinSafeAttackRange()
        {
            var mapData = CreateMapData(false);
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

            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));
            planner.TryApply(
                new List<Entity> { entities[0], entities[1], entities[2] },
                new List<Entity> { entities[3], entities[4], entities[5] });

            var safeAttackRange = entities[1].AttackRange * 90 / 100;
            return entities[1].GetDestinationForTest().GetDistance(redFrontlinePosition) <= safeAttackRange &&
                   entities[2].GetDestinationForTest().GetDistance(redFrontlinePosition) <= safeAttackRange;
        }

        private static bool TestBlueAndRedPlacementIsSymmetric()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();
            simulator.Update(50);
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

        private static bool TestPlacementOrderIsDeterministicWhenInputOrderChanges()
        {
            var firstMapData = CreateMapData(false);
            var secondMapData = CreateMapData(false);
            var firstSimulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, firstMapData);
            var secondSimulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, secondMapData);
            firstSimulator.Init();
            secondSimulator.Init();

            var firstEntities = GetEntities(firstSimulator.GetAliveEntities());
            var secondEntities = GetEntities(secondSimulator.GetAliveEntities());
            var firstPlanner = new InitialTacticalFormationPlanner(firstMapData, new BattleMapPathFinder(firstMapData));
            var secondPlanner = new InitialTacticalFormationPlanner(secondMapData, new BattleMapPathFinder(secondMapData));

            firstPlanner.TryApply(
                new List<Entity> { firstEntities[0], firstEntities[1], firstEntities[2] },
                new List<Entity> { firstEntities[3], firstEntities[4], firstEntities[5] });
            secondPlanner.TryApply(
                new List<Entity> { secondEntities[2], secondEntities[0], secondEntities[1] },
                new List<Entity> { secondEntities[5], secondEntities[3], secondEntities[4] });

            return HaveSameDestinations(
                GetDestinationsById(firstSimulator.GetAliveEntities()),
                GetDestinationsById(secondSimulator.GetAliveEntities()));
        }

        private static bool TestDisabledMapKeepsAuthoredDestinations()
        {
            var simulator = CreateSimulator(false);
            simulator.Init();

            for (var i = 0; i < 200; i++)
            {
                simulator.Update(50);
            }

            return !simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   HasAuthoredDestinations(simulator.GetAliveEntities());
        }

        private static bool TestExistingFindWaypointsResultIsPreserved()
        {
            var mapData = CreateMapData(false);
            mapData.obstacles.Add(CreateCenterObstacle());
            var pathFinder = new BattleMapPathFinder(mapData);
            var before = new List<GridPos>();
            var after = new List<GridPos>();

            pathFinder.FindWaypoints(new GridPos(-6, 0), new GridPos(6, 0), before);
            var transientPath = new List<GridPos>();
            pathFinder.TryFindWaypoints(new GridPos(-20, 0), new GridPos(20, 0), transientPath);
            pathFinder.FindWaypoints(new GridPos(-6, 0), new GridPos(6, 0), after);

            if (before.Count != after.Count)
                return false;

            for (var i = 0; i < before.Count; i++)
            {
                if (before[i] != after[i])
                    return false;
            }

            return true;
        }

        private static bool TestTest001RuntimeSimulationAppliesFormation()
        {
            var json = File.ReadAllText("Assets/Data/MapData/TEST-001-NORMAL_Data.json");
            var mapData = JsonConvert.DeserializeObject<BattleMapData>(json);
            if (mapData == null || !mapData.useInitialTacticalPositioning)
                return false;

            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            var authoredDestinations = GetDestinationsById(simulator.GetAliveEntities());

            for (var i = 0; i < 2000 && !simulator.WasInitialTacticalPositioningAttemptedForTest; i++)
            {
                simulator.Update(50);
            }

            if (!simulator.WasInitialTacticalPositioningAttemptedForTest)
                return false;

            var plannedDestinations = GetDestinationsById(simulator.GetAliveEntities());
            if (HaveSameDestinations(authoredDestinations, plannedDestinations))
                return false;

            var plannedEntities = new List<Entity>();
            var positionsAfterPlanning = new Dictionary<uint, FixedPos>();
            var entities = GetEntities(simulator.GetAliveEntities());
            for (var i = 0; i < entities.Count; i++)
            {
                if (!entities[i].ShouldPrioritizeMovement)
                    continue;

                plannedEntities.Add(entities[i]);
                positionsAfterPlanning.Add(entities[i].Id, entities[i].GetPos());
            }

            if (plannedEntities.Count == 0)
                return false;

            for (var tick = 0; tick < 2000; tick++)
            {
                var hasMovingEntity = false;
                for (var i = 0; i < plannedEntities.Count; i++)
                {
                    if (plannedEntities[i].ShouldPrioritizeMovement)
                    {
                        hasMovingEntity = true;
                        break;
                    }
                }

                if (!hasMovingEntity)
                    break;

                simulator.Update(50);
            }

            for (var i = 0; i < plannedEntities.Count; i++)
            {
                var entity = plannedEntities[i];
                if (entity.ShouldPrioritizeMovement ||
                    entity.GetPos() == positionsAfterPlanning[entity.Id] ||
                    entity.GetPos() != entity.GetDestinationForTest())
                {
                    return false;
                }
            }

            return true;
        }

        private static ObstacleData CreateCenterObstacle()
        {
            return new ObstacleData
            {
                blockedPoints = new List<GridPos>
                {
                    new(0, -1),
                    new(0, 0),
                    new(0, 1),
                },
                waypoints = new List<GridPos>
                {
                    new(-6, -3),
                    new(6, -3),
                    new(-6, 3),
                    new(6, 3),
                },
            };
        }

        private static bool HasAuthoredDestinations(IReadOnlyList<IEntityContext> entityContexts)
        {
            for (var i = 0; i < entityContexts.Count; i++)
            {
                var entity = (Entity)entityContexts[i];
                var expectedX = entity.GetTeamFlag() == TeamFlag.Blue ? 20000 : -20000;
                if (entity.GetDestinationForTest().X != expectedX)
                    return false;
            }

            return true;
        }

        private static Dictionary<uint, FixedPos> GetDestinationsById(IReadOnlyList<IEntityContext> entityContexts)
        {
            var result = new Dictionary<uint, FixedPos>();

            for (var i = 0; i < entityContexts.Count; i++)
            {
                var entity = (Entity)entityContexts[i];
                result.Add(entity.Id, entity.GetDestinationForTest());
            }

            return result;
        }

        private static bool HaveSameDestinations(
            Dictionary<uint, FixedPos> first,
            Dictionary<uint, FixedPos> second)
        {
            if (first.Count != second.Count)
                return false;

            foreach (var pair in first)
            {
                if (!second.TryGetValue(pair.Key, out var destination) || destination != pair.Value)
                    return false;
            }

            return true;
        }

        private static List<Entity> GetEntities(IReadOnlyList<IEntityContext> entityContexts)
        {
            var entities = new List<Entity>();

            for (var i = 0; i < entityContexts.Count; i++)
            {
                entities.Add((Entity)entityContexts[i]);
            }

            return entities;
        }

        private static BattleMapSimulator CreateSimulator(bool useInitialTacticalPositioning)
        {
            return new BattleMapSimulator(
                NullBattleMapEventHandler.Instance,
                CreateMapData(useInitialTacticalPositioning));
        }

        private static BattleMapData CreateMapData(bool useInitialTacticalPositioning)
        {
            var battlePositions = new List<BattlePositionData>
            {
                CreateBattlePosition("BlueStart1", -6, 0),
                CreateBattlePosition("BlueStart2", -6, -4),
                CreateBattlePosition("BlueStart3", -6, 4),
                CreateBattlePosition("RedStart1", 6, 0),
                CreateBattlePosition("RedStart2", 6, -4),
                CreateBattlePosition("RedStart3", 6, 4),
                CreateBattlePosition("BlueEnd1", 20, 0),
                CreateBattlePosition("BlueEnd2", 20, -4),
                CreateBattlePosition("BlueEnd3", 20, 4),
                CreateBattlePosition("RedEnd1", -20, 0),
                CreateBattlePosition("RedEnd2", -20, -4),
                CreateBattlePosition("RedEnd3", -20, 4),
            };

            return new BattleMapData
            {
                minGridPos = new GridPos(-30, -15),
                maxGridPos = new GridPos(30, 15),
                useInitialTacticalPositioning = useInitialTacticalPositioning,
                battlePositions = battlePositions,
                obstacles = new List<ObstacleData>(),
                entities = new List<EntityData>
                {
                    CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 5000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart2", "BlueEnd2", 12000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart3", "BlueEnd3", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart1", "RedEnd1", 5000),
                    CreateEntityData(TeamFlag.Red, "RedStart2", "RedEnd2", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart3", "RedEnd3", 12000),
                },
            };
        }

        private static BattlePositionData CreateBattlePosition(string name, int x, int y)
        {
            return new BattlePositionData
            {
                name = name,
                gridPos = new GridPos(x, y),
                positionType = BattlePositionData.PositionType.Waypoint,
            };
        }

        private static EntityData CreateEntityData(
            TeamFlag teamFlag,
            string startPositionName,
            string endPositionName,
            ushort attackRange)
        {
            return new EntityData
            {
                teamFlag = teamFlag,
                name = startPositionName,
                startPositionName = startPositionName,
                endPositionName = endPositionName,
                maxHp = 100,
                attackDamage = 0,
                attackDelayMs = ushort.MaxValue,
                attackRange = attackRange,
                moveSpeed = 5000,
            };
        }
    }
}
