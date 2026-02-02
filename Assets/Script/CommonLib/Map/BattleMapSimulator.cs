using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public class BattleMapSimulator
    {
        public BattleMapSimulator(BattleMapData battleMapData)
        {
            _battleMapData = battleMapData;
            _battleMapPathFinder = new BattleMapPathFinder(battleMapData);
        }
        
        private readonly BattleMapData _battleMapData;
        private readonly BattleMapPathFinder _battleMapPathFinder;
        
        private List<Entity> _entities = new();

        public void Update(float deltaTime)
        {
            foreach (var entity in _entities)
            {
                entity.Update(deltaTime);
            }
        }
    }
}
