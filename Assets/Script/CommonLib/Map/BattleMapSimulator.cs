using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public class BattleMapSimulator : IBattleMapEventHandler
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
        
        private readonly List<Entity> _entities = new();

        public void Init()
        {
            foreach (var entityData in _battleMapData.entities)
            {
                var entity = new Entity(this, _battleMapPathFinder, entityData);
                _entities.Add(entity);
                OnEntityAdded(entity);
            }
        }

        public void Update(float deltaTime)
        {
            foreach (var entity in _entities)
            {
                entity.Update(deltaTime);
            }
        }

        public void OnEntityAdded(Entity entity)
        {
            _battleMapEventHandler.OnEntityAdded(entity);

            var startPositionData = _battleMapData.GetBattlePositionDataByName(entity.startPositionName);
            var endPositionData = _battleMapData.GetBattlePositionDataByName(entity.endPositionName);

            if (startPositionData != null)
                entity.SetPos(startPositionData.gridPos);
            
            if (endPositionData != null)
                entity.MoveTo(new Vector3(endPositionData.gridPos.x, 0, endPositionData.gridPos.y));
        }

        public void OnEntityPositionChanged(Entity entity, Vector3 pos)
        {
            _battleMapEventHandler.OnEntityPositionChanged(entity, pos);
        }

        public void OnEntityDirectionChanged(Entity entity, Vector3 pos)
        {
            _battleMapEventHandler.OnEntityDirectionChanged(entity, pos);
        }
    }
}
