using System.Collections.Generic;
using Script.CommonLib.Map;
using static Script.CommonLib.Tests.TestResultVerifier;
using static Script.CommonLib.Tests.TacticalPositioningTestHelper;

namespace Script.CommonLib.Tests
{
    public sealed class TacticalMovementStateTest : ITest
    {
        public bool Test()
        {
            var success = true;

            success &= Verify<TacticalMovementStateTest>(TestTacticalMovementPriorityContract(),
                nameof(TestTacticalMovementPriorityContract));
            success &= Verify<TacticalMovementStateTest>(TestAuthoredDestinationResumeContract(),
                nameof(TestAuthoredDestinationResumeContract));
            success &= Verify<TacticalMovementStateTest>(TestPathFailureIsNotRetried(),
                nameof(TestPathFailureIsNotRetried));
            return success;
        }

        private static bool TestTacticalMovementPriorityContract()
        {
            return TestPlannedEntityReachesTacticalDestinationThroughCombatChanges() &&
                   TestCurrentPositionIsRemovedFromTacticalPath();
        }

        private static bool TestPlannedEntityReachesTacticalDestinationThroughCombatChanges()
        {
            var simulator = CreateSimulator();
            simulator.Init();
            if (!AdvanceUntilFormationAttempted(simulator))
                return false;

            var entities = GetEntities(simulator.GetAliveEntities());
            if (!HasEntityPrioritizingMovementInAttackRange(entities, TeamFlag.Blue) ||
                !HasEntityPrioritizingMovementInAttackRange(entities, TeamFlag.Red))
            {
                return false;
            }

            simulator.Update(50);
            var blueRanged = entities[1];
            var destination = blueRanged.GetDestinationForTest();
            var currentTargetId = blueRanged.GetMainTargetIdForTest();
            if (!blueRanged.ShouldPrioritizeMovement || !currentTargetId.HasValue)
            {
                return false;
            }

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
            simulator.Update(50);
            if (!blueRanged.IsAlive() || currentTarget.IsAlive() || !blueRanged.ShouldPrioritizeMovement)
                return false;

            for (var i = 0; i < 500 && blueRanged.ShouldPrioritizeMovement; i++)
            {
                simulator.Update(50);
            }

            return blueRanged.IsAlive() &&
                   !currentTarget.IsAlive() &&
                   !blueRanged.ShouldPrioritizeMovement &&
                   blueRanged.GetPos() == destination;
        }

        private static bool TestCurrentPositionIsRemovedFromTacticalPath()
        {
            var context = new PathTestContext(shouldFindAuthoredPath: true);
            var entity = new Entity(1, context, CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000));
            context.AddEntity(entity);
            entity.SetPos(new GridPos(-5, 0));
            entity.SetDestination(new GridPos(5, 0).ToFixedPos());
            context.Update(entity, 50);
            context.Update(entity, 50);

            var positionBeforeTacticalMove = entity.GetPos();
            var tacticalDestination = new GridPos(5, 5);
            entity.SetTacticalDestination(
                tacticalDestination.ToFixedPos(),
                new List<GridPos> { tacticalDestination, positionBeforeTacticalMove.ToGridPos() });
            context.Update(entity, 50);

            var positionAfterTacticalMove = entity.GetPos();
            return positionAfterTacticalMove.X > positionBeforeTacticalMove.X &&
                   positionAfterTacticalMove.Z > positionBeforeTacticalMove.Z;
        }

        private static bool TestAuthoredDestinationResumeContract()
        {
            return TestExecutedAttackResumesAuthoredDestination() &&
                   TestTargetDepartureBeforeAttackResumesAuthoredDestination();
        }

        private static bool TestExecutedAttackResumesAuthoredDestination()
        {
            var context = new PathTestContext(shouldFindAuthoredPath: true);
            var scenario = CreateTacticalScenario(context, new GridPos(1, 0), 0);

            for (var i = 0; i < 20 && context.AttackRequestCount == 0; i++)
                context.Update(scenario.Attacker, 50);

            if (context.AttackRequestCount == 0)
                return false;

            var tacticalPosition = scenario.Attacker.GetPos();
            if (tacticalPosition != scenario.TacticalDestination ||
                scenario.Attacker.GetDestinationForTest() != scenario.TacticalDestination)
            {
                return false;
            }

            scenario.Enemy.SetPos(new GridPos(8, 0));
            for (var i = 0; i < 20; i++)
            {
                if (scenario.Attacker.GetDestinationForTest() == scenario.AuthoredDestination &&
                    scenario.Attacker.GetPos() != tacticalPosition)
                {
                    break;
                }

                context.Update(scenario.Attacker, 50);
            }

            return context.AuthoredPathRequestCount == 1 &&
                   scenario.Attacker.GetDestinationForTest() == scenario.AuthoredDestination &&
                   scenario.Attacker.GetPos() != tacticalPosition;
        }

        private static bool TestTargetDepartureBeforeAttackResumesAuthoredDestination()
        {
            var context = new PathTestContext(shouldFindAuthoredPath: true);
            var scenario = CreateTacticalScenario(context, new GridPos(1, 0), ushort.MaxValue);

            for (var i = 0; i < 20 && scenario.Attacker.ShouldPrioritizeMovement; i++)
                context.Update(scenario.Attacker, 50);

            if (scenario.Attacker.GetPos() != scenario.TacticalDestination || context.AttackRequestCount != 0)
                return false;

            scenario.Enemy.SetPos(new GridPos(8, 0));
            for (var i = 0; i < 20 && scenario.Attacker.GetDestinationForTest() != scenario.AuthoredDestination; i++)
                context.Update(scenario.Attacker, 50);

            return context.AttackRequestCount == 0 &&
                   context.AuthoredPathRequestCount == 1 &&
                   scenario.Attacker.GetDestinationForTest() == scenario.AuthoredDestination;
        }

        private static bool TestPathFailureIsNotRetried()
        {
            return TestGeneralDestinationPathFailureIsAttemptedOnce() &&
                   TestAuthoredDestinationResumeFailureIsAttemptedOnce();
        }

        private static bool HasEntityPrioritizingMovementInAttackRange(List<Entity> entities, TeamFlag teamFlag)
        {
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (entity.GetTeamFlag() == teamFlag &&
                    entity.ShouldPrioritizeMovement &&
                    entity.IsMainTargetInRange())
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TestGeneralDestinationPathFailureIsAttemptedOnce()
        {
            var context = new PathTestContext(shouldFindAuthoredPath: true, shouldFindGeneralPath: false);
            var entity = new Entity(1, context, CreateEntityData(TeamFlag.Blue, "BlueStart1", "BlueEnd1", 12000));
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
                   context.GeneralPathRequestCount == 1;
        }

        private static bool TestAuthoredDestinationResumeFailureIsAttemptedOnce()
        {
            var context = new PathTestContext(shouldFindAuthoredPath: false);
            var scenario = CreateTacticalScenario(context, new GridPos(10, 0), 0);

            for (var i = 0; i < 20 && scenario.Attacker.ShouldPrioritizeMovement; i++)
                context.Update(scenario.Attacker, 50);

            scenario.Enemy.SetPos(new GridPos(1, 0));
            for (var i = 0; i < 20 && context.AttackRequestCount == 0; i++)
                context.Update(scenario.Attacker, 50);

            if (context.AttackRequestCount == 0)
                return false;

            var tacticalPosition = scenario.Attacker.GetPos();
            if (tacticalPosition != scenario.TacticalDestination)
                return false;

            scenario.Enemy.SetPos(new GridPos(8, 0));
            for (var i = 0; i < 20; i++)
                context.Update(scenario.Attacker, 50);

            return context.AuthoredPathRequestCount == 1 &&
                   scenario.Attacker.GetDestinationForTest() == scenario.TacticalDestination &&
                   scenario.Attacker.GetPos() == tacticalPosition;
        }

        private static (Entity Attacker, Entity Enemy, FixedPos AuthoredDestination, FixedPos TacticalDestination)
            CreateTacticalScenario(PathTestContext context, GridPos enemyPosition, ushort attackDelayMs)
        {
            var attackerData = CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000);
            attackerData.attackDelayMs = attackDelayMs;
            var attacker = new Entity(1, context, attackerData);
            var enemy = new Entity(2, context, CreateEntityData(TeamFlag.Red, string.Empty, string.Empty, 2000));
            context.AddEntity(attacker);
            context.AddEntity(enemy);

            var authoredDestination = new GridPos(10, 0).ToFixedPos();
            var tacticalDestination = new GridPos(0, 0).ToFixedPos();
            attacker.SetPos(new GridPos(-1, 0));
            attacker.SetDestination(authoredDestination);
            attacker.SetTacticalDestination(tacticalDestination, new List<GridPos> { tacticalDestination.ToGridPos() });
            enemy.SetPos(enemyPosition);
            enemy.SetDestination(enemy.GetPos());
            return (attacker, enemy, authoredDestination, tacticalDestination);
        }

        private sealed class PathTestContext : IBattleMapContext
        {
            private readonly bool _shouldFindAuthoredPath;
            private readonly bool _shouldFindGeneralPath;
            private Entity _firstEntity;
            private Entity _secondEntity;

            public PathTestContext(bool shouldFindAuthoredPath, bool shouldFindGeneralPath = true)
            {
                _shouldFindAuthoredPath = shouldFindAuthoredPath;
                _shouldFindGeneralPath = shouldFindGeneralPath;
            }

            public int AttackRequestCount { get; private set; }
            public int GeneralPathRequestCount { get; private set; }
            public int AuthoredPathRequestCount { get; private set; }
            public uint ElapsedMs { get; private set; }

            public void AddEntity(Entity entity)
            {
                if (_firstEntity == null)
                    _firstEntity = entity;
                else
                    _secondEntity = entity;
            }

            public void Update(Entity entity, ushort deltaMs)
            {
                ElapsedMs += deltaMs;
                entity.Update(deltaMs);
            }

            public IEntityContext TryGetNearestEnemy(uint entityId, long maxDistance)
            {
                var entity = GetEntity(entityId);
                var enemy = GetOtherEntity(entityId);
                if (entity == null || enemy == null || !enemy.IsAlive() ||
                    enemy.GetTeamFlag() == entity.GetTeamFlag())
                {
                    return null;
                }

                return entity.GetPos().GetDistance(enemy.GetPos()) <= maxDistance ? enemy : null;
            }

            public bool HasAliveEnemy(uint entityId)
            {
                var entity = GetEntity(entityId);
                var enemy = GetOtherEntity(entityId);
                return entity != null &&
                       enemy != null &&
                       enemy.IsAlive() &&
                       enemy.GetTeamFlag() != entity.GetTeamFlag();
            }

            public bool TryFindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
            {
                ++GeneralPathRequestCount;
                if (!_shouldFindGeneralPath)
                    return false;

                resultWaypoints.Add(goal);
                resultWaypoints.Add(start);
                return true;
            }

            public bool TryFindWaypointsFromArbitraryPositions(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
            {
                ++AuthoredPathRequestCount;
                if (!_shouldFindAuthoredPath)
                    return false;

                resultWaypoints.Add(goal);
                resultWaypoints.Add(start);
                return true;
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
                if (_firstEntity != null && _firstEntity.Id == entityId)
                    return _firstEntity;
                if (_secondEntity != null && _secondEntity.Id == entityId)
                    return _secondEntity;

                return null;
            }

            private Entity GetOtherEntity(uint entityId)
            {
                if (_firstEntity != null && _firstEntity.Id != entityId)
                    return _firstEntity;
                if (_secondEntity != null && _secondEntity.Id != entityId)
                    return _secondEntity;

                return null;
            }
        }
    }
}
