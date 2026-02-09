using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    public interface IBattleMapContext
    {
        public IEntityContext TryGetNearestEnemy(uint entityId, float maxDistance);
        public void FindWaypoints(GridPos start, GridPos goal, List<GridPos> resultWaypoints);
    }
}
