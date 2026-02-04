using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Script.CommonLib.Map
{
    [Serializable]
    public class BattleMapData
    {
        public const int MaxMapSize = 1000;    // 너무 크게 만들지 않도록 제한
    
        public GridPos minGridPos;
        public GridPos maxGridPos;

        public List<BattlePositionData> battlePositions;
        public List<ObstacleData> obstacles = new();
        public List<EntityData> entities = new();
    
        private HashSet<GridPos> _blockedPoints = new();
    
        [JsonIgnore]
        public HashSet<GridPos> BlockedPoints
        {
            get
            {
                if (_blockedPoints.Count > 0)
                    return _blockedPoints;
            
                for (var i = 0; i < obstacles.Count; i++)
                {
                    var obstacle = obstacles[i];
                
                    for (var j = 0; j < obstacle.blockedPoints.Count; j++)
                    {
                        _blockedPoints.Add(obstacle.blockedPoints[j]);
                    }
                }

                return _blockedPoints;
            }
        }
    
        private List<GridPos> _waypoints = new();
        [JsonIgnore]
        public List<GridPos> Waypoints
        {
            get
            {
                if (_waypoints.IsNotEmpty())
                    return _waypoints;
            
                for (var i = 0; i < battlePositions.Count; i++)
                {
                    var battlePositionData = battlePositions[i];
                    _waypoints.Add(battlePositionData.gridPos);
                }
            
                for (var i = 0; i < obstacles.Count; i++)
                {
                    var obstacle = obstacles[i];
                    _waypoints.AddRange(obstacle.waypoints);
                }

                return _waypoints;
            }
        }

        public BattlePositionData GetBattlePositionDataByName(string name)
        {
            // TODO: 성능 개선
            return battlePositions.Find(e => e.name == name);
        }
    }
}