using System;
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
            success &= Verify(TestEncounterDetectionMarginBoundary(), nameof(TestEncounterDetectionMarginBoundary));
            success &= Verify(TestLongRangeBacklineDoesNotStartEncounterEarly(), nameof(TestLongRangeBacklineDoesNotStartEncounterEarly));
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
            success &= Verify(TestSmoothPathTransitionSplitsCornerDeterministically(), nameof(TestSmoothPathTransitionSplitsCornerDeterministically));
            success &= Verify(TestSmoothPathTransitionKeepsOriginalPathWhenBlendIsBlocked(), nameof(TestSmoothPathTransitionKeepsOriginalPathWhenBlendIsBlocked));
            success &= Verify(TestSmoothPathTransitionKeepsUTurns(), nameof(TestSmoothPathTransitionKeepsUTurns));
            success &= Verify(TestSmoothPathTransitionReducesInternalCorner(), nameof(TestSmoothPathTransitionReducesInternalCorner));
            success &= Verify(TestIntegerAngleOrderingBoundaries(), nameof(TestIntegerAngleOrderingBoundaries));
            success &= Verify(TestEntityAppliesSmoothingToTacticalDestination(), nameof(TestEntityAppliesSmoothingToTacticalDestination));
            success &= Verify(TestPartialCandidateFailureKeepsWholeTeamDestinations(), nameof(TestPartialCandidateFailureKeepsWholeTeamDestinations));
            success &= Verify(TestDestinationsStayWithinSafeAttackRange(), nameof(TestDestinationsStayWithinSafeAttackRange));
            success &= Verify(TestFourEntityTeamKeepsMinimumSpacing(), nameof(TestFourEntityTeamKeepsMinimumSpacing));
            success &= Verify(TestFourEntityCandidateFailureKeepsWholeTeamDestinations(), nameof(TestFourEntityCandidateFailureKeepsWholeTeamDestinations));
            success &= Verify(TestPlacementPreservesCurrentLateralSide(), nameof(TestPlacementPreservesCurrentLateralSide));
            success &= Verify(TestPlacementPreservesRelativeLateralOrder(), nameof(TestPlacementPreservesRelativeLateralOrder));
            success &= Verify(TestRedPlacementPreservesCurrentLateralSide(), nameof(TestRedPlacementPreservesCurrentLateralSide));
            success &= Verify(TestDiagonalPlacementPreservesCurrentLateralSide(), nameof(TestDiagonalPlacementPreservesCurrentLateralSide));
            success &= Verify(TestBlueAndRedPlacementIsSymmetric(), nameof(TestBlueAndRedPlacementIsSymmetric));
            success &= Verify(TestPlacementOrderIsDeterministicWhenInputOrderChanges(), nameof(TestPlacementOrderIsDeterministicWhenInputOrderChanges));
            success &= Verify(TestEntityResumesAuthoredDestinationAfterExecutedAttack(), nameof(TestEntityResumesAuthoredDestinationAfterExecutedAttack));
            success &= Verify(TestEntityAppliesSmoothingToAuthoredDestinationResume(), nameof(TestEntityAppliesSmoothingToAuthoredDestinationResume));
            success &= Verify(TestEntityDoesNotResumeBeforeExecutingAttack(), nameof(TestEntityDoesNotResumeBeforeExecutingAttack));
            success &= Verify(TestFailedAuthoredDestinationResumeIsAttemptedOnce(), nameof(TestFailedAuthoredDestinationResumeIsAttemptedOnce));
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

        private static bool TestEncounterDetectionMarginBoundary()
        {
            var mapData = CreateMapData(false);
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
            var mapData = CreateMapData(false);
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

        private static bool TestFirstEncounterAppliesFormationOnce()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();
            AdvanceUntilFormationAttempted(simulator);

            return simulator.WasInitialTacticalPositioningAttemptedForTest &&
                   !HasAuthoredDestinations(simulator.GetAliveEntities());
        }

        private static bool TestRepeatedUpdateDoesNotReapplyFormation()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();
            AdvanceUntilFormationAttempted(simulator);
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

            AdvanceUntilFormationAttempted(simulator);

            return HasEntityPrioritizingMovementInAttackRange(entities, TeamFlag.Blue) &&
                   HasEntityPrioritizingMovementInAttackRange(entities, TeamFlag.Red);
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
            AdvanceUntilFormationAttempted(simulator);
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
            return TestInvalidMovementReleasesPriority(0);
        }

        private static bool TestSubStepTacticalMovementReleasesMovementPriority()
        {
            return TestInvalidMovementReleasesPriority(1);
        }

        private static bool TestInvalidMovementReleasesPriority(ushort moveSpeed)
        {
            var mapData = CreateMapData(true);
            mapData.entities[1].moveSpeed = moveSpeed;
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            AdvanceUntilFormationAttempted(simulator);
            var blueRanged = (Entity)simulator.GetAliveEntities()[1];

            for (var i = 0; i < 20 && blueRanged.ShouldPrioritizeMovement; i++)
            {
                simulator.Update(50);
            }

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
            var entities = GetEntities(simulator.GetAliveEntities());
            entities[0].SetPos(new GridPos(-3, 0));
            entities[3].SetPos(new GridPos(3, 0));
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

        private static bool TestSmoothPathTransitionSplitsCornerDeterministically()
        {
            var mapData = CreateMapData(false);
            var pathFinder = new BattleMapPathFinder(mapData);
            var start = new GridPos(0, 0).ToFixedPos();
            var incomingDirection = new FixedDir(new GridPos(-1, 0).ToFixedPos(), start);
            var firstPath = new List<GridPos> { new(10, 10), new(0, 0) };
            var secondPath = new List<GridPos> { new(10, 10), new(0, 0) };

            pathFinder.SmoothPathTransition(start, incomingDirection, firstPath);
            pathFinder.SmoothPathTransition(start, incomingDirection, secondPath);

            return firstPath.Count >= 3 &&
                   HaveSamePath(firstPath, secondPath) &&
                   HasReachableSegments(pathFinder, firstPath) &&
                   HasAllTurnsSmallerThanOriginal(incomingDirection, firstPath, new GridPos(10, 10));
        }

        private static bool TestSmoothPathTransitionKeepsOriginalPathWhenBlendIsBlocked()
        {
            var mapData = CreateMapData(false);
            mapData.obstacles.Add(new ObstacleData
            {
                blockedPoints = new List<GridPos> { new(2, 2), new(1, 1) },
                waypoints = new List<GridPos>(),
            });
            var pathFinder = new BattleMapPathFinder(mapData);
            var start = new GridPos(0, 0).ToFixedPos();
            var path = new List<GridPos> { new(0, 10), new(0, 0) };

            pathFinder.SmoothPathTransition(
                start,
                new FixedDir(new GridPos(-1, 0).ToFixedPos(), start),
                path);

            return path.Count == 1 && path[0] == new GridPos(0, 10);
        }

        private static bool TestSmoothPathTransitionKeepsUTurns()
        {
            var pathFinder = new BattleMapPathFinder(CreateMapData(false));
            var start = new GridPos(0, 0).ToFixedPos();
            var incomingDirection = new FixedDir(new GridPos(-1, 0).ToFixedPos(), start);
            var destinations = new[] { new GridPos(-10, 0), new GridPos(-10, 1) };

            for (var i = 0; i < destinations.Length; i++)
            {
                var path = new List<GridPos> { destinations[i], new(0, 0) };
                pathFinder.SmoothPathTransition(start, incomingDirection, path);
                if (path.Count != 1 || path[0] != destinations[i])
                    return false;
            }

            return true;
        }

        private static bool TestSmoothPathTransitionReducesInternalCorner()
        {
            var pathFinder = new BattleMapPathFinder(CreateMapData(false));
            var start = new GridPos(0, 0).ToFixedPos();
            var path = new List<GridPos> { new(10, 10), new(10, 0), new(0, 0) };
            var originalIncoming = new FixedPos(10, 0, 0);
            var originalOutgoing = new FixedPos(0, 0, 10);

            pathFinder.SmoothPathTransition(
                start,
                new FixedDir(new GridPos(-1, 0).ToFixedPos(), start),
                path);

            if (path.Count < 3 || !HasReachableSegments(pathFinder, path))
                return false;

            var previousDirection = path[path.Count - 2].ToFixedPos() - path[path.Count - 1].ToFixedPos();
            for (var i = path.Count - 2; i > 0; i--)
            {
                var direction = path[i - 1].ToFixedPos() - path[i].ToFixedPos();
                if (!BattleMapPathFinder.IsAngleLessThan(
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

            return BattleMapPathFinder.IsAngleLessThan(forward, shallow, forward, diagonal) &&
                   !BattleMapPathFinder.IsAngleLessThan(forward, diagonal, forward, shallow) &&
                   !BattleMapPathFinder.IsAngleLessThan(forward, diagonal, forward, diagonal * 2) &&
                   BattleMapPathFinder.IsAngleLessThan(forward, perpendicular, forward, obtuse) &&
                   BattleMapPathFinder.IsAngleLessThan(forward, obtuse, forward, deeperObtuse) &&
                   !BattleMapPathFinder.IsAngleLessThan(forward, deeperObtuse, forward, obtuse);
        }

        private static bool TestEntityAppliesSmoothingToTacticalDestination()
        {
            var context = new ResumePathTestContext(true);
            var entity = new Entity(
                1,
                context,
                CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000));
            context.AddEntity(entity);
            entity.SetPos(new GridPos(-5, 0));
            entity.SetDestination(new GridPos(5, 0).ToFixedPos());
            context.Update(entity, 50);

            var currentGridPosition = entity.GetPos().ToGridPos();
            var tacticalPath = new List<GridPos> { new(5, 5), currentGridPosition };
            entity.SetTacticalDestination(new GridPos(5, 5).ToFixedPos(), tacticalPath);

            return context.PathSmoothingCallCount == 1 &&
                   tacticalPath.Count > 2;
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

        private static bool TestFourEntityTeamKeepsMinimumSpacing()
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

            var planner = new InitialTacticalFormationPlanner(mapData, new BattleMapPathFinder(mapData));
            if (!planner.TryApply(blueEntities, redEntities))
                return false;

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

        private static bool TestBlueAndRedPlacementIsSymmetric()
        {
            var simulator = CreateSimulator(true);
            simulator.Init();
            AdvanceUntilFormationAttempted(simulator);
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
            var mapData = CreateMapData(false);
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
            var mapData = CreateMapData(false);
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

            planner.TryApply(
                new List<Entity> { entities[0], entities[1], entities[2] },
                new List<Entity> { entities[3], entities[4], entities[5] });

            return upperStart.Z > 0 &&
                   lowerStart.Z < 0 &&
                   entities[firstIndex].GetDestinationForTest().Z > 0 &&
                   entities[secondIndex].GetDestinationForTest().Z < 0;
        }

        private static bool TestDiagonalPlacementPreservesCurrentLateralSide()
        {
            var mapData = CreateMapData(false);
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
            planner.TryApply(
                new List<Entity> { entities[0], entities[1], entities[2] },
                new List<Entity> { entities[3], entities[4], entities[5] });

            return HasSameLateralSide(starts[1], entities[1].GetDestinationForTest(), blueFrontline, redFrontline) &&
                   HasSameLateralSide(starts[2], entities[2].GetDestinationForTest(), blueFrontline, redFrontline) &&
                   HasSameLateralSide(starts[4], entities[4].GetDestinationForTest(), redFrontline, blueFrontline) &&
                   HasSameLateralSide(starts[5], entities[5].GetDestinationForTest(), redFrontline, blueFrontline);
        }

        private static bool TestEntityResumesAuthoredDestinationAfterExecutedAttack()
        {
            var context = new ResumePathTestContext(true);
            var attackerData = CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000);
            attackerData.attackDelayMs = 0;
            var enemyData = CreateEntityData(TeamFlag.Red, string.Empty, string.Empty, 2000);
            var attacker = new Entity(1, context, attackerData);
            var enemy = new Entity(2, context, enemyData);
            context.AddEntity(attacker);
            context.AddEntity(enemy);
            attacker.SetPos(new GridPos(-1, 0));
            var authoredDestination = new GridPos(10, 0).ToFixedPos();
            attacker.SetDestination(authoredDestination);
            attacker.SetTacticalDestination(
                new GridPos(0, 0).ToFixedPos(),
                new List<GridPos> { new(0, 0) });
            enemy.SetPos(new GridPos(1, 0));
            enemy.SetDestination(enemy.GetPos());

            for (var i = 0; i < 20 && context.AttackRequestCount == 0; i++)
            {
                context.Update(attacker, 50);
            }

            if (context.AttackRequestCount == 0)
                return false;

            var tacticalPosition = attacker.GetPos();
            enemy.SetPos(new GridPos(8, 0));
            for (var i = 0; i < 20 && attacker.GetPos() == tacticalPosition; i++)
            {
                context.Update(attacker, 50);
            }

            return context.ResumePathRequestCount == 1 &&
                   attacker.GetDestinationForTest() == authoredDestination &&
                   attacker.GetPos() != tacticalPosition;
        }

        private static bool TestEntityAppliesSmoothingToAuthoredDestinationResume()
        {
            var context = new ResumePathTestContext(true);
            var attackerData = CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000);
            attackerData.attackDelayMs = 0;
            var attacker = new Entity(1, context, attackerData);
            var enemy = new Entity(
                2,
                context,
                CreateEntityData(TeamFlag.Red, string.Empty, string.Empty, 2000));
            context.AddEntity(attacker);
            context.AddEntity(enemy);
            attacker.SetPos(new GridPos(-1, 0));
            attacker.SetDestination(new GridPos(10, 10).ToFixedPos());
            attacker.SetTacticalDestination(
                new GridPos(0, 0).ToFixedPos(),
                new List<GridPos> { new(0, 0) });
            enemy.SetPos(new GridPos(1, 0));
            enemy.SetDestination(enemy.GetPos());

            for (var i = 0; i < 20 && context.AttackRequestCount == 0; i++)
                context.Update(attacker, 50);

            if (context.AttackRequestCount == 0)
                return false;

            enemy.SetPos(new GridPos(20, 20));
            for (var i = 0; i < 20 && context.ResumePathRequestCount == 0; i++)
                context.Update(attacker, 50);

            return context.ResumePathRequestCount == 1 &&
                   context.PathSmoothingCallCount == 1 &&
                   context.LastSmoothedWaypointCount > 2;
        }

        private static bool TestEntityDoesNotResumeBeforeExecutingAttack()
        {
            var mapData = CreateMapData(true);
            var eventHandler = new AttackRecordingEventHandler();
            var simulator = new BattleMapSimulator(eventHandler, mapData);
            simulator.Init();
            AdvanceUntilFormationAttempted(simulator);
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueRanged = entities[1];

            for (var i = 0; i < 500 && blueRanged.ShouldPrioritizeMovement; i++)
            {
                simulator.Update(50);
            }

            for (var i = 0; i < 20 && !blueRanged.IsMainTargetInRange(); i++)
            {
                simulator.Update(50);
            }

            if (!blueRanged.IsMainTargetInRange() || eventHandler.HasAttackFrom(blueRanged.Id))
                return false;

            var tacticalDestination = blueRanged.GetDestinationForTest();
            var redSurvivor = entities[5];
            for (var i = 3; i < 6; i++)
            {
                if (entities[i] != redSurvivor)
                    entities[i].Hit(entities[i].MaxHp);
            }

            var survivorPosition = new GridPos(28, 0).ToFixedPos();
            redSurvivor.SetPos(survivorPosition);
            redSurvivor.SetDestination(survivorPosition);
            for (var i = 0; i < 20; i++)
            {
                simulator.Update(50);
            }

            return !eventHandler.HasAttackFrom(blueRanged.Id) &&
                   blueRanged.GetDestinationForTest() == tacticalDestination;
        }

        private static bool TestFailedAuthoredDestinationResumeIsAttemptedOnce()
        {
            var context = new ResumePathTestContext(false);
            var attackerData = CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000);
            attackerData.attackDelayMs = 0;
            var enemyData = CreateEntityData(TeamFlag.Red, string.Empty, string.Empty, 2000);
            var attacker = new Entity(1, context, attackerData);
            var enemy = new Entity(2, context, enemyData);
            context.AddEntity(attacker);
            context.AddEntity(enemy);
            attacker.SetPos(new GridPos(-1, 0));
            attacker.SetDestination(new GridPos(10, 0).ToFixedPos());
            attacker.SetTacticalDestination(
                new GridPos(0, 0).ToFixedPos(),
                new List<GridPos> { new(0, 0) });
            enemy.SetPos(new GridPos(10, 0));
            enemy.SetDestination(enemy.GetPos());

            for (var i = 0; i < 20 && attacker.ShouldPrioritizeMovement; i++)
            {
                context.Update(attacker, 50);
            }

            enemy.SetPos(new GridPos(1, 0));
            for (var i = 0; i < 20 && context.AttackRequestCount == 0; i++)
            {
                context.Update(attacker, 50);
            }

            if (context.AttackRequestCount == 0)
                return false;

            enemy.SetPos(new GridPos(8, 0));
            for (var i = 0; i < 20; i++)
            {
                context.Update(attacker, 50);
            }

            return context.ResumePathRequestCount == 1 &&
                   attacker.GetDestinationForTest() == new GridPos(0, 0).ToFixedPos();
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

        private static bool HasEntityPrioritizingMovementInAttackRange(
            List<Entity> entities,
            TeamFlag teamFlag)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].GetTeamFlag() == teamFlag &&
                    entities[i].ShouldPrioritizeMovement &&
                    entities[i].IsMainTargetInRange())
                {
                    return true;
                }
            }

            return false;
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

            var minimumObservedAllySpacing = GetMinimumSameTeamSpacing(entities);

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
                minimumObservedAllySpacing = Math.Min(
                    minimumObservedAllySpacing,
                    GetMinimumSameTeamSpacing(GetEntities(simulator.GetAliveEntities())));
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

            return minimumObservedAllySpacing >= 3000;
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

        private static bool AdvanceUntilFormationAttempted(BattleMapSimulator simulator)
        {
            for (var i = 0; i < 2000 && !simulator.WasInitialTacticalPositioningAttemptedForTest; i++)
            {
                simulator.Update(50);
            }

            return simulator.WasInitialTacticalPositioningAttemptedForTest;
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

        private static long GetMinimumSameTeamSpacing(List<Entity> entities)
        {
            var minimumSpacing = long.MaxValue;

            for (var i = 0; i < entities.Count; i++)
            {
                for (var j = i + 1; j < entities.Count; j++)
                {
                    if (entities[i].GetTeamFlag() != entities[j].GetTeamFlag())
                        continue;

                    minimumSpacing = Math.Min(
                        minimumSpacing,
                        entities[i].GetPos().GetDistance(entities[j].GetPos()));
                }
            }

            return minimumSpacing;
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
                if (!BattleMapPathFinder.IsAngleLessThan(
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

        private static BattleMapData CreateFourEntityTeamMapData()
        {
            var battlePositions = new List<BattlePositionData>
            {
                CreateBattlePosition("BlueStart1", -10, 0),
                CreateBattlePosition("BlueStart2", -10, -6),
                CreateBattlePosition("BlueStart3", -10, 6),
                CreateBattlePosition("BlueStart4", -10, 10),
                CreateBattlePosition("RedStart1", 10, 0),
                CreateBattlePosition("RedStart2", 10, -6),
                CreateBattlePosition("RedStart3", 10, 6),
                CreateBattlePosition("RedStart4", 10, 10),
                CreateBattlePosition("BlueEnd1", 25, 0),
                CreateBattlePosition("BlueEnd2", 25, -6),
                CreateBattlePosition("BlueEnd3", 25, 6),
                CreateBattlePosition("BlueEnd4", 25, 10),
                CreateBattlePosition("RedEnd1", -25, 0),
                CreateBattlePosition("RedEnd2", -25, -6),
                CreateBattlePosition("RedEnd3", -25, 6),
                CreateBattlePosition("RedEnd4", -25, 10),
            };

            return new BattleMapData
            {
                minGridPos = new GridPos(-30, -20),
                maxGridPos = new GridPos(30, 20),
                useInitialTacticalPositioning = false,
                battlePositions = battlePositions,
                obstacles = new List<ObstacleData>(),
                entities = new List<EntityData>
                {
                    CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 5000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart2", "BlueEnd2", 12000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart3", "BlueEnd3", 12000),
                    CreateEntityData(TeamFlag.Blue, "BlueStart4", "BlueEnd4", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart1", "RedEnd1", 5000),
                    CreateEntityData(TeamFlag.Red, "RedStart2", "RedEnd2", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart3", "RedEnd3", 12000),
                    CreateEntityData(TeamFlag.Red, "RedStart4", "RedEnd4", 12000),
                },
            };
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

        private sealed class AttackRecordingEventHandler : IBattleMapEventHandler
        {
            private readonly List<uint> _attackerIds = new();

            public bool HasAttackFrom(uint attackerId) => _attackerIds.Contains(attackerId);

            public void OnEntityAdded(uint entityId, Entity entity) { }
            public void OnEntityPositionChanged(uint entityId, FixedPos pos) { }
            public void OnEntityDirectionChanged(uint entityId, FixedDir dir) { }
            public void OnEntityStartMove(uint entityId) { }
            public void OnEntityStopMove(uint entityId) { }
            public void OnEntityStartAttack(uint attackerId, uint targetId) => _attackerIds.Add(attackerId);
            public void OnEntityGetDamage(uint entityId, uint damage) { }
            public void OnEntityRetired(uint entityId) { }
            public void OnProjectileAdded(ulong projectileId, Projectile projectile) { }
            public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos) { }
            public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir) { }
            public void OnProjectileTriggered(ulong projectileId) { }
            public void OnBattleEnd(TeamFlag winner) { }
            public void OnBattleMapUpdated(ushort deltaMs) { }
        }

        private sealed class ResumePathTestContext : IBattleMapContext
        {
            private readonly bool _shouldFindResumePath;
            private readonly List<Entity> _entities = new();
            private readonly BattleMapPathFinder _pathFinder;

            public ResumePathTestContext(bool shouldFindResumePath)
            {
                _shouldFindResumePath = shouldFindResumePath;
                _pathFinder = new BattleMapPathFinder(CreateMapData(false));
            }

            public int AttackRequestCount { get; private set; }
            public int ResumePathRequestCount { get; private set; }
            public int PathSmoothingCallCount { get; private set; }
            public int LastSmoothedWaypointCount { get; private set; }
            public uint ElapsedMs { get; private set; }

            public void AddEntity(Entity entity)
            {
                _entities.Add(entity);
            }

            public void Update(Entity entity, ushort deltaMs)
            {
                ElapsedMs += deltaMs;
                entity.Update(deltaMs);
            }

            public IEntityContext TryGetNearestEnemy(uint entityId, long maxDistance)
            {
                var entity = GetEntity(entityId);
                Entity nearest = null;
                var nearestDistance = long.MaxValue;

                for (var i = 0; i < _entities.Count; i++)
                {
                    var otherEntity = _entities[i];
                    if (otherEntity.Id == entityId ||
                        !otherEntity.IsAlive() ||
                        otherEntity.GetTeamFlag() == entity.GetTeamFlag())
                    {
                        continue;
                    }

                    var distance = entity.GetPos().GetDistance(otherEntity.GetPos());
                    if (distance <= maxDistance && distance < nearestDistance)
                    {
                        nearest = otherEntity;
                        nearestDistance = distance;
                    }
                }

                return nearest;
            }

            public bool HasAliveEnemy(uint entityId)
            {
                var entity = GetEntity(entityId);
                for (var i = 0; i < _entities.Count; i++)
                {
                    if (_entities[i].Id != entityId &&
                        _entities[i].IsAlive() &&
                        _entities[i].GetTeamFlag() != entity.GetTeamFlag())
                    {
                        return true;
                    }
                }

                return false;
            }

            public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
            {
                resultWaypoints.Add(goal);
            }

            public bool TryFindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
            {
                ++ResumePathRequestCount;
                if (!_shouldFindResumePath)
                    return false;

                resultWaypoints.Add(goal);
                resultWaypoints.Add(start);
                return true;
            }

            public void SmoothPathTransition(FixedPos start, FixedDir incomingDirection, List<GridPos> waypoints)
            {
                ++PathSmoothingCallCount;
                _pathFinder.SmoothPathTransition(start, incomingDirection, waypoints);
                LastSmoothedWaypointCount = waypoints.Count;
            }

            public void RequestAttack(uint attackerId, uint targetEntityId)
            {
                ++AttackRequestCount;
            }

            public void OnEntityPositionChanged(uint entityId, FixedPos pos) { }
            public void OnEntityDirectionChanged(uint entityId, FixedDir dir) { }
            public void OnEntityGetDamage(uint entityId, uint damage) { }
            public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos) { }
            public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir) { }
            public void OnProjectileTriggered(ulong projectileId) { }
            public void OnEntityStartMove(uint entityId) { }
            public void OnEntityStopMove(uint entityId) { }

            private Entity GetEntity(uint entityId)
            {
                for (var i = 0; i < _entities.Count; i++)
                {
                    if (_entities[i].Id == entityId)
                        return _entities[i];
                }

                return null;
            }
        }
    }
}
