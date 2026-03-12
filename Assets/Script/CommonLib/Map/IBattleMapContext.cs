using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public interface IBattleMapContext
    {
        public void OnEntityPositionChanged(uint entityId, FixedPos pos);
        public void OnEntityDirectionChanged(uint entityId, FixedDir dir);
        public void OnEntityGetDamage(uint entityId, uint damage);
        public IEntityContext TryGetNearestEnemy(uint entityId, float maxDistance);
        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints);
        public uint ElapsedMs { get; }
        public void RequestAttack(uint attackerId, uint targetEntityId);
        
        public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos);
        public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir);
        public void OnProjectileTriggered(ulong projectileId);
        
        public void OnEntityStartMove(uint entityId);
        public void OnEntityStopMove(uint entityId);
    }
}
