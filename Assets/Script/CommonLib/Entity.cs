using System;
using Script.CommonLib.Map;

namespace Script.CommonLib
{
    [Serializable]
    public class Entity : IMover
    {
        private uint _id;
        
        private IBattleMapEventHandler _battleMapEventHandler;
        private MoveAgent _moveAgent;
        private Vector3 _pos;
        private Vector3 _dir;

        private TeamFlag _teamFlag;
        public string name;
        public string startPositionName;
        public string endPositionName;
        
        private float _maxHp = 10f;
        private float _hp;
        private float _attackDamage = 1f;
        private float _attackSpeed = 1f;
        private float _attackRange = 15f;
        private float _moveSpeed = 5f;

        public Entity(uint id, IBattleMapEventHandler battleMapEventHandler, BattleMapPathFinder pathFinder, EntityData entityData)
        {
            _id = id;
            _teamFlag = entityData.teamFlag;
            _battleMapEventHandler = battleMapEventHandler;
            _moveAgent = new MoveAgent(pathFinder, this, _moveSpeed);
            name = entityData.name;
            startPositionName = entityData.startPositionName;
            endPositionName = entityData.endPositionName;

            _hp = _maxHp;
        }
        
        public TeamFlag GetTeamFlag() => _teamFlag;

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
            _battleMapEventHandler.OnEntityPositionChanged(_id, pos);
        }

        public Vector3 GetDir()
        {
            return _dir;
        }
        
        public void SetDir(Vector3 dir)
        {
            _dir = dir;
            _battleMapEventHandler.OnEntityDirectionChanged(_id, dir);
        }

        public void MoveTo(Vector3 pos)
        {
            _moveAgent.SetDestination(pos);
            _moveAgent.SetIsMoving(true);
            _battleMapEventHandler.OnEntityStartMoving(_id);
        }
        
        public void StopMove()
        {
            _battleMapEventHandler.OnEntityStopMoving(_id);
        }
    }
}
