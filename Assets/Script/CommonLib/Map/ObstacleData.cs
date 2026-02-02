using System;
using System.Collections.Generic;

namespace Script.CommonLib
{
    [Serializable]
    public class ObstacleData
    {
        public Vector2 position;
        
        public List<GridPos> waypoints;
        public List<GridPos> blockedPoints;
    }
}
