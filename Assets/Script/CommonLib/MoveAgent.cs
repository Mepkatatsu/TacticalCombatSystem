using System.Collections.Generic;
using System.Linq;
using Script.CommonLib.Map;

namespace Script.CommonLib
{
    public interface IMover
    {
        public Vector3 GetPos();
        public void SetPos(Vector3 pos);
        public Vector3 GetDir();
        public void SetDir(Vector3 dir);
    }

    public class MoveAgent
    {
        private BattleMapPathFinder _pathFinder;
        private IMover _mover;
    
        private Vector3 _destination;
    
        private List<GridPos> _paths = new(); // TODO: List에서 다른 자료형으로 바꾸는 게 나을 수도... 현재는 에디터에서 List를 사용하고 있어서 변경사항이 많아질 것 같아 임시로 구현.
        private bool _isMoving;

        private float _moveSpeed;

        public MoveAgent(BattleMapPathFinder pathFinder, IMover mover, float moveSpeed)
        {
            _pathFinder = pathFinder;
            _mover = mover;
            _moveSpeed = moveSpeed;
            
            _isMoving = false;
        }

        public void SetDestination(Vector3 destination)
        {
            _destination = destination;
        }

        public void SetIsMoving(bool isMoving)
        {
            _isMoving = isMoving;
        }
    
        public void Update(float deltaTime)
        {
            if (!_isMoving)
                return;
        
            var pos = _mover.GetPos();

            if (pos == _destination)
                return;

            if (_paths.IsEmpty())
            {
                FindPath();
            }

            MovePath(pos, deltaTime);
        }
    
        private void FindPath()
        {
            _paths.Clear();

            // GridPos가 아닌 곳으로 이동하고 싶을 수 있을 듯 하여 추후 수정이 필요할 것 같음.
            var startPos = new GridPos(_mover.GetPos());
            var endPos = new GridPos(_destination);
        
            _pathFinder.FindWaypoints(startPos, endPos, _paths);
        }

        public void MovePath(Vector3 pos, float deltaTime)
        {
            if (_paths.Count == 0)
                return;
            
            var nextGridPos = _paths.Last();
            var nextPos = new Vector3(nextGridPos.x, 0, nextGridPos.y);

            var nextMoveVector = nextPos - pos;

            var dir = nextMoveVector.normalized;
            var moveDistance = deltaTime * _moveSpeed;
            var maxMoveDistance = Vector3.Distance(pos, nextPos);

            if (moveDistance > maxMoveDistance)
            {
                _paths.RemoveAt(_paths.Count - 1);
                moveDistance = maxMoveDistance;
            }
            
            nextPos = pos + dir * moveDistance;
        
            _mover.SetPos(nextPos);
            
            var currentDir = _mover.GetDir();
            var nextDir = Vector3.Lerp(currentDir, dir, deltaTime * _moveSpeed);
            _mover.SetDir(nextDir);
        }
    }
}