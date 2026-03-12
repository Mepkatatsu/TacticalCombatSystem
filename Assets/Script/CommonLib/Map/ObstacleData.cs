using System;
using System.Collections.Generic;

namespace Script.CommonLib.Map
{
    [Serializable]
    public class ObstacleData
    {
        public List<GridPos> waypoints;
        public List<GridPos> blockedPoints;
    }
}
