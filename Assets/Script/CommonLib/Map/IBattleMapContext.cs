using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public interface IBattleMapContext
    {
        public void OnEntityPositionChanged(uint entityId, Vec3 pos);
        public void OnEntityDirectionChanged(uint entityId, Vec3 direction);
        public IEntityContext TryGetNearestEnemy(uint entityId, float maxDistance);
        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints);
        
        public void OnEntityStartMove(uint entityId);
        public void OnEntityStopMove(uint entityId);
    }
}
