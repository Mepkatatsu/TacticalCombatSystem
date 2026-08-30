using System.Collections.Generic;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TacticalPositioningTestData;

namespace Script.CommonLib.Tests
{
    public sealed class TacticalMovementStateTest : ITest
    {
        public bool Test()
        {
            var success = true;

            success &= Verify(TestPlannedEntityMovesEvenWhenEnemyIsInRange(), nameof(TestPlannedEntityMovesEvenWhenEnemyIsInRange));
            success &= Verify(TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken(), nameof(TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken));
            success &= Verify(TestUnplannedEntityKeepsAttackPriority(), nameof(TestUnplannedEntityKeepsAttackPriority));
            success &= Verify(TestImmobilePlannedEntityReleasesMovementPriority(), nameof(TestImmobilePlannedEntityReleasesMovementPriority));
            success &= Verify(TestSubStepTacticalMovementReleasesMovementPriority(), nameof(TestSubStepTacticalMovementReleasesMovementPriority));
            success &= Verify(TestExhaustedTacticalPathReleasesMovementPriority(), nameof(TestExhaustedTacticalPathReleasesMovementPriority));
            success &= Verify(TestFailedMovementPathStopsAndIsNotRetried(), nameof(TestFailedMovementPathStopsAndIsNotRetried));
            success &= Verify(TestEntityResumesAuthoredDestinationAfterExecutedAttack(), nameof(TestEntityResumesAuthoredDestinationAfterExecutedAttack));
            success &= Verify(TestEntityRequestsSmoothingWhenResumingAuthoredDestination(), nameof(TestEntityRequestsSmoothingWhenResumingAuthoredDestination));
            success &= Verify(TestEntityResumesWhenTargetLeavesBeforeExecutingAttack(), nameof(TestEntityResumesWhenTargetLeavesBeforeExecutingAttack));
            success &= Verify(TestFailedAuthoredDestinationResumeIsAttemptedOnce(), nameof(TestFailedAuthoredDestinationResumeIsAttemptedOnce));
            success &= Verify(TestEntityRequestsSmoothingForTacticalDestinationAfterMovement(), nameof(TestEntityRequestsSmoothingForTacticalDestinationAfterMovement));
            return success;
        }

        private static bool TestPlannedEntityMovesEvenWhenEnemyIsInRange()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());

            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            return HasEntityPrioritizingMovementInAttackRange(entities, TeamFlag.Blue) &&
                   HasEntityPrioritizingMovementInAttackRange(entities, TeamFlag.Red);
        }

        private static bool TestTacticalMovementReachesDestinationAfterCurrentTargetDeathAndDamageTaken()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

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
            var simulator = CreateSimulator();
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var blueFrontline = entities[0];

            for (var i = 0; i < 30; i++)
            {
                simulator.Update(50);
            }

            return !blueFrontline.ShouldPrioritizeMovement &&
                   blueFrontline.CurrentStateType == Script.CommonLib.Battle.EntityStateType.Attack;
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
            var mapData = CreateMapData();
            mapData.entities[1].moveSpeed = moveSpeed;
            var simulator = new BattleMapSimulator(NullBattleMapEventHandler.Instance, mapData);
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

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
            var context = new ResumePathTestContext(false);
            var entity = new Entity(
                1,
                context,
                CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 12000));
            context.AddEntity(entity);
            var currentPosition = new GridPos(-6, 0).ToFixedPos();
            entity.SetPos(currentPosition);
            entity.SetDestination(new GridPos(20, 0).ToFixedPos());
            var unreachableDestination = new FixedPos(
                currentPosition.X + 10000,
                currentPosition.Y,
                currentPosition.Z);
            entity.SetTacticalDestination(
                unreachableDestination,
                new List<GridPos> { currentPosition.ToGridPos() });

            context.Update(entity, 50);

            return entity.GetPos() == currentPosition &&
                   !entity.ShouldPrioritizeMovement;
        }

        private static bool TestFailedMovementPathStopsAndIsNotRetried()
        {
            var context = new ResumePathTestContext(false, false);
            var entity = new Entity(
                1,
                context,
                CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 12000));
            context.AddEntity(entity);
            var start = new GridPos(-6, 0).ToFixedPos();
            entity.SetPos(start);
            entity.SetDestination(new GridPos(20, 0).ToFixedPos());

            context.Update(entity, 50);
            context.Update(entity, 50);
            context.Update(entity, 50);

            return entity.GetPos() == start &&
                   entity.CurrentStateType == Script.CommonLib.Battle.EntityStateType.Idle &&
                   entity.HasPathSearchFailed &&
                   context.PathRequestCount == 1;
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

        private static bool TestEntityRequestsSmoothingWhenResumingAuthoredDestination()
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
                   context.PathSmoothingCallCount == 1;
        }

        private static bool TestEntityResumesWhenTargetLeavesBeforeExecutingAttack()
        {
            var mapData = CreateMapData();
            var eventHandler = new AttackRecordingEventHandler();
            var simulator = new BattleMapSimulator(eventHandler, mapData);
            simulator.Init();
            var entities = GetEntities(simulator.GetAliveEntities());
            var blueRanged = entities[1];
            var authoredDestination = blueRanged.GetDestinationForTest();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

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
                   blueRanged.GetDestinationForTest() == authoredDestination;
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

        private static bool TestEntityRequestsSmoothingForTacticalDestinationAfterMovement()
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
            context.Update(entity, 50);

            var currentGridPosition = entity.GetPos().ToGridPos();
            var tacticalPath = new List<GridPos> { new(5, 5), currentGridPosition };
            entity.SetTacticalDestination(new GridPos(5, 5).ToFixedPos(), tacticalPath);

            return context.PathSmoothingCallCount == 1;
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

        private static bool Verify(bool result, string testName)
        {
            if (!result)
                LogHelper.Error($"[{nameof(TacticalMovementStateTest)}] {testName} failed.");

            return result;
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
            private readonly bool _shouldFindPath;
            private readonly List<Entity> _entities = new();
            private readonly BattleMapPathSmoother _pathSmoother;

            public ResumePathTestContext(bool shouldFindResumePath, bool shouldFindPath = true)
            {
                _shouldFindResumePath = shouldFindResumePath;
                _shouldFindPath = shouldFindPath;
                var mapData = CreateMapData();
                _pathSmoother = new BattleMapPathSmoother(mapData, new BattleMapPathFinder(mapData));
            }

            public int AttackRequestCount { get; private set; }
            public int PathRequestCount { get; private set; }
            public int ResumePathRequestCount { get; private set; }
            public int PathSmoothingCallCount { get; private set; }
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

            public bool TryFindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
            {
                ++PathRequestCount;
                if (!_shouldFindPath)
                    return false;

                resultWaypoints.Add(goal);
                resultWaypoints.Add(start);
                return true;
            }

            public bool TryFindWaypointsFromArbitraryPositions(
                GridPos start,
                GridPos goal,
                List<GridPos> resultWaypoints)
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
                _pathSmoother.SmoothPathTransition(start, incomingDirection, waypoints);
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
