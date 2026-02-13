using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public interface IBattleMapContext
    {
        public void OnEntityPositionChanged(uint entityId, Vec3 pos);
        public void OnEntityDirectionChanged(uint entityId, Vec3 dir);
        public void OnEntityGetDamage(uint entityId, float damage);
        public IEntityContext TryGetNearestEnemy(uint entityId, float maxDistance);
        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints);
        public float ElapsedSec { get; }
        public void RequestAttack(uint attackerId, uint targetEntityId);
        
        public void OnProjectilePositionChanged(ulong projectileId, Vec3 pos);
        public void OnProjectileDirectionChanged(ulong projectileId, Vec3 dir);
        public void OnProjectileTriggered(ulong projectileId);
        
        public void OnEntityStartMove(uint entityId);
        public void OnEntityStopMove(uint entityId);
    }
}
