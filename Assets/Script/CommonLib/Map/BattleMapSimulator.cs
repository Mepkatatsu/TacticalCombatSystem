using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public class BattleMapSimulator : IBattleMapEventHandler, IBattleMapContext
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
        
        private readonly Dictionary<uint, Entity> _entities = new();

        private uint _entityIdKey;

        public void Init()
        {
            foreach (var entityData in _battleMapData.entities)
            {
                AddEntity(entityData);
            }
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in _entities.Values)
            {
                entity.Update(deltaTime);
            }
        }

        private void AddEntity(EntityData entityData)
        {
            var entity = new Entity(++_entityIdKey, this, this, _battleMapPathFinder, entityData);
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
                entity.MoveTo(new Vector3(endPositionData.gridPos.x, 0, endPositionData.gridPos.y));
        }

        public void OnEntityPositionChanged(uint entityId, Vector3 pos)
        {
            _battleMapEventHandler.OnEntityPositionChanged(entityId, pos);
        }

        public void OnEntityDirectionChanged(uint entityId, Vector3 pos)
        {
            _battleMapEventHandler.OnEntityDirectionChanged(entityId, pos);
        }

        public void OnEntityStartMoving(uint entityId)
        {
            _battleMapEventHandler.OnEntityStartMoving(entityId);
        }

        public void OnEntityStopMoving(uint entityId)
        {
            _battleMapEventHandler.OnEntityStopMoving(entityId);
        }

        public IEntityContext GetNearestEnemy(uint entityId, float maxDistance)
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
                var distance = Vector3.Distance(pos, otherPos);

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
    }
}
