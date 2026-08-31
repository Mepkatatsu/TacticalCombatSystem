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
            return Verify<TacticalMovementStateTest>(TestTargetDepartureBeforeAttackResumesAuthoredDestination(),
                nameof(TestTargetDepartureBeforeAttackResumesAuthoredDestination));
        }

        private static bool TestTargetDepartureBeforeAttackResumesAuthoredDestination()
        {
            var context = new PathTestContext();
            var scenario = CreateTacticalScenario(context);

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

        private static (Entity Attacker, Entity Enemy, FixedPos AuthoredDestination, FixedPos TacticalDestination)
            CreateTacticalScenario(PathTestContext context)
        {
            var attackerData = CreateEntityData(TeamFlag.Blue, string.Empty, string.Empty, 2000);
            attackerData.attackDelayMs = ushort.MaxValue;
            var attacker = new Entity(1, context, attackerData);
            var enemy = new Entity(2, context, CreateEntityData(TeamFlag.Red, string.Empty, string.Empty, 2000));
            context.AddEntity(attacker);
            context.AddEntity(enemy);

            var authoredDestination = new GridPos(10, 0).ToFixedPos();
            var tacticalDestination = new GridPos(0, 0).ToFixedPos();
            attacker.SetPos(new GridPos(-1, 0));
            attacker.SetDestination(authoredDestination);
            attacker.SetTacticalDestination(tacticalDestination, new List<GridPos> { tacticalDestination.ToGridPos() });
            enemy.SetPos(new GridPos(1, 0));
            enemy.SetDestination(enemy.GetPos());
            return (attacker, enemy, authoredDestination, tacticalDestination);
        }

        private sealed class PathTestContext : IBattleMapContext
        {
            private Entity _firstEntity;
            private Entity _secondEntity;

            public int AttackRequestCount { get; private set; }
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
                resultWaypoints.Add(goal);
                resultWaypoints.Add(start);
                return true;
            }

            public bool TryFindWaypointsFromArbitraryPositions(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
            {
                ++AuthoredPathRequestCount;
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
