using System.Collections.Generic;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    public class BattleMapSimulatorDeterminismTest : ITest
    {
        public bool Test()
        {
            return TestInsertionOrderDeterminism() &&
                   TestEntityRemovalKeepsUpdateOrder() &&
                   TestProjectileRemovalKeepsRemainingProjectilesInIdOrder();
        }

        private static bool TestInsertionOrderDeterminism()
        {
            var first = CreateSimulation(4, 3, 1, 2);
            var second = CreateSimulation(2, 4, 3, 1);

            if (!HasAliveEntityIds(first.simulator, 1, 2, 3, 4) ||
                !HasAliveEntityIds(second.simulator, 1, 2, 3, 4))
                return false;

            // Entity 1의 같은 거리 적인 Entity 2와 4 중 EntityId가 작은 Entity 2가 선택되어야 한다.
            if (first.simulator.TryGetNearestEnemy(1, long.MaxValue)?.Id != 2 ||
                second.simulator.TryGetNearestEnemy(1, long.MaxValue)?.Id != 2)
            {
                return false;
            }

            // 첫 tick은 IdleState에서 타겟을 찾고, 두 번째 tick은 AttackState 전이 후 공격 이벤트를 발생시킨다.
            first.simulator.Update(1);
            first.simulator.Update(1);
            second.simulator.Update(1);
            second.simulator.Update(1);

            // 홀수 ID는 Blue 팀, 짝수 ID는 Red 팀이다. 같은 거리 적 중 EntityId가 작은 대상을 선택한다.
            var expectedAttacks = new (uint attackerId, uint targetId)[]
            {
                (1, 2),
                (2, 1),
                (3, 2),
                (4, 1),
            };

            return HasSameAttackOrder(first.eventHandler.Attacks, second.eventHandler.Attacks) &&
                   HasAttackOrder(first.eventHandler.Attacks, expectedAttacks);
        }

        private static bool TestEntityRemovalKeepsUpdateOrder()
        {
            var simulation = CreateSimulation(4, 2, 1, 3);

            simulation.simulator.Update(1);
            simulation.entities[2].Hit(simulation.entities[2].Hp);
            simulation.simulator.OnEntityRetired(2);
            simulation.simulator.Update(1);

            if (!HasAliveEntityIds(simulation.simulator, 1, 3, 4))
                return false;

            simulation.eventHandler.Attacks.Clear();
            simulation.simulator.Update(1);

            return HasAttackOrder(simulation.eventHandler.Attacks, (1, 4), (3, 4), (4, 1));
        }

        private static bool TestProjectileRemovalKeepsRemainingProjectilesInIdOrder()
        {
            var simulation = CreateSimulation(2, 1);

            simulation.simulator.RequestAttack(1, 2);
            simulation.simulator.RequestAttack(1, 2);
            simulation.simulator.RequestAttack(1, 2);

            // ID 2만 제거 예약한 뒤, 다음 tick에 남은 ID 1과 3만 순서대로 갱신되어야 한다.
            simulation.simulator.OnProjectileTriggered(2);
            simulation.eventHandler.ProjectilePositionChanges.Clear();
            simulation.eventHandler.ProjectileTriggers.Clear();
            simulation.simulator.Update(500);

            return !simulation.simulator.IsProjectileScheduledForUpdateForTest(2) &&
                   simulation.simulator.IsProjectileScheduledForUpdateForTest(1) &&
                   simulation.simulator.IsProjectileScheduledForUpdateForTest(3) &&
                   HasProjectileOrder(simulation.eventHandler.ProjectilePositionChanges, 1, 3) &&
                   HasProjectileOrder(simulation.eventHandler.ProjectileTriggers, 1, 3);
        }

        private static (BattleMapSimulator simulator, RecordingEventHandler eventHandler, Dictionary<uint, Entity> entities) CreateSimulation(params uint[] entityIds)
        {
            var eventHandler = new RecordingEventHandler();
            var entities = new Dictionary<uint, Entity>();
            var mapData = new BattleMapData
            {
                battlePositions = new List<BattlePositionData>(),
                entities = new List<EntityData>(),
            };
            var simulator = new BattleMapSimulator(eventHandler, mapData);

            for (var i = 0; i < entityIds.Length; i++)
            {
                var entityId = entityIds[i];
                var teamFlag = entityId % 2 == 1 ? TeamFlag.Blue : TeamFlag.Red;
                var entity = new Entity(entityId, simulator, new EntityData
                {
                    teamFlag = teamFlag,
                    maxHp = 100,
                    attackDamage = 0,
                    attackRange = 1,
                    attackDelayMs = 0,
                });
                simulator.OnEntityAdded(entityId, entity);
                entity.SetPos(new GridPos(0, 0));
                entities.Add(entityId, entity);
            }

            return (simulator, eventHandler, entities);
        }

        private static bool HasAliveEntityIds(BattleMapSimulator simulator, params uint[] entityIds)
        {
            var aliveEntities = simulator.GetAliveEntities();
            if (aliveEntities.Count != entityIds.Length)
                return false;

            for (var i = 0; i < entityIds.Length; i++)
            {
                if (aliveEntities[i].Id != entityIds[i])
                    return false;
            }

            return true;
        }

        private static bool HasSameAttackOrder(List<(uint attackerId, uint targetId)> first, List<(uint attackerId, uint targetId)> second)
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

        private static bool HasAttackOrder(List<(uint attackerId, uint targetId)> attacks, params (uint attackerId, uint targetId)[] expectedAttacks)
        {
            if (attacks.Count != expectedAttacks.Length)
                return false;

            for (var i = 0; i < attacks.Count; i++)
            {
                if (attacks[i] != expectedAttacks[i])
                    return false;
            }

            return true;
        }

        private static bool HasProjectileOrder(List<ulong> projectileIds, params ulong[] expectedIds)
        {
            if (projectileIds.Count != expectedIds.Length)
                return false;

            for (var i = 0; i < projectileIds.Count; i++)
            {
                if (projectileIds[i] != expectedIds[i])
                    return false;
            }

            return true;
        }

        private sealed class RecordingEventHandler : IBattleMapEventHandler
        {
            public List<(uint attackerId, uint targetId)> Attacks { get; } = new();
            public List<ulong> ProjectilePositionChanges { get; } = new();
            public List<ulong> ProjectileTriggers { get; } = new();

            public void OnEntityAdded(uint entityId, Entity entity) { }
            public void OnEntityPositionChanged(uint entityId, FixedPos pos) { }
            public void OnEntityDirectionChanged(uint entityId, FixedDir dir) { }
            public void OnEntityStartMove(uint entityId) { }
            public void OnEntityStopMove(uint entityId) { }
            public void OnEntityStartAttack(uint attackerId, uint targetId) => Attacks.Add((attackerId, targetId));
            public void OnEntityGetDamage(uint entityId, uint damage) { }
            public void OnEntityRetired(uint entityId) { }
            public void OnProjectileAdded(ulong projectileId, Projectile projectile) { }
            public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos) => ProjectilePositionChanges.Add(projectileId);
            public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir) { }
            public void OnProjectileTriggered(ulong projectileId) => ProjectileTriggers.Add(projectileId);
            public void OnBattleEnd(TeamFlag winner) { }
            public void OnBattleMapUpdated(ushort deltaMs) { }
        }
    }
}
