using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.CommonLib.Map;

namespace Script.CommonLib.Tests
{
    public partial class InitialTacticalFormationPlannerTest
    {
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

        private static BattleMapSimulator CreateSimulator()
        {
            return new BattleMapSimulator(
                NullBattleMapEventHandler.Instance,
                CreateMapData());
        }

        private static bool AdvanceUntilFormationAttempted(BattleMapSimulator simulator)
        {
            for (var i = 0; i < 2000 && !simulator.WasInitialTacticalPositioningAttemptedForTest; i++)
            {
                simulator.Update(50);
            }

            return simulator.WasInitialTacticalPositioningAttemptedForTest;
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

        private static BattleMapData CreateMapData()
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
            private readonly bool _shouldFindPath;
            private readonly List<Entity> _entities = new();
            private readonly BattleMapPathFinder _pathFinder;
            private readonly BattleMapPathSmoother _pathSmoother;

            public ResumePathTestContext(bool shouldFindResumePath, bool shouldFindPath = true)
            {
                _shouldFindResumePath = shouldFindResumePath;
                _shouldFindPath = shouldFindPath;
                var mapData = CreateMapData();
                _pathFinder = new BattleMapPathFinder(mapData);
                _pathSmoother = new BattleMapPathSmoother(mapData, _pathFinder);
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
