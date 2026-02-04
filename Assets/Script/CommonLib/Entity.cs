using System;
using Script.CommonLib.Map;

namespace Script.CommonLib
{
    [Serializable]
    public class Entity : IMover
    {
        private IBattleMapEventHandler _battleMapEventHandler;
        private MoveAgent _moveAgent;
        private Vector3 _pos;
        private Vector3 _dir;

        public string name;
        public string startPositionName;
        public string endPositionName;
        public Vector3 modelScale = new Vector3(3.5f, 3.5f, 3.5f);

        public Entity(BattleMapPathFinder pathFinder)
        {
            _moveAgent = new MoveAgent(pathFinder, this, 5);
        }

        public Entity(IBattleMapEventHandler battleMapEventHandler, BattleMapPathFinder pathFinder, EntityData entityData)
        {
            _battleMapEventHandler = battleMapEventHandler;
            _moveAgent = new MoveAgent(pathFinder, this, 5);
            name = entityData.name;
            startPositionName = entityData.startPositionName;
            endPositionName = entityData.endPositionName;
            modelScale = entityData.modelScale;
        }

        public void Update(float deltaTime)
        {
            _moveAgent.Update(deltaTime);
        }

        public Vector3 GetPos()
        {
            return _pos;
        }
        
        public void SetPos(GridPos gridPos)
        {
            SetPos(new Vector3(gridPos.x, 0, gridPos.y));
        }

        public void SetPos(Vector3 pos)
        {
            _pos = pos;
            _battleMapEventHandler.OnEntityPositionChanged(this, pos);
        }

        public Vector3 GetDir()
        {
            return _dir;
        }
        
        public void SetDir(Vector3 dir)
        {
            _dir = dir;
            _battleMapEventHandler.OnEntityDirectionChanged(this, dir);
        }

        public void MoveTo(Vector3 pos)
        {
            _moveAgent.SetDestination(pos);
            _moveAgent.SetIsMoving(true);
        }
        
        public void Stop()
        {
            _moveAgent.SetIsMoving(false);
        }
    }
}
