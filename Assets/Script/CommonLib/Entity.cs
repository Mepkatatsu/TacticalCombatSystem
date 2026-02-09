using System;
using System.Collections.Generic;
using Script.CommonLib.Battle;
using Script.CommonLib.Map;

namespace Script.CommonLib
{
    [Serializable]
    public class Entity : IEntityContext
    {
        private uint _id;
        
        private IBattleMapContext _battleMapContext;

        private TeamFlag _teamFlag;
        public TeamFlag GetTeamFlag() => _teamFlag;
        
        public string name;
        public string startPositionName;
        public string endPositionName;

        private EntityStateType _currentStateTypeType = EntityStateType.Idle;
        public EntityStateType CurrentStateTypeType => _currentStateTypeType;
        
        private float _maxHp = 10f;
        private float _hp;
        private float _attackDamage = 1f;
        private float _attackSpeed = 1f;
        private float _attackRange = 15f;
        private float _moveSpeed = 5f;

        private IEntityContext _mainTarget;

        private EntityBrain _brain;
        private IdleState _idleState;
        private MoveState _moveState;
        private AttackState _attackState;
        private DieState _dieState;
        

        public Entity(uint id, IBattleMapContext battleMapContext, EntityData entityData)
        {
            _id = id;
            _teamFlag = entityData.teamFlag;
            _battleMapContext = battleMapContext;
            name = entityData.name;
            startPositionName = entityData.startPositionName;
            endPositionName = entityData.endPositionName;

            _hp = _maxHp;

            _brain = new EntityBrain(this);
            _idleState = new IdleState(this);
            _moveState = new MoveState(this, _moveSpeed);
            _attackState = new AttackState(this);
            _dieState = new DieState(this);
        }

        public void Update(float deltaTime)
        {
            var nextStateType = _brain.ThinkNextStateType();
            var nextState = GetState(nextStateType);

            if (_currentStateTypeType != nextStateType)
            {
                var currentState = GetState(_currentStateTypeType);
                currentState.Exit();
                nextState.Enter();
            }

            nextState.Update(deltaTime);
        }

        private IState GetState(EntityStateType stateType)
        {
            switch (stateType)
            {
                case EntityStateType.Idle:
                    return _idleState;
                case EntityStateType.Move:
                    return _moveState;
                case EntityStateType.Attack:
                    return _attackState;
                case EntityStateType.Die:
                    return _dieState;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
            }
        }
        
        public bool HasMainTarget() => _mainTarget != null && _mainTarget.IsAlive();

        public void TryGetNearestEnemy()
        {
            _mainTarget = _battleMapContext.TryGetNearestEnemy(_id, _attackRange);
        }

        public Vec3 GetPos()
        {
            return _moveState.GetPos();
        }

        public bool IsAlive()
        {
            return _hp > 0;
        }

        public bool HasArrived()
        {
            return _moveState.HasArrived();
        }

        public void SetPos(GridPos gridPos)
        {
            SetPos(new Vec3(gridPos.x, 0, gridPos.y));
        }

        public void SetPos(Vec3 pos)
        {
            _moveState.SetPos(pos);
            _battleMapContext.OnEntityPositionChanged(_id, pos);
        }

        public Vec3 GetDir() => _moveState.GetDir();
        
        public void SetDir(Vec3 dir)
        {
            _moveState.SetDir(dir);
            _battleMapContext.OnEntityDirectionChanged(_id, dir);
        }

        public void SetDestination(Vec3 pos)
        {
            _moveState.SetDestination(pos);
        }

        public void OnStartMove()
        {
            _battleMapContext.OnEntityStartMove(_id);
        }
        
        public void OnStopMove()
        {
            _battleMapContext.OnEntityStopMove(_id);
        }

        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
        {
            _battleMapContext.FindWaypoints(start, goal, resultWaypoints);
        }
    }
}
