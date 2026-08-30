using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public interface IBattleMapContext
    {
        public void OnEntityPositionChanged(uint entityId, FixedPos pos);
        public void OnEntityDirectionChanged(uint entityId, FixedDir dir);
        public void OnEntityGetDamage(uint entityId, uint damage);
        public IEntityContext TryGetNearestEnemy(uint entityId, long maxDistance);
        public bool HasAliveEnemy(uint entityId);
        public bool TryFindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints);
        public bool TryFindWaypointsFromArbitraryPositions(
            GridPos start,
            GridPos goal,
            List<GridPos> resultWaypoints);
        public void SmoothPathTransition(FixedPos start, FixedDir incomingDirection, List<GridPos> waypoints);
        public uint ElapsedMs { get; }
        public void RequestAttack(uint attackerId, uint targetEntityId);
        
        public void OnProjectilePositionChanged(ulong projectileId, FixedPos pos);
        public void OnProjectileDirectionChanged(ulong projectileId, FixedDir dir);
        public void OnProjectileTriggered(ulong projectileId);
        
        public void OnEntityStartMove(uint entityId);
        public void OnEntityStopMove(uint entityId);
    }
}
