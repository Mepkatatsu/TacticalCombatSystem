using System.Collections.Generic;
using System.Linq;
using Script.CommonLib.Battle;

namespace Script.CommonLib
{
    public class MoveState : IState
    {
        private const int GlobalMoveSpeedDivisor = 1000;
        
        private IEntityContext _entityContext;
    
        private FixedPos _pos;
        private FixedDir _dir;
        private FixedPos _destination;
        private FixedPos _authoredDestination;
        private bool _hasAuthoredDestination;
        private bool _hasActiveTacticalDestination;
        private bool _hasExecutedAttackAtTacticalDestination;
        private bool _hasAttemptedAuthoredDestinationResume;
        private bool _shouldPrioritizeMovement;
        private FixedDir _lastMoveDirection;
        private bool _hasLastMoveDirection;
        private readonly List<GridPos> _paths = new(); // TODO: List에서 다른 자료형으로 바꾸는 게 나을 수도... 현재는 에디터에서 List를 사용하고 있어서 변경사항이 많아질 것 같아 임시로 구현.

        private ushort _moveSpeed;

        public MoveState(IEntityContext entityContext, ushort moveSpeed)
        {
            _entityContext = entityContext;
            _moveSpeed = moveSpeed;
        }

        public bool HasArrived()
        {
            return _pos == _destination;
        }

        public bool ShouldPrioritizeMovement => _shouldPrioritizeMovement && !HasArrived();

        public void SetDestination(FixedPos destination)
        {
            _destination = destination;
            _hasAuthoredDestination = false;
            _hasActiveTacticalDestination = false;
            _hasExecutedAttackAtTacticalDestination = false;
            _hasAttemptedAuthoredDestinationResume = false;
            _shouldPrioritizeMovement = false;
        }

        internal void SetTacticalDestination(FixedPos destination, List<GridPos> paths)
        {
            _authoredDestination = _destination;
            _hasAuthoredDestination = true;
            _hasActiveTacticalDestination = true;
            _hasExecutedAttackAtTacticalDestination = false;
            _hasAttemptedAuthoredDestinationResume = false;
            SetDestinationWithPath(destination, paths, true);
        }

        internal void SetPredictionDestination(FixedPos destination, List<GridPos> paths)
        {
            SetDestinationWithPath(destination, paths, false);
        }

        private void SetDestinationWithPath(
            FixedPos destination,
            List<GridPos> paths,
            bool shouldPrioritizeMovement)
        {
            _destination = destination;
            _shouldPrioritizeMovement = shouldPrioritizeMovement;
            _paths.Clear();
            _paths.AddRange(paths);
        }

        internal FixedPos GetDestination() => _destination;

        internal bool IsWaitingAtTacticalDestination =>
            _hasActiveTacticalDestination &&
            !_shouldPrioritizeMovement &&
            HasArrived();

        internal FixedPos GetAuthoredDestination() => _authoredDestination;

        internal bool TryGetLastMoveDirection(out FixedDir direction)
        {
            direction = _lastMoveDirection;
            return _hasLastMoveDirection;
        }

        internal void MarkTacticalDestinationAttackExecuted()
        {
            if (_hasActiveTacticalDestination)
                _hasExecutedAttackAtTacticalDestination = true;
        }

        internal bool TryBeginAuthoredDestinationResume()
        {
            if (!_hasAuthoredDestination ||
                !IsWaitingAtTacticalDestination ||
                !_hasExecutedAttackAtTacticalDestination ||
                _hasAttemptedAuthoredDestinationResume ||
                _destination == _authoredDestination)
            {
                return false;
            }

            _hasAttemptedAuthoredDestinationResume = true;
            return true;
        }

        internal void ResumeAuthoredDestination(List<GridPos> paths)
        {
            _destination = _authoredDestination;
            _hasAuthoredDestination = false;
            _hasActiveTacticalDestination = false;
            _hasExecutedAttackAtTacticalDestination = false;
            _hasAttemptedAuthoredDestinationResume = false;
            _shouldPrioritizeMovement = false;
            _paths.Clear();
            _paths.AddRange(paths);
        }

        public void Enter()
        {
            _entityContext.OnStartMove();
        }

        public void Update(ushort deltaMs)
        {
            _entityContext.TryGetNearestEnemy();

            if (HasArrived())
            {
                _shouldPrioritizeMovement = false;
                return;
            }

            if (_shouldPrioritizeMovement && _moveSpeed == 0)
            {
                _shouldPrioritizeMovement = false;
                return;
            }

            if (_shouldPrioritizeMovement && (long)_moveSpeed * deltaMs / GlobalMoveSpeedDivisor == 0)
            {
                _shouldPrioritizeMovement = false;
                return;
            }

            if (_paths.IsEmpty())
            {
                if (_shouldPrioritizeMovement)
                {
                    _shouldPrioritizeMovement = false;
                    return;
                }

                FindPath();
            }

            if (_paths.IsEmpty())
            {
                _shouldPrioritizeMovement = false;
                return;
            }
            
            MovePath(_pos, deltaMs);

            if (_shouldPrioritizeMovement && (HasArrived() || _paths.IsEmpty()))
                _shouldPrioritizeMovement = false;
        }

        public void Exit()
        {
            _entityContext.OnStopMove();
        }

        private void FindPath()
        {
            _paths.Clear();

            // TODO: GridPos가 아닌 곳으로 이동하고 싶을 수 있을 듯 하여 추후 수정이 필요할 것 같음.
            var startPos = _pos.ToGridPos();
            var endPos = _destination.ToGridPos();
        
            _entityContext.FindWaypoints(startPos, endPos, _paths);
        }

        public void SetPos(FixedPos pos)
        {
            _pos = pos;
        }

        public void SetDir(FixedDir dir)
        {
            _dir = dir;
        }
        
        public FixedPos GetPos() => _pos;
        public FixedDir GetDir() => _dir;

        public void MovePath(FixedPos pos, ushort deltaMs)
        {
            if (_paths.Count == 0)
                return;
            
            var nextGridPos = _paths.Last();
            var nextPos = nextGridPos.ToFixedPos();
            
            var moveDistance = (long)_moveSpeed * deltaMs / GlobalMoveSpeedDivisor;
            var maxMoveDistance = pos.GetDistance(nextPos);

            if (moveDistance >= maxMoveDistance)
            {
                _paths.RemoveAt(_paths.Count - 1);
            }
            else
            {
                var deltaFixedPos = nextPos - pos;
                var distance = nextPos.GetDistance(pos);
                var moveX = deltaFixedPos.X * moveDistance / distance; // TODO: 반올림/최솟값 처리 고려
                var moveZ = deltaFixedPos.Z * moveDistance / distance; // TODO: 반올림/최솟값 처리 고려
                
                nextPos = new FixedPos(pos.X + moveX, pos.Y, pos.Z + moveZ);
            }
        
            _entityContext.SetPos(nextPos);
            _lastMoveDirection = new FixedDir(pos, nextPos);
            _hasLastMoveDirection = pos != nextPos;
            _entityContext.SetDir(new FixedDir(pos, nextPos));
        }
    }
}
