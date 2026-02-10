using System.Collections.Generic;
using Script.CommonLib.Battle;

namespace Script.CommonLib
{
    public interface IEntityContext
    {
        public EntityStateType CurrentStateType { get; }
        public float AttackSpeed { get; }
        public bool IsAlive();
        public bool HasArrived();
        public bool HasMainTarget();
        public void TryGetNearestEnemy();
        public Vec3 GetPos();
        public void SetPos(Vec3 pos);
        public Vec3 GetDir();
        public void SetDir(Vec3 dir);
        public void OnStartMove();
        public void OnStopMove();
        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints);
    }
}
