using System.Collections.Generic;
using Script.CommonLib.Battle;

namespace Script.CommonLib
{
    public interface IEntityContext
    {
        public EntityStateType CurrentStateTypeType { get; }
        public bool IsAlive();
        public bool HasArrived();
        public bool HasMainTarget();
        public void TryGetNearestEnemy();
        public Vector3 GetPos();
        public void SetPos(Vector3 pos);
        public Vector3 GetDir();
        public void SetDir(Vector3 dir);
        public void OnStartMove();
        public void OnStopMove();
        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints);
    }
}
