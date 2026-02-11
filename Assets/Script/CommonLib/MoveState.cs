using System.Collections.Generic;
using System.Linq;
using Script.CommonLib.Battle;

namespace Script.CommonLib
{
    public class MoveState : IState
    {
        private IEntityContext _entityContext;
    
        private Vec3 _pos;
        private Vec3 _dir;
        private Vec3 _destination;
        private readonly List<GridPos> _paths = new(); // TODO: List에서 다른 자료형으로 바꾸는 게 나을 수도... 현재는 에디터에서 List를 사용하고 있어서 변경사항이 많아질 것 같아 임시로 구현.

        private float _moveSpeed;

        public MoveState(IEntityContext entityContext, float moveSpeed)
        {
            _entityContext = entityContext;
            _moveSpeed = moveSpeed;
        }

        public bool HasArrived()
        {
            return _entityContext.GetPos() == _destination;
        }

        public void SetDestination(Vec3 destination)
        {
            _destination = destination;
        }

        public void Enter()
        {
            _entityContext.OnStartMove();
        }

        public void Update(float deltaTime)
        {
            _entityContext.TryGetNearestEnemy();

            if (HasArrived())
                return;

            if (_paths.IsEmpty())
                FindPath();
            
            var pos = _entityContext.GetPos();
            MovePath(pos, deltaTime);
        }

        public void Exit()
        {
            _entityContext.OnStopMove();
        }

        private void FindPath()
        {
            _paths.Clear();

            // GridPos가 아닌 곳으로 이동하고 싶을 수 있을 듯 하여 추후 수정이 필요할 것 같음.
            var startPos = new GridPos(_entityContext.GetPos());
            var endPos = new GridPos(_destination);
        
            _entityContext.FindWaypoints(startPos, endPos, _paths);
        }

        public void SetPos(Vec3 pos)
        {
            _pos = pos;
        }

        public void SetDir(Vec3 dir)
        {
            _dir = dir;
        }
        
        public Vec3 GetPos() => _pos;
        public Vec3 GetDir() => _dir;

        public void MovePath(Vec3 pos, float deltaTime)
        {
            if (_paths.Count == 0)
                return;
            
            var nextGridPos = _paths.Last();
            var nextPos = new Vec3(nextGridPos.x, 0, nextGridPos.y);

            var nextMoveVector = nextPos - pos;

            var dir = nextMoveVector.normalized;
            var moveDistance = deltaTime * _moveSpeed;
            var maxMoveDistance = Vec3.Distance(pos, nextPos);

            if (moveDistance > maxMoveDistance)
            {
                _paths.RemoveAt(_paths.Count - 1);
                moveDistance = maxMoveDistance;
            }
            
            nextPos = pos + dir * moveDistance;
        
            _entityContext.SetPos(nextPos);
            
            var nextDir = Vec3.Lerp(_dir, dir, deltaTime * _moveSpeed);
            _entityContext.SetDir(nextDir);
        }
    }
}