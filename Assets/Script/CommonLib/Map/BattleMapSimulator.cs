using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public class BattleMapSimulator : IBattleMapContext
    {
        public BattleMapSimulator(IBattleMapEventHandler battleMapEventHandler, BattleMapData battleMapData)
        {
            _battleMapEventHandler = battleMapEventHandler;
            _battleMapData = battleMapData;
            _battleMapPathFinder = new BattleMapPathFinder(battleMapData);
        }
        
        private readonly IBattleMapEventHandler _battleMapEventHandler;
        private readonly BattleMapData _battleMapData;
        private readonly BattleMapPathFinder _battleMapPathFinder;
        
        private readonly Dictionary<uint, Entity> _entities = new();            // TODO: PoolObject
        private readonly Dictionary<ulong, Projectile> _projectiles = new();    // TODO: PoolObject
        
        private readonly List<uint> _removeEntityIds = new();
        private readonly List<ulong> _removeProjectileIds = new();
        
        private uint _entityIdKey;
        private ulong _projectileIdKey;
        
        private bool _battleEnded;
        
        public float ElapsedSec { get; private set; }

        public void Init()
        {
            ElapsedSec = 0;
            
            foreach (var entityData in _battleMapData.entities)
            {
                AddEntity(entityData);
            }
        }

        public void Update(float deltaTime)
        {
            if (_battleEnded)
                return;
            
            ElapsedSec += deltaTime;
            
            foreach (var removeProjectileId in _removeProjectileIds)
            {
                _projectiles.Remove(removeProjectileId);
            }
            _removeProjectileIds.Clear();
            
            foreach (var removeEntityId in _removeEntityIds)
            {
                _entities.Remove(removeEntityId);
            }
            _removeEntityIds.Clear();
            
            foreach (var projectile in _projectiles.Values)
            {
                projectile.Update(deltaTime);
            }
            
            foreach (var entity in _entities.Values)
            {
                entity.Update(deltaTime);
            }
            
            OnBattleMapUpdated(deltaTime);
            CheckBattleEnd();
        }

        private void RemoveEntity(uint entityId)
        {
            _removeEntityIds.Add(entityId);
        }

        private void RemoveProjectile(ulong projectileId)
        {
            _removeProjectileIds.Add(projectileId);
        }

        private void AddEntity(EntityData entityData)
        {
            var entity = new Entity(++_entityIdKey, this, entityData);
            OnEntityAdded(_entityIdKey, entity);
        }

        public void OnEntityAdded(uint entityId, Entity entity)
        {
            _entities.Add(entityId, entity);
            _battleMapEventHandler.OnEntityAdded(entityId, entity);

            var startPositionData = _battleMapData.GetBattlePositionDataByName(entity.startPositionName);
            var endPositionData = _battleMapData.GetBattlePositionDataByName(entity.endPositionName);
            
            if (startPositionData != null)
                entity.SetPos(startPositionData.gridPos);
            
            if (endPositionData != null)
                entity.SetDestination(new Vec3(endPositionData.gridPos.x, 0, endPositionData.gridPos.y));
        }

        public void OnEntityRetired(uint entityId)
        {
            _battleMapEventHandler.OnEntityRetired(entityId);
            RemoveEntity(entityId);
        }

        private void CheckBattleEnd()
        {
            var blueTeamCount = 0;
            var redTeamCount = 0;
            
            foreach (var entity in _entities.Values)
            {
                if (!entity.IsAlive())
                    continue;

                if (entity.GetTeamFlag() == TeamFlag.Blue)
                {
                    ++blueTeamCount;
                }
                else
                {
                    ++redTeamCount;
                }
            }

            if (blueTeamCount == 0 && redTeamCount > 0)
            {
                BattleEnd(TeamFlag.Red);
            }
            else if (redTeamCount == 0 && blueTeamCount > 0)
            {
                BattleEnd(TeamFlag.Blue);
            }
            else if (redTeamCount == 0 && blueTeamCount == 0)
            {
                BattleEnd(TeamFlag.None);
            }
        }

        private void BattleEnd(TeamFlag winner)
        {
            _battleEnded = true;
            _battleMapEventHandler.OnBattleEnd(winner);
        }

        public void RequestAttack(uint attackerId, uint targetEntityId)
        {
            if (!_entities.TryGetValue(attackerId, out var attacker))
                return;
            
            if (!_entities.TryGetValue(targetEntityId, out var target))
                return;

            if (!attacker.IsAlive())
                return;

            if (Vec3.Distance(attacker.GetPos(), target.GetPos()) > attacker.AttackRange)
                return;

            const float projectileLifeTime = 0.5f; // TODO: 임시값 변경 필요
            
            var projectile = new Projectile(this, ++_projectileIdKey, attacker, target, attacker.AttackDamage, projectileLifeTime, attacker.GetPos());
            _projectiles.Add(_projectileIdKey, projectile);
            
            attacker.SetDir(target.GetPos() - attacker.GetPos());
            _battleMapEventHandler.OnEntityStartAttack(attackerId, targetEntityId);
            _battleMapEventHandler.OnProjectileAdded(projectile.Id, projectile);
        }

        public void OnProjectilePositionChanged(ulong projectileId, Vec3 pos)
        {
            _battleMapEventHandler.OnProjectilePositionChanged(projectileId, pos);
        }

        public void OnProjectileDirectionChanged(ulong projectileId, Vec3 dir)
        {
            _battleMapEventHandler.OnProjectileDirectionChanged(projectileId, dir);
        }

        public void OnProjectileTriggered(ulong projectileId)
        {
            if (!_projectiles.TryGetValue(projectileId, out var projectile))
                return;
            
            RemoveProjectile(projectileId);
            _battleMapEventHandler.OnProjectileTriggered(projectileId);

            if (!_entities.TryGetValue(projectile.Target.Id, out var target))
                return;
            
            target.Hit(projectile.Damage);
            
            if (!target.IsAlive())
                OnEntityRetired(target.Id);
        }

        public void OnEntityStartMove(uint entityId)
        {
            _battleMapEventHandler.OnEntityStartMove(entityId);
        }

        public void OnEntityStopMove(uint entityId)
        {
            _battleMapEventHandler.OnEntityStopMove(entityId);
        }

        public void OnBattleMapUpdated(float elapsedTime)
        {
            _battleMapEventHandler.OnBattleMapUpdated(elapsedTime);
        }

        public void OnEntityPositionChanged(uint entityId, Vec3 pos)
        {
            _battleMapEventHandler.OnEntityPositionChanged(entityId, pos);
        }

        public void OnEntityDirectionChanged(uint entityId, Vec3 dir)
        {
            _battleMapEventHandler.OnEntityDirectionChanged(entityId, dir);
        }

        public void OnEntityGetDamage(uint entityId, float damage)
        {
            _battleMapEventHandler.OnEntityGetDamage(entityId, damage);
        }

        public IEntityContext TryGetNearestEnemy(uint entityId, float maxDistance)
        {
            if (!_entities.TryGetValue(entityId, out var entity))
                return null;
            
            var pos = entity.GetPos();
            
            var minDistance = float.MaxValue;
            
            IEntityContext nearest = null;
            
            foreach (var otherEntity in _entities.Values)
            {
                if (otherEntity == entity)
                    continue;

                if (!otherEntity.IsAlive())
                    continue;

                if (entity.GetTeamFlag() == otherEntity.GetTeamFlag())
                    continue;
                
                var otherPos = otherEntity.GetPos();
                var distance = Vec3.Distance(pos, otherPos);

                if (distance > maxDistance)
                    continue;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = otherEntity;
                }
            }

            return nearest;
        }

        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
        {
            _battleMapPathFinder.FindWaypoints(start, goal, resultWaypoints);
        }
    }
}
