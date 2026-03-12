using System.Collections.Generic;
using Script.CommonLib.Battle;

namespace Script.CommonLib
{
    public interface IEntityContext
    {
        public uint Id { get; }
        public EntityStateType CurrentStateType { get; }
        public uint Hp { get; }
        public ushort AttackDelayMs { get; }
        public bool IsAlive();
        public bool HasArrived();
        public bool HasMainTarget();
        public bool IsMainTargetInRange();
        public void TryGetNearestEnemy();
        public FixedPos GetPos();
        public void SetPos(FixedPos pos);
        public FixedDir GetDir();
        public void SetDir(FixedDir dir);
        public void OnStartMove();
        public void OnStopMove();
        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints);
        public uint GetBattleMapElapsedMs();
        public void RequestAttack();
    }
}
