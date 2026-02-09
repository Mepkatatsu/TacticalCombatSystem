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

        public void OnEntityStartMove(uint entityId)
        {
            _battleMapEventHandler.OnEntityStartMove(entityId);
        }

        public void OnEntityStopMove(uint entityId)
        {
            _battleMapEventHandler.OnEntityStopMove(entityId);
        }

        public void OnEntityPositionChanged(uint entityId, Vec3 pos)
        {
            _battleMapEventHandler.OnEntityPositionChanged(entityId, pos);
        }

        public void OnEntityDirectionChanged(uint entityId, Vec3 dir)
        {
            _battleMapEventHandler.OnEntityDirectionChanged(entityId, dir);
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
