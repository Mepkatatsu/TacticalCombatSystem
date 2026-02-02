using System.Collections.Generic;

namespace Script.CommonLib
{
    public interface IMover
    {
        public Vector3 GetPos();
        public void SetPos(Vector3 pos);
    }

    public class MoveAgent
    {
        private IMover _mover;
    
        private Vector3 _destination;
    
        private Queue<Vector3> _paths;

        private bool _isMoving;

        private float _moveSpeed;

        public MoveAgent(IMover mover, float moveSpeed)
        {
            _mover = mover;
            _moveSpeed = moveSpeed;
            _paths = new Queue<Vector3>();
        
            _destination = Vector3.zero;
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
            _paths ??= new Queue<Vector3>();
            _paths.Clear();
        
            // TODO: FindPath
        
            _paths.Enqueue(_destination);
        }

        public void MovePath(Vector3 pos, float deltaTime)
        {
            if (!_paths.TryPeek(out var nextPos))
                return;

            var nextMoveVector = nextPos - pos;

            var dir = nextMoveVector.normalized;
            var moveDistance = deltaTime * _moveSpeed;
            var maxMoveDistance = Vector3.Distance(pos, nextPos);

            if (moveDistance > maxMoveDistance)
            {
                _paths.Dequeue();
            }
            else
            {
                nextPos = pos + dir * moveDistance;
            }
        
            _mover.SetPos(nextPos);
        }
    }
}