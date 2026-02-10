using System;
using System.Collections.Generic;
using Script.CommonLib.Battle;
using Script.CommonLib.Map;

namespace Script.CommonLib
{
    [Serializable]
    public class Entity : IEntityContext
    {
        public uint Id { get; }

        private IBattleMapContext _battleMapContext;

        private TeamFlag _teamFlag;
        public TeamFlag GetTeamFlag() => _teamFlag;
        
        public string name;
        public string startPositionName;
        public string endPositionName;

        private EntityStateType _currentStateType = EntityStateType.Idle;
        public EntityStateType CurrentStateType => _currentStateType;
        
        private float _maxHp;
        private float _hp;
        private float _attackDamage;
        private float _attackSpeed;
        private float _attackRange;
        private float _moveSpeed;

        public float AttackDamage => _attackDamage;
        public float AttackSpeed => _attackSpeed;
        public float AttackRange => _attackRange;

        private IEntityContext _mainTarget;

        private EntityBrain _brain;
        private IdleState _idleState;
        private MoveState _moveState;
        private AttackState _attackState;
        private DieState _dieState;
        

        public Entity(uint id, IBattleMapContext battleMapContext, EntityData entityData)
        {
            Id = id;
            _teamFlag = entityData.teamFlag;
            _battleMapContext = battleMapContext;
            name = entityData.name;
            startPositionName = entityData.startPositionName;
            endPositionName = entityData.endPositionName;

            _maxHp = entityData.maxHp;
            _attackDamage = entityData.attackDamage;
            _attackSpeed = entityData.attackSpeed;
            _attackRange = entityData.attackRange;
            _moveSpeed = entityData.moveSpeed;
            
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

            if (_currentStateType != nextStateType)
            {
                var currentState = GetState(_currentStateType);
                currentState.Exit();
                nextState.Enter();
                _currentStateType = nextStateType;
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

        public bool IsMainTargetInRange()
        {
            if (!HasMainTarget())
                return false;
            
            var distance = Vec3.Distance(GetPos(), _mainTarget.GetPos());
            return distance <= _attackRange;
        }

        public void TryGetNearestEnemy()
        {
            _mainTarget = _battleMapContext.TryGetNearestEnemy(Id, _attackRange);
        }

        public Vec3 GetPos()
        {
            return _moveState.GetPos();
        }

        public void Hit(float damage)
        {
            _hp -= damage;
            
            if (_hp < 0)
                _hp = 0;
            
            if (_hp <= 0)
                LogHelper.Log($"entity {Id} is dead!!!");
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
            _battleMapContext.OnEntityPositionChanged(Id, pos);
        }

        public Vec3 GetDir() => _moveState.GetDir();
        
        public void SetDir(Vec3 dir)
        {
            _moveState.SetDir(dir);
            _battleMapContext.OnEntityDirectionChanged(Id, dir);
        }

        public void SetDestination(Vec3 pos)
        {
            _moveState.SetDestination(pos);
        }

        public void OnStartMove()
        {
            _battleMapContext.OnEntityStartMove(Id);
        }
        
        public void OnStopMove()
        {
            _battleMapContext.OnEntityStopMove(Id);
        }

        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints)
        {
            _battleMapContext.FindWaypoints(start, goal, resultWaypoints);
        }

        public float GetBattleMapElapsedSec()
        {
            return _battleMapContext.ElapsedSec;
        }

        public void RequestAttack()
        {
            _battleMapContext.RequestAttack(Id, _mainTarget.Id);
        }
    }
}
